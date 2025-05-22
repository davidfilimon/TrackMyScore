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
    public async Task<IActionResult> Create(string name, int playerNumber, int roomCount, string reward, string location, DateOnly startDate, TimeOnly startTime, int gameId, string mode)
    { // method for creating tournaments

      var startDateTime = startDate.ToDateTime(startTime); // converting date and time to datetime
      var host = await GetLoggedUserAsync();
      var code = GenerateTournamentCode();
      var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
      int currentTournamentId = -1;

      var existingCode = await _context.Tournaments.FirstOrDefaultAsync(c => c.Code == code);
      while (existingCode != null)
      {
        code = GenerateTournamentCode();
        existingCode = await _context.Tournaments.FirstOrDefaultAsync(c => c.Code == code);
      }

      if (mode == "single")
      {

        var newTournament = new Tournament // single mode tournament
        {
          Name = name,
          MaxPlayers = 0,
          Reward = reward,
          StartDate = startDateTime,
          Code = code,
          RoomCount = roomCount,
          IsActive = false,
          Game = game,
          Type = mode,
          Host = host,
          Location = location,
          Winner = "",
          Stage = 1
        };

        await _context.Tournaments.AddAsync(newTournament);

        for (int i = 1; i <= roomCount; i++)
        { // creating rooms for single mode tournament
          var newRoom = new Room
          {
            Name = "Room " + i + " - Tournament: " + name,
            Host = host,
            Game = game,
            Tournament = newTournament,
            Location = location,
            Stage = -1,
            Mode = "single",
            Type = mode,
          };
          await _context.Rooms.AddAsync(newRoom);
        }
        await _context.SaveChangesAsync();
        return RedirectToAction("CurrentTournament", "Tournament", new { id = newTournament.Id });


      }
      else if (mode == "team")
      { // team mode tournament
        var newTournament = new Tournament
        {
          Name = name,
          MaxPlayers = playerNumber,
          Reward = reward,
          StartDate = startDateTime,
          RoomCount = roomCount,
          Code = code,
          Game = game,
          IsActive = false,
          Type = mode,
          Host = host,
          Winner = "",
          Location = location,
          Stage = 1
        };

        await _context.Tournaments.AddAsync(newTournament);

        for (int i = 1; i <= roomCount; i++)
        { // creating rooms for team mode tournament
          var newRoom = new Room
          {
            Name = $"Room {i} - Tournament: {name} - Stage 1",
            Host = host,
            Game = game,
            Tournament = newTournament,
            Stage = -1,
            Mode = "team",
            Type = mode,
            StartDate = startDateTime,
            Location = location
          };
          await _context.Rooms.AddAsync(newRoom);
        }
        await _context.SaveChangesAsync(); // saving changes

        return RedirectToAction("CurrentTournament", "Tournament", new { id = newTournament.Id }); // redirecting to newly created tournament view
      }

      return NotFound(); // returns not found if doesnt create the tournament
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {

      var user = await GetLoggedUserAsync();
      var tournaments = await _context.Tournaments
          .Include(t => t.Host)
          .Include(t => t.Game)
          .Where(t => t.Host.Id != user.Id)
          .ToListAsync();

      var myTournaments = await _context.Tournaments
          .Include(t => t.Host)
          .Include(t => t.Game)
          .Where(t => t.Host.Id == user.Id)
          .ToListAsync();

      TournamentListView tournamentList = new TournamentListView(tournaments, myTournaments);

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
      var user = await GetLoggedUserAsync();
      var mutualFollowers = await GetMutualFollowersAsync(user);
      List<Team> teams = null;

      var tournament = await _context.Tournaments
          .Include(t => t.Host)
          .Include(t => t.Game)
          .FirstOrDefaultAsync(t => t.Id == id);

      if (tournament == null)
      {
        return NotFound();
      }

      var players = await _context.Players
          .Where(p => p.Tournament.Id == id)
          .Include(p => p.User)
          .Include(p => p.Team)
          .ToListAsync();

      if (tournament.Type == "team")
      {
        foreach (var player in players)
        {
          teams = await _context.Players
              .Where(p => p.Tournament.Id == id)
              .Select(p => p.Team)
              .Distinct()
              .ToListAsync();
        }
      }

      var rooms = await _context.Rooms
          .Where(r => r.Tournament.Id == id)
          .ToListAsync();

      var matches = await _context.Matches
          .Include(r => r.Room)
          .Where(m => m.Room.Tournament.Id == id)
          .ToListAsync();

      var model = new TournamentModel(user, tournament, players, rooms, teams, mutualFollowers, matches);
      return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> JoinSingle(int id, string code)
    {
      var user = await GetLoggedUserAsync();

      if (user == null)
        return RedirectToAction("Login", "Account");

      var tournament = await _context.Tournaments
          .Include(t => t.Game)
          .FirstOrDefaultAsync(t => t.Id == id);

      if (tournament == null)
      {
        return Json(new { success = false, message = "Tournament not found." });
      }

      if (string.IsNullOrEmpty(code) || tournament.Code != code)
      {
        return Json(new { success = false, message = "Invalid tournament code." });
      }

      var existingPlayer = await _context.Players
          .AnyAsync(p => p.Tournament.Id == id && p.User.Id == user.Id);
      if (existingPlayer)
      {
        return Json(new { success = false, message = "You already joined this tournament." });
      }

      if (tournament.Type == "single")
      {
        var player = new Player
        {
          User = user,
          Tournament = tournament,
          Eliminated = false,
          RespectPoints = 1
        };
        _context.Players.Add(player);
      }

      await _context.SaveChangesAsync();
      return Json(new { success = true, message = "Joined successfully" });
    }

    [HttpPost]
    public async Task<IActionResult> JoinTeam(int id, string code, List<int>? teammates, string? teamName)
    {

      var user = await GetLoggedUserAsync();

      if (user == null)
        return RedirectToAction("Login", "Account");

      var tournament = await _context.Tournaments
        .Include(t => t.Game)
        .FirstOrDefaultAsync(t => t.Id == id);

      if (tournament == null)
      {
        return Json(new { success = false, message = "Tournament not found." });
      }

      if (teammates.Count + 1 > tournament.MaxPlayers)
      {
        return Json(new { success = false, message = "The number of teammates is bigger than the maximum number of players allowed per team." });
      }

      if (string.IsNullOrEmpty(code) || tournament.Code != code)
      {
        return Json(new { success = false, message = "Invalid tournament code." });
      }

      var existingPlayer = await _context.Players
        .AnyAsync(p => p.Tournament.Id == id && p.User.Id == user.Id);
      if (existingPlayer)
      {
        return Json(new { success = false, message = "You already joined this tournament." });
      }

      foreach (var teammate in teammates)
      {
        var alreadyJoined = await _context.Players
          .AnyAsync(p => p.Tournament.Id == id && p.User.Id == teammate);

        if (alreadyJoined)
        {
          return Json(new { success = false, message = "One of your teammates already joined the tournament." });
        }
      }

      if (teammates.Count > tournament.MaxPlayers)
      {
        return Json(new { success = false, message = "The chosen team exceeds the player limit set by the tournament. " });
      }

      if (tournament.Type == "team")
      {
        if (string.IsNullOrEmpty(teamName) || teammates == null || !teammates.Any())
        {
          return Json(new { success = false, message = "Team name and at least one teammate required." });
        }

        var team = new Team { Name = teamName };
        _context.Teams.Add(team);

        var allMembers = new List<User> { user };
        var teammateUsers = await _context.Users
          .Where(u => teammates.Contains(u.Id))
          .ToListAsync();
        allMembers.AddRange(teammateUsers);

        foreach (var member in allMembers)
        {
          _context.Players.Add(new Player
          {
            User = member,
            Tournament = tournament,
            Team = team,
            Eliminated = false,
            RespectPoints = 1
          });
        }
      }

      await _context.SaveChangesAsync();
      return Json(new { success = true, message = "Joined successfully" });
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
        return NotFound();
      }

      if (tournament.Type == "single")
      { // removing the player
        var player = await _context.Players
          .Include(p => p.User)
          .FirstOrDefaultAsync(p => p.Tournament == tournament && p.User.Id == user.Id);

        if (player == null)
        {
          return NotFound();
        }

        _context.Players.Remove(player);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Left tournament." });
      }

      if (tournament.Type == "team")
      { // removing the team of the player and the teammates
        var player = await _context.Players
          .Include(p => p.Tournament)
          .Include(p => p.User)
          .Include(p => p.Team)
          .FirstOrDefaultAsync(p => p.Tournament.Id == id && p.User.Id == user.Id);

        if (player == null)
        {
          return Json(new { success = false, message = "Player not found." });
        }

        var team = await _context.Teams
          .FirstOrDefaultAsync(t => t.Id == player.Team.Id);

        if (team == null)
        {
          return Json(new { success = false, message = "Team not found." });
        }

        var teamPlayers = await _context.Players
          .Where(p => p.Tournament.Id == id && p.Team.Id == team.Id)
          .ToListAsync();

        foreach (var p in teamPlayers)
        {
          _context.Players.Remove(p);
        }

        _context.Teams.Remove(team);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Left tournament." });
      }

      return Json(new { success = false, message = "Tournament not found or you are not taking part in it." });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    { // delete tournament

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

      if (!tournament.IsActive && tournament.Winner != "")
      {
        return Json(new { success = false, message = "Tournament already ended." });
      }

      var players = await _context.Players
          .Where(p => p.Tournament.Id == id)
          .ToListAsync();

      var rooms = await _context.Rooms
          .Where(r => r.Tournament.Id == id)
          .ToListAsync();

      foreach (var player in players)
      {
        _context.Players.Remove(player);
      }

      foreach (var room in rooms)
      {
        _context.Rooms.Remove(room);
      }

      _context.Tournaments.Remove(tournament);
      await _context.SaveChangesAsync();
      return Json(new { success = true, message = "Tournament deleted." });
    }

    [HttpPost]
    public async Task<IActionResult> Start(int id)
    { // start tournament

      var tournament = await _context.Tournaments
          .Include(t => t.Host)
          .FirstOrDefaultAsync(t => t.Id == id);
      var user = await GetLoggedUserAsync();

      if (tournament == null)
      {
        return Json(new { success = false, message = "Tournament not found." });
      }

      if (tournament.Host != user)
      {
        return Json(new { success = false, message = "You are not the host of this tournament." });
      }

      var success = await StartStage(tournament);

      if (!success)
      {
        return Json(new { success = true, message = "Not enough rooms to distribute players or teams. " });
      }

      tournament.IsActive = true;

      await _context.SaveChangesAsync();
      return Json(new { success = true, message = "Tournament started." });
    }

    [HttpPost]
    public async Task<IActionResult> EndStage(int id)
    {

      var tournament = await _context.Tournaments
        .Include(t => t.Game)
        .Include(t => t.Host)
        .FirstOrDefaultAsync(t => t.Id == id);

      if (tournament == null)
      {
        return Json(new { success = false, message = "Tournament not found." });
      }

      if (tournament.Type == "single")
      {
        var winnerList = await _context.Matches
          .Include(m => m.Room)
          .Where(m => m.Room.Tournament == tournament && m.Room.Stage == tournament.Stage)
          .Select(m => m.Winner)
          .ToListAsync();

        var playerList = await _context.Players
          .Where(p => p.Tournament == tournament && !p.Eliminated)
          .Include(p => p.User)
          .ToListAsync();

        foreach (var player in playerList)
        {
          if (!winnerList.Contains(player.User.Username))
          {

            player.Eliminated = true;
            player.User.RespectPoints += player.RespectPoints;

          }
          else
          {

            player.RespectPoints *= 2;

          }
        }
      }
      else if (tournament.Type == "team")
      {
        var winnerList = await _context.Matches
          .Include(m => m.Room)
          .Where(m => m.Room.Tournament == tournament && m.Room.Stage == tournament.Stage)
          .Select(m => m.Winner)
          .ToListAsync();

        var teamList = await _context.Players
          .Include(p => p.Team)
          .Where(p => p.Tournament == tournament && !p.Eliminated && p.Team != null)
          .Select(p => p.Team)
          .Distinct()
          .ToListAsync();

        foreach (var team in teamList)
        {

          var players = await _context.Players
              .Where(p => p.Tournament == tournament && p.Team == team)
              .Include(p => p.User)
              .ToListAsync();

          if (!winnerList.Contains(team.Name))
          {
            foreach (var player in players)
            {
              player.Eliminated = true;
              player.User.RespectPoints += player.RespectPoints;
            }

          }
          else
          {

            foreach (var player in players)
            {
              player.RespectPoints *= 2;
            }

          }
        }
      }

      for (int i = 0; i < tournament.RoomCount / (int)Math.Pow(2, tournament.Stage); i++)
      {

        var room = new Room
        {
          Name = $"Room {i} - Tournament: {tournament.Name} - Stage {tournament.Stage + 1}",
          Location = tournament.Location,
          Type = tournament.Type,
          StartDate = tournament.StartDate,
          Stage = -1, // inactive rooms for the next stage
          Mode = tournament.Type,
          Host = tournament.Host,
          Game = tournament.Game,
          Tournament = tournament
        };
        await _context.Rooms.AddAsync(room);
      }

      await _context.SaveChangesAsync();

      var success = await StartStage(tournament);

      if (!success)
      {
        return Json(new { success = false, message = "Not enough rooms to distribute players or teams." });
      }

      tournament.Stage += 1;

      await _context.SaveChangesAsync();

      return Json(new { success = true, message = "Stage ended successfully." });

    }
    private async Task<bool> StartStage(Tournament tournament)
    {
      if (tournament.Type == "single")
      {
        var rooms = await _context.Rooms
            .Where(r => r.Tournament.Id == tournament.Id && r.Stage == -1)
            .ToListAsync();

        var players = await _context.Players
            .Include(p => p.User)
            .Where(p => !p.Eliminated && p.Tournament == tournament)
            .ToListAsync();

        int roomIndex = 0, playersInRoom = 0;

        foreach (var player in players)
        {
          if (roomIndex >= rooms.Count) return false;

          await _context.JoinRooms.AddAsync(new JoinRoom
          {
            User = player.User,
            Room = rooms[roomIndex]
          });

          playersInRoom++;

          if (playersInRoom == 2)
          {
            roomIndex++;
            playersInRoom = 0;
          }
        }
      }
      else if (tournament.Type == "team")
      {
        var rooms = await _context.Rooms
            .Where(r => r.Tournament.Id == tournament.Id && r.Stage == -1)
            .ToListAsync();

        var teams = await _context.Players
            .Include(p => p.Team)
            .Where(p => p.Tournament == tournament && !p.Eliminated && p.Team != null)
            .Select(p => p.Team)
            .Distinct()
            .ToListAsync();

        int roomIndex = 0, teamsInRoom = 0;

        foreach (var team in teams)
        {
          if (roomIndex >= rooms.Count) return false;

          var players = await _context.Players
              .Include(p => p.User)
              .Where(p => p.Team == team && p.Tournament == tournament)
              .ToListAsync();

          foreach (var player in players)
          {
            await _context.JoinRooms.AddAsync(new JoinRoom
            {
              User = player.User,
              Room = rooms[roomIndex],
              Team = player.Team
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

      return true;
    }

    [HttpPost]
    public async Task<IActionResult> EndTournament(int tournamentId)
    {

      var tournament = await _context.Tournaments
        .FirstOrDefaultAsync(t => t.Id == tournamentId);

      var lastMatch = await _context.Matches
        .Include(m => m.Room)
          .ThenInclude(r => r.Tournament)
        .FirstOrDefaultAsync(m => m.Room.Tournament == tournament && m.Room.Stage == tournament.Stage);

      if (lastMatch.Winner == null)
      {
        return Json(new { success = false, message = "Last match did not end." });
      }

      tournament.Winner = lastMatch.Winner;
      tournament.IsActive = false;

      if (tournament.Type == "single")
      {
        var winnerList = await _context.Matches
          .Include(m => m.Room)
          .Where(m => m.Room.Tournament == tournament && m.Room.Stage == tournament.Stage)
          .Select(m => m.Winner)
          .ToListAsync();

        var playerList = await _context.Players
          .Where(p => p.Tournament == tournament && !p.Eliminated)
          .Include(p => p.User)
          .ToListAsync();

        foreach (var player in playerList)
        {
          if (!winnerList.Contains(player.User.Username))
          {

            player.Eliminated = true;
            player.User.RespectPoints += player.RespectPoints;

          }
          else
          {

            player.User.RespectPoints = player.RespectPoints * 2;
          }
        }
      }
      else if (tournament.Type == "team")
      {
        var winnerList = await _context.Matches
          .Include(m => m.Room)
          .Where(m => m.Room.Tournament == tournament && m.Room.Stage == tournament.Stage)
          .Select(m => m.Winner)
          .ToListAsync();

        var teamList = await _context.Players
          .Include(p => p.Team)
          .Where(p => p.Tournament == tournament && !p.Eliminated && p.Team != null)
          .Select(p => p.Team)
          .Distinct()
          .ToListAsync();

        foreach (var team in teamList)
        {

          var players = await _context.Players
              .Where(p => p.Tournament == tournament && p.Team == team)
              .Include(p => p.User)
              .ToListAsync();

          if (!winnerList.Contains(team.Name))
          {
            foreach (var player in players)
            {
              player.Eliminated = true;
              player.User.RespectPoints += player.RespectPoints;
            }

          }
          else
          {

            foreach (var player in players)
            {
              player.User.RespectPoints = player.RespectPoints * 2;
            }

          }

        }

      }
      await _context.SaveChangesAsync();
      return Json(new { success = true, message = "Tournament ended successfully." });
    }
       
    }
    // tournament start, end, brackets = room / 2 // dont edit this
}

