using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using TrackMyScore.Database;
using TrackMyScore.Models;
using TrackMyScore.Services;

namespace TrackMyScore.Controllers
{
  [Route("[controller]/[action]/{id?}")]
  public class TournamentController : Controller
  {
    private readonly AppDbContext _context;
    private static readonly char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
    private static readonly Random random = new Random();
    private readonly UserService _userService;

    private string GenerateTournamentCode()
    { // method for generating a new code of 6 characters for tournaments
      return new string(Enumerable.Repeat(chars, 6)
      .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public TournamentController(AppDbContext context, UserService userService)
    {
      _context = context;
      _userService = userService;
    }

    private async Task<User> GetLoggedUserAsync() // getting logged user
    {
      string email = HttpContext.Session.GetString("email");

      if (email == null)
      {
        return null;
      }

      return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    [HttpGet]
    public async Task<IActionResult> Create() // create tournament view page
    {
      var user = await GetLoggedUserAsync();

      if (user == null)
      {
        return RedirectToAction("Login", "Account");
      }

      var games = await _context.FavoriteGames
                          .Where(f => f.User.Id == user.Id)
                          .Select(f => f.Game)
                          .ToListAsync();

      return View(games);
    }

    [HttpPost]
    public async Task<IActionResult> Create(string name, int roomCount, string reward, string location, DateOnly startDate, TimeOnly startTime, int gameId, string mode)
    { // method for creating tournaments

      if (name.Length > 50 || reward.Length > 50)
      {
        return Json(new { success = false, message = "Name or reward length is too long, please choose a shorter one." }); // check if the game exists
      }

      var startDateTime = startDate.ToDateTime(startTime); // converting date and time to datetime
      var host = await GetLoggedUserAsync();
      var code = GenerateTournamentCode();
      var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId);

      var existingCode = await _context.Tournaments.FirstOrDefaultAsync(c => c.Code == code && c.IsActive == true);
      while (existingCode != null) // avoid code duping
      {
        code = GenerateTournamentCode();
        existingCode = await _context.Tournaments.FirstOrDefaultAsync(c => c.Code == code);
      }

      if (game == null || game.Deleted)
      {
        return Json(new { success = false, message = "Game not found." }); // check if the game exists
      }

      var tournament = new Tournament // single mode tournament
      {
        Name = name,
        Reward = reward,
        StartDate = startDateTime,
        Code = code,
        RoomCount = roomCount,
        IsActive = false,
        Host = host,
        Location = location,
        Stage = 0
      };

      await _context.Tournaments.AddAsync(tournament);

      for (int i = 1; i <= roomCount; i++)
      { // creating the rooms regardless of room mode
        var match = new Match
        {
          Name = $"Room {i} - Tournament {name} - Stage 1",
          StartDate = startDateTime,
          Mode = mode,
          Stage = -1,
          StopDate = null,
          Tournament = tournament,
          Host = host,
          Game = game
        };

        await _context.Matches.AddAsync(match);
      }
      await _context.SaveChangesAsync();
      return RedirectToAction("CurrentTournament", "Tournament", new { tournament.Id });
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var user = await GetLoggedUserAsync();
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var joinedSingleTournaments = await _context.Players
            .Include(p => p.Match)
                .ThenInclude(m => m.Tournament)
                    .ThenInclude(t => t.Host)
            .Where(p => p.User.Id == user.Id
                        && p.Match.Tournament.Host.Id != user.Id
                        && (p.Match.Tournament.Stage < 0
                        || (p.Match.Tournament.Stage >= 0
                          && p.Match.Tournament.IsActive == true)))
            .Select(p => p.Match.Tournament)
            .Distinct()
            .ToListAsync();

        var joinedTeamTournaments = await _context.TeamPlayers
            .Include(tp => tp.Match)
                .ThenInclude(m => m.Tournament)
                    .ThenInclude(t => t.Host)
            .Where(tp => tp.User.Id == user.Id
                        && tp.Match.Tournament.Host.Id != user.Id
                        && (tp.Match.Tournament.Stage < 0
                        || (tp.Match.Tournament.Stage >= 0
                          && tp.Match.Tournament.IsActive == true)))
            .Select(tp => tp.Match.Tournament)
            .Distinct()
            .ToListAsync();

        var joinedTournaments = joinedSingleTournaments
            .Concat(joinedTeamTournaments)
            .ToList();

        var myTournaments = await _context.Tournaments
            .Include(t => t.Host)
            .Where(t => t.Host.Id == user.Id && (t.IsActive == true || t.Stage < 1))
            .ToListAsync();

        var joinedIds = joinedTournaments
            .Where(t => t != null)
            .Select(t => t.Id)
            .ToList();

        var availableTournaments = await _context.Tournaments
            .Include(t => t.Host)
            .Where(t => t.Host.Id != user.Id && !joinedIds.Contains(t.Id) && t.IsActive == false && t.Stage == 0)
            .ToListAsync();

        var tournamentList = new TournamentListView
        {
            MyTournaments = myTournaments,
            JoinedTournaments = joinedTournaments,
            AvailableTournaments = availableTournaments
        };

        return View(tournamentList);
    }




    [HttpGet]
    public IActionResult Play()
    {
      return View();
    }

    [HttpGet]
    public async Task<IActionResult> CurrentTournament(int id)
    {
        
        var tournament = await _context.Tournaments
            .Include(t => t.Host)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tournament == null)
        {
            return Json(new { success = false, message = "Tournament not found." });
        }

        var matches = await _context.Matches
            .Include(m => m.Game)
            .Where(m => m.Tournament == tournament)
            .ToListAsync();

        if (!matches.Any())
        {
            return Json(new { success = false, message = "There are no matches in this tournament." });
        }

        var user = await GetLoggedUserAsync();
        var mutualFollowers = await GetMutualFollowersAsync(user);

        List<Player> players = new();
        List<TeamPlayer> teamPlayers = new();
        List<Team> teams = new();

        if (matches[0].Mode == "single")
        {
            players = await _context.Players
            .Include(p => p.Match).ThenInclude(m => m.Tournament)
            .Include(p => p.User)
            .Where(p => p.Match.TournamentId == tournament.Id)
            .ToListAsync();
        }
        else if (matches[0].Mode == "team")
        {
            teamPlayers = await _context.TeamPlayers
              .Include(tp => tp.Match).ThenInclude(m => m.Tournament)
              .Include(tp => tp.User)
              .Include(tp => tp.Team)
              .Where(tp => tp.Match.TournamentId == id)
              .ToListAsync();

            teams = await _context.TeamPlayers
                .Where(p => p.Match.TournamentId == tournament.Id)
                .Select(p => p.Team)
                .Distinct()
                .ToListAsync();
        }

        string tournamentWinner = "";

        if (tournament.Stage > 1 && tournament.IsActive == false)
        {
            if (matches[0].Mode == "single")
            {
                var winnerPlayer = await _context.Players
                    .Include(p => p.User)
                    .Include(p => p.Match)
                        .ThenInclude(p => p.Tournament)
                    .Where(p => p.Match.TournamentId == id && !p.Eliminated)
                    .Select(p => p.User.Username)
                    .FirstOrDefaultAsync();

                tournamentWinner = winnerPlayer ?? "";
            }
            else
            {
                var winnerTeam = await _context.TeamPlayers
                    .Include(tp => tp.Team)
                    .Include(tp => tp.Match)
                        .ThenInclude(tp => tp.Tournament)
                    .Where(tp => tp.Match.TournamentId == id && !tp.Eliminated)
                    .Select(tp => tp.Team.Name)
                    .FirstOrDefaultAsync();

                tournamentWinner = winnerTeam ?? "";
            }
        }

        var model = new TournamentModel
        {
            Tournament = tournament,
            Matches = matches,
            LoggedUser = user,
            SinglePlayers = players,
            TeamPlayers = teamPlayers,
            Teams = teams,
            MutualFollowers = mutualFollowers,
            ModelMatch = matches[0],
            TournamentWinner = tournamentWinner
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> JoinSingle(int id, string code)
    {
      var user = await GetLoggedUserAsync();

      if (user == null)
        return RedirectToAction("Login", "Account");

      var tournament = await _context.Tournaments
          .FirstOrDefaultAsync(t => t.Id == id);

      if (tournament == null)
      {
        return Json(new { success = false, message = "Tournament not found." });
      }

      if (tournament.IsActive || tournament.Stage > 1) {
        return Json(new { success = false, message = "Tournament already started." });
      }

      var modelMatch = await _context.Matches
          .Include(t => t.Tournament)
          .Include(t => t.Game)
          .FirstOrDefaultAsync(t => t.TournamentId == tournament.Id);

      if (modelMatch == null)
      {
        return Json(new { success = false, message = "No matches found for the tournament." });
      }

      var matches = await _context.Matches
        .Include(m => m.Game)
        .Where(t => t.TournamentId == tournament.Id)
        .ToListAsync();

      if (string.IsNullOrEmpty(code) || code != tournament.Code)
      {
        return Json(new { success = false, message = "Invalid tournament code." });
      }

      bool participatingTeam = await _context.TeamPlayers
        .Include(m => m.Match)
          .ThenInclude(m => m.Tournament)
        .Where(m => m.Match.TournamentId != tournament.Id && m.Match.Tournament != null && (m.Match.Tournament.IsActive || (m.Match.Tournament.IsActive == false && m.Match.Stage == -1 )) && m.User == user)
        .AnyAsync();

      bool participatingSingle = await _context.Players
        .Include(m => m.Match)
          .ThenInclude(m => m.Tournament)
        .Where(m => m.Match.TournamentId != tournament.Id && m.Match.Tournament != null && (m.Match.Tournament.IsActive || (m.Match.Tournament.IsActive == false && m.Match.Stage == -1 )) && m.User == user)
        .AnyAsync();

      if (participatingSingle || participatingTeam) { // add .count method if i want to check in how many tournaments the user is taking part
        return Json(new { success = false, message = "Cannot join since the user is already taking part in another tournament." });
      }

      bool joined = false;
      foreach (var match in matches)
      {
        int playerCount = await _context.Players
            .Where(p => p.MatchId == match.Id)
            .CountAsync();

        if (playerCount < 2)
        {
          var player = new Player
          {
            User = user,
            Match = match,
            Eliminated = false,
            Reward = 1,
            Score = 0
          };
          await _context.Players.AddAsync(player);
          joined = true;
          break;
        }
      }

      if (!joined)
      {
        return Json(new { success = false, message = "There is no space left in the matches in order to participate." });
      }

      await _context.SaveChangesAsync();
      return Json(new { success = true, message = "Successfully joined the tournament." });
    }

    [HttpPost]
    public async Task<IActionResult> JoinTeam(int id, string code, List<int>? teammates, string? teamName)
    {

      var user = await GetLoggedUserAsync();

      if (user == null)
        return RedirectToAction("Login", "Account");

      var tournament = await _context.Tournaments
          .FirstOrDefaultAsync(t => t.Id == id);

      if (tournament == null)
      {
        return Json(new { success = false, message = "Tournament not found." });
      }

      if (tournament.IsActive || tournament.Stage > 1) {
        return Json(new { success = false, message = "Tournament already started." });
      }
      
      var modelMatch = await _context.Matches
          .Include(t => t.Tournament)
          .Include(t => t.Game)
          .FirstOrDefaultAsync(t => t.TournamentId == tournament.Id);

      if (modelMatch == null)
      {
        return Json(new { success = false, message = "No matches found for the tournament." });
      }

      if (string.IsNullOrEmpty(code) || code != tournament.Code)
      {
        return Json(new { success = false, message = "Invalid tournament code." });
      }

      if (string.IsNullOrEmpty(teamName) || teammates == null || !teammates.Any())
      {
          return Json(new { success = false, message = "Team name and at least one teammate required." });
      }

      if (teammates.Count + 1 > modelMatch.Game.MaxPlayers)
      {
        return Json(new { success = false, message = "The number of teammates is bigger than the maximum number of players allowed per team." });
      }

      bool participatingTeam = await _context.TeamPlayers
        .Include(m => m.Match)
        .Where(m => m.Match.TournamentId != tournament.Id && m.User == user && m.Match.Tournament.IsActive == true)
        .AnyAsync();

      bool participatingSingle = await _context.Players
        .Include(m => m.Match)
          .ThenInclude(m => m.Tournament)
        .Where(m =>m.User == user && m.Match.TournamentId != tournament.Id && m.Match.Tournament.IsActive == true)
        .AnyAsync();

      if (participatingSingle || participatingTeam) { // checking if the user is participating in any other tournament
        // add .count method if i want to check in how many tournaments the user is taking part
        return Json(new { success = false, message = "Cannot join since the user is already taking part in another tournament." });
      }

      var matches = await _context.Matches
        .Include(m => m.Game)
        .Where(m => m.TournamentId == tournament.Id)
        .ToListAsync();

      foreach (var teammate in teammates)
      {
        var p = await _context.Users.FirstOrDefaultAsync(u => u.Id == teammate);

        if (p == null)
        {
          return Json(new { success = false, message = "User not found." });
        }

        var teammateParticipatingTeam = await _context.TeamPlayers
            .Include(m => m.Match)
            .Where(m => m.Match.TournamentId != tournament.Id && m.User == user && m.Match.Tournament.IsActive == true)
            .AnyAsync(); 

        var teammateParticipatingSingle = await _context.Players
          .Include(m => m.Match)
          .ThenInclude(m => m.Tournament)
          .Where(m =>m.User == user && m.Match.TournamentId != tournament.Id && m.Match.Tournament.IsActive == true)
          .AnyAsync();

        if (teammateParticipatingSingle || teammateParticipatingTeam) // checking if the teammates are participating in any other tournament
        { // add .count method if i want to check in how many tournaments the user is taking part
          return Json(new { success = false, message = $"Cannot join since {p.Username} is already taking part in another tournament." });
        }

        var alreadyJoined = await _context.Players
          .Include(p => p.Match)
          .Include(p => p.User)
          .AnyAsync(p => p.Match.Tournament.Id == id && p.User.Id == teammate);

        if (alreadyJoined)
        {
          return Json(new { success = false, message = "One of your teammates already joined the tournament." });
        }
        
      }

      var team = new Team
      {
        Name = teamName,
        Score = 0
      };

      await _context.AddAsync(team);

      var allPlayers = new List<User> { user };
      var allTeammates = await _context.Users
        .Where(u => teammates.Contains(u.Id))
        .ToListAsync();

      allPlayers.AddRange(allTeammates);

      Match matchWithSpace = null;

      foreach (var match in matches)
      {
        var teamCount = await _context.TeamPlayers
            .Where(tp => tp.MatchId == match.Id)
            .Select(tp => tp.TeamId)
            .Distinct()
            .CountAsync();

        if (teamCount < 2)
        {
          matchWithSpace = match;
          break;
        }
      }

      if (matchWithSpace == null)
      {
        return Json(new { success = false, message = "There is no space left in the matches in order to participate." });
      }

      foreach (var member in allPlayers)
      {
        await _context.TeamPlayers.AddAsync(new TeamPlayer
        {
          User = member,
          Team = team,
          Match = matchWithSpace,
          Eliminated = false,
          Reward = 1
        });
      }

      await _context.SaveChangesAsync();
      return Json(new { success = true, message = "Successfully joined the tournament." });
    }

    private async Task<List<User>> GetMutualFollowersAsync(User user)
    {
      var following = await _context.Followers
          .Where(f => f.Follower.Id == user.Id)
          .Select(f => f.Following)
          .ToListAsync();

      var followers = await _context.Followers
          .Where(f => f.Following.Id == user.Id)
          .Select(f => f.Follower)
          .ToListAsync();

      return following.Intersect(followers).ToList();
    }

    [HttpPost]
    public async Task<IActionResult> Leave(int id)
    { // method for leaving the tournament

      var user = await GetLoggedUserAsync();
      var tournament = await _context.Tournaments
        .FirstOrDefaultAsync(t => t.Id == id);

      if (tournament == null)
      {
        return Json(new { success = false, message = "Tournament not found." });
      }

      var matches = await _context.Matches
        .Where(m => m.TournamentId == tournament.Id)
        .ToListAsync();

      if (!matches.Any())
      {
        return Json(new { success = false, message = "No matches found in the tournament." });
      }

      if (tournament.IsActive && tournament.Stage > 0)
      {
        return Json(new { success = false, message = "Cannot remove player since the tournament already started." });
      }

      if (matches[0].Mode == "single")
      { // removing the player
        var player = await _context.Players
          .Include(p => p.Match)
            .ThenInclude(p => p.Tournament)
          .FirstOrDefaultAsync(p => p.Match.TournamentId == tournament.Id && p.UserId == user.Id);

        if (player == null)
        {
          return Json(new { success = false, message = "Player not found." });
        }

        _context.Players.Remove(player);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Left tournament." });
      }
      else if (matches[0].Mode == "team")
      { // removing the team of the player and the teammates
        var player = await _context.TeamPlayers
          .Include(p => p.Match)
            .ThenInclude(p => p.Tournament)
          .Include(p => p.Team)
          .FirstOrDefaultAsync(p => p.Match.TournamentId == id && p.UserId == user.Id);

        if (player == null)
        {
          return Json(new { success = false, message = "Player not found." });
        }

        var team = await _context.Teams
          .FirstOrDefaultAsync(t => t.Id == player.TeamId);

        if (team == null)
        {
          return Json(new { success = false, message = "Team not found." });
        }

        var teamPlayers = await _context.TeamPlayers
          .Where(p => p.Match.TournamentId == id && p.TeamId == team.Id)
          .ToListAsync();

        foreach (var p in teamPlayers)
        {
          _context.TeamPlayers.Remove(p);
        }

        _context.Teams.Remove(team);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Left tournament." });
      }

      return Json(new { success = false, message = "Tournament not found or you are not taking part in it." });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    { // delete tournament function

      var user = await GetLoggedUserAsync();

      if (user == null)
      {
        return Json(new { success = false, message = "User not found." });
      }

      var tournament = await _context.Tournaments
          .Include(t => t.Host)
          .FirstOrDefaultAsync(t => t.Id == id);

      if (tournament == null)
      {
        return Json(new { success = false, message = "Tournament not found." });
      }

      if (user != tournament.Host)
      {
        return Json(new { success = false, message = "You are not the host of this tournament." });
      }

      if (!tournament.IsActive && tournament.Stage != 0)
      {
        return Json(new { success = false, message = "Tournament already ended." });
      }

      var matches = await _context.Matches
        .Where(p => p.TournamentId == id)
        .ToListAsync();

      if (!matches.Any())
      {
        return Json(new { success = false, message = "No matches found." });
      }

      if (matches[0].Mode == "single")
      {
        var players = await _context.Players
          .Include(p => p.Match)
          .Where(p => p.Match.TournamentId == id)
          .ToListAsync();

        foreach (var player in players)
        {
          _context.Players.Remove(player);
        }

        foreach (var match in matches)
        {
          _context.Matches.Remove(match);
        }
      }
      else if (matches[0].Mode == "team")
      {
        var players = await _context.TeamPlayers
          .Include(p => p.Match)
          .Where(p => p.Match.TournamentId == id)
          .ToListAsync();

        foreach (var player in players)
        {
          _context.TeamPlayers.Remove(player);
        }

        foreach (var match in matches)
        {
          _context.Matches.Remove(match);
        }

        var teams = await _context.TeamPlayers
          .Include(p => p.Match)
          .Where(p => p.Match.TournamentId == id)
          .Select(p => p.Team)
          .Distinct()
          .ToListAsync();

        foreach (var team in teams)
        {
          _context.Teams.Remove(team);
        }
      }

      _context.Tournaments.Remove(tournament);

      await _context.SaveChangesAsync();

      return Json(new { success = true, message = "Successfully deleted the tournament." });
    }

    [HttpPost]
    public async Task<IActionResult> Start(int id)
    { // start tournament
      var tournament = await _context.Tournaments
          .Include(t => t.Host)
          .FirstOrDefaultAsync(t => t.Id == id);

      var modelMatch = await _context.Matches
        .FirstOrDefaultAsync(m => m.Tournament == tournament);

      if (modelMatch == null)
      {
        return Json(new { success = false, message = "Match not found." });
      }

      var user = await GetLoggedUserAsync();

      if (tournament == null)
      {
        return Json(new { success = false, message = "Tournament not found." });
      }

      if (tournament.Host != user)
      {
        return Json(new { success = false, message = "You are not the host of this tournament." });
      }

      if (tournament.Stage != 0)
      {
        return Json(new { success = false, message = "This tournament already ended or is currently taking place." });
      }

      int requiredParticipants = tournament.RoomCount * 2;

      if (modelMatch.Mode == "single")
    {
        int joinedPlayers = await _context.Players
            .Where(p => p.Match.TournamentId == tournament.Id)
            .Select(p => p.UserId)
            .Distinct()
            .CountAsync();

        if (joinedPlayers < requiredParticipants)
        {
            return Json(new { success = false, message = $"Not enough players to start. Exactly {requiredParticipants} players required." });
        }
    }
    else if (modelMatch.Mode == "team")
    {
        int joinedTeams = await _context.TeamPlayers
            .Where(tp => tp.Match.TournamentId == tournament.Id)
            .Select(tp => tp.TeamId)
            .Distinct()
            .CountAsync();

        if (joinedTeams < requiredParticipants)
        {
            return Json(new { success = false, message = $"Not enough teams to start. Exactly {requiredParticipants} teams required." });
        }
    }

      tournament.IsActive = true;
      tournament.Stage = 1;

      await _context.SaveChangesAsync();
      return Json(new { success = true, message = "Tournament started." });
    }

    [HttpPost]
    public async Task<IActionResult> EndStage(int id)
    {
        var tournament = await _context.Tournaments
            .Include(t => t.Host)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (tournament == null)
        {
            return Json(new { success = false, message = "Tournament not found." });
        }

        var roomsInStage = await _context.Matches
            .Where(m => m.TournamentId == tournament.Id && m.Stage == tournament.Stage)
            .ToListAsync();
        if (!roomsInStage.Any())
        {
            return Json(new { success = false, message = "No matches found." });
        }

        if (roomsInStage[0].Mode == "single")
        {
            foreach (var room in roomsInStage)
            {
                var activePlayers = await _context.Players
                    .Include(p => p.User)
                    .Where(p => p.MatchId == room.Id && !p.Eliminated)
                    .ToListAsync();

                var survivor = activePlayers.SingleOrDefault();

                var losers = activePlayers
                    .Where(p => survivor == null || p.Id != survivor.Id)
                    .ToList();

                foreach (var loser in losers)
                {
                    loser.Eliminated = true;
                    loser.User.RespectPoints += loser.Reward;
                }

                if (survivor != null)
                {
                    survivor.Reward *= 2;
                }
            }
        }
        else // team mode
        {
            foreach (var room in roomsInStage)
            {
                var activeTeamPlayers = await _context.TeamPlayers
                    .Include(tp => tp.Team)
                    .Include(tp => tp.User)
                    .Where(tp => tp.MatchId == room.Id && !tp.Eliminated)
                    .ToListAsync();

                var groupedByTeam = activeTeamPlayers
                    .GroupBy(tp => tp.TeamId)
                    .ToList();

                var winningGroup = groupedByTeam.SingleOrDefault();

                foreach (var group in groupedByTeam)
                {
                    if (winningGroup == null || group.Key != winningGroup.Key)
                    {
                        foreach (var tp in group)
                        {
                            tp.Eliminated = true;
                            tp.User.RespectPoints += tp.Reward;
                        }
                    }
                }

                if (winningGroup != null)
                {
                    foreach (var tp in winningGroup)
                    {
                        tp.Reward *= 2;
                    }
                }
            }
        }

        int nextRoomCount = tournament.RoomCount / (int)Math.Pow(2, tournament.Stage);

        for (int i = 1; i <= nextRoomCount; i++)
        {
            var match = new Match
            {
                Name = $"Room {i} - Tournament: {tournament.Name} - Stage {tournament.Stage + 1}",
                StartDate = tournament.StartDate,
                Stage = -1,
                Mode = roomsInStage[0].Mode,
                HostId = roomsInStage[0].GameId,
                GameId = tournament.Host.Id,
                TournamentId = tournament.Id
            };
            await _context.Matches.AddAsync(match);
        }

        await _context.SaveChangesAsync();

        var success = await StartStage(tournament, roomsInStage);
        if (!success)
        {
            return Json(new { success = false, message = "Not enough rooms to distribute players or teams." });
        }

        tournament.Stage += 1;
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Stage ended successfully." });
    }

    private async Task<bool> StartStage(Tournament tournament, List<Match> previousStageRooms)
    {
        if (previousStageRooms[0].Mode == "single")
        {
            var rooms = await _context.Matches
                .Where(r => r.TournamentId == tournament.Id && r.Stage == -1)
                .ToListAsync();

            var survivors = await _context.Players
                .Include(p => p.User)
                .Include(p => p.Match)
                    .ThenInclude(m => m.Tournament)
                .Where(p => !p.Eliminated && p.Match.Tournament == tournament)
                .ToListAsync();

            int roomIndex = 0, playersInRoom = 0;
            foreach (var player in survivors)
            {
                if (roomIndex >= rooms.Count) return false;

                await _context.Players.AddAsync(new Player
                {
                    UserId = player.UserId,
                    MatchId = rooms[roomIndex].Id,
                    Eliminated = false,
                    Reward = player.Reward,
                    Score = 0
                });

                playersInRoom++;
                if (playersInRoom == 2)
                {
                    roomIndex++;
                    playersInRoom = 0;
                }
            }
        }
        else // team mode
        {
            var rooms = await _context.Matches
            .Where(r => r.TournamentId == tournament.Id && r.Stage == -1)
            .ToListAsync();

        var survivors = await _context.TeamPlayers
            .Include(tp => tp.User)
            .Include(tp => tp.Team)
            .Include(tp => tp.Match).ThenInclude(m => m.Tournament)
            .Where(tp => !tp.Eliminated && tp.Match.TournamentId == tournament.Id)
            .ToListAsync();

        int roomIndex = 0, teamsInRoom = 0;

        foreach (var teamGroup in survivors.GroupBy(tp => tp.TeamId))
        {
            if (roomIndex >= rooms.Count) return false;

            var oldTeam = teamGroup.First().Team;

            var newTeam = new Team 
            {
                Name  = oldTeam.Name,
                Score = 0
            };
            _context.Teams.Add(newTeam);
            
            await _context.SaveChangesAsync();

            foreach (var tp in teamGroup)
            {
                await _context.TeamPlayers.AddAsync(new TeamPlayer
                {
                    UserId  = tp.UserId,
                    MatchId = rooms[roomIndex].Id,
                    TeamId  = newTeam.Id,
                    Eliminated = false,  
                    Reward = tp.Reward 
                });
            }

            teamsInRoom++;
            if (teamsInRoom == 2)
            {
                roomIndex++;
                teamsInRoom = 0;
            }
        }
    }

    await _context.SaveChangesAsync();
    return true;
    }



    [HttpPost]
    public async Task<IActionResult> EndTournament(int tournamentId)
    {
        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentId);
        if (tournament == null)
        {
            return Json(new { success = false, message = "Tournament not found." });
        }
        if (await GetLoggedUserAsync() != tournament.Host)
        {
            return Json(new { success = false, message = "You are not the host of this tournament." });
        }
        var lastMatch = await _context.Matches
            .Include(m => m.Tournament)
            .FirstOrDefaultAsync(m => m.Tournament == tournament && m.Stage == tournament.Stage);
        if (lastMatch == null)
        {
            return Json(new { success = false, message = "No ongoing match for the current stage." });
        }
        var tournamentMatch = await _context.Matches
            .FirstOrDefaultAsync(m => m.Tournament == tournament);
        if (tournamentMatch == null)
        {
            return Json(new { success = false, message = "No matches found for the tournament." });
        }
        if (lastMatch.Stage <= 0)
        {
            return Json(new { success = false, message = "Last match did not end." });
        }
        tournament.IsActive = false;
        if (tournamentMatch.Mode == "single")
        {
            var winnerList = await _context.Players
                .Include(p => p.Match)
                .Where(p => p.Match.Tournament == tournament && p.Match.Stage == tournament.Stage)
                .ToListAsync();
            var playerList = await _context.Players
                .Include(p => p.Match)
                .Include(p => p.User)
                .Where(p => p.Match.Tournament == tournament && !p.Eliminated)
                .ToListAsync();
            foreach (var player in playerList)
            {
                if (!winnerList.Contains(player))
                {
                    player.Eliminated = true;
                    player.User.RespectPoints += player.Reward;
                }
                else
                {
                    player.Reward += player.Reward * 2;
                }
            }
        }
        else if (tournamentMatch.Mode == "team")
        {
            var roomsInStage = await _context.Matches
                .Where(m => m.TournamentId == tournament.Id && m.Stage == tournament.Stage)
                .ToListAsync();
            foreach (var room in roomsInStage)
            {
                var maxScoreInRoom = await _context.TeamPlayers
                    .Where(tp => tp.MatchId == room.Id)
                    .MaxAsync(tp => tp.Team.Score);
                var winningTeamsInRoom = await _context.TeamPlayers
                    .Include(tp => tp.Team)
                    .Where(tp => tp.MatchId == room.Id && tp.Team.Score == maxScoreInRoom)
                    .Select(tp => tp.Team)
                    .Distinct()
                    .ToListAsync();
                var allPlayersInRoom = await _context.TeamPlayers
                    .Include(tp => tp.User)
                    .Include(tp => tp.Team)
                    .Where(tp => tp.MatchId == room.Id)
                    .ToListAsync();
                foreach (var player in allPlayersInRoom)
                {
                    if (!winningTeamsInRoom.Contains(player.Team))
                    {
                        player.Eliminated = true;
                        player.User.RespectPoints += player.Reward;
                    }
                    else
                    {
                        player.Reward += player.Reward * 2;
                    }
                }
            }
        }
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Tournament ended successfully." });
    }     
  }
}

