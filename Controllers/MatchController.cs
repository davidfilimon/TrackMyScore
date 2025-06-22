using System.Threading.Tasks.Dataflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackMyScore.Database;
using TrackMyScore.Models;
using TrackMyScore.Services;

namespace TrackMyScore.Controllers
{
    [Route("[controller]/[action]/{id?}")]
    public class MatchController : Controller
    {

        private readonly AppDbContext _context;
        private readonly UserService _userService;

        public MatchController(AppDbContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        private async Task<User> GetLoggedUserAsync()
        {
            string email = HttpContext.Session.GetString("email");

            if (email == null)
            {
                return null;
            }

            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var user = await GetLoggedUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Account");

            // matches the user hosts
            var hostedMatches = await _context.Matches
                .Include(m => m.Host)
                .Include(m => m.Game)
                .Where(m =>
                    m.Host.Id == user.Id &&
                    m.Tournament == null &&
                    m.Stage >= -1)
                .ToListAsync();

            // matches the user joined as a single player
            var joinsSingle = await _context.Players
                .Include(j => j.Match).ThenInclude(m => m.Host)
                .Where(j =>
                    j.Match.Tournament == null &&
                    j.User.Id == user.Id &&
                    j.Match.Host.Id != user.Id &&
                    j.Match.Stage >= -1)
                .Select(j => j.Match)
                .ToListAsync();

            // matches the user joined as part of a team
            var joinsTeam = await _context.TeamPlayers
                .Include(j => j.Match).ThenInclude(m => m.Host)
                .Where(j =>
                    j.Match.Tournament == null &&
                    j.User.Id == user.Id &&
                    j.Match.Host.Id != user.Id &&
                    j.Match.Stage >= -1)
                .Select(j => j.Match)
                .ToListAsync();

            // unify joinedMatches
            var joinedMatches = joinsSingle
                .Concat(joinsTeam)
                .GroupBy(m => m.Id)
                .Select(g => g.First())
                .ToList();

            // all the users matches
            var myMatches = hostedMatches
                .Concat(joinedMatches)
                .GroupBy(m => m.Id)
                .Select(g => g.First())
                .ToList();

            // all other available matches
            var allOthers = await _context.Matches
                .Include(m => m.Host)
                .Include(m => m.Game)
                .Where(m =>
                    m.Host.Id != user.Id &&
                    m.Tournament == null &&
                    m.Stage >= -1)
                .ToListAsync();

            var availableMatches = allOthers
                .Where(m => myMatches.All(mm => mm.Id != m.Id))
                .ToList();

            // count solo players per match
            var playerSingleList = await _context.Players
                .Where(p => p.Match.Stage >= -1)
                .GroupBy(p => p.MatchId)
                .Select(g => new {
                    MatchId = g.Key,
                    PlayerCount = g.Count()
                })
                .ToListAsync();

            // count distinct teams per match
            var teamCountsPerMatch = await _context.TeamPlayers
                .Where(tp => tp.Match.Stage >= -1)
                .GroupBy(tp => new { tp.MatchId, tp.TeamId })
                .Select(g => g.Key.MatchId) // one entry per (MatchId, TeamId)
                .GroupBy(matchId => matchId) // now group by MatchId
                .Select(g => new {
                    MatchId = g.Key,
                    TeamCount = g.Count() // number of distinct teams
                })
                .ToListAsync();

            var playerCountDict = playerSingleList
                .Concat(teamCountsPerMatch.Select(t => new {
                    MatchId = t.MatchId,
                    PlayerCount = t.TeamCount
                }))
                .GroupBy(x => x.MatchId)
                .ToDictionary(
                    g => g.Key,
                    g => (int?)g.Sum(x => x.PlayerCount)
                );

            var model = new MatchListModel
            {
                HostedMatches = hostedMatches,
                JoinedMatches = joinedMatches,
                AvailableMatches = availableMatches,
                PlayerCount = playerCountDict   // Dictionary<int, int?> 
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
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

            ViewBag.User = user;

            return View(games);
        }

        [HttpGet]
        public async Task<IActionResult> CurrentMatch(int id)
        {
            var match = await _context.Matches
                .Include(r => r.Game)
                .Include(r => r.Host)
                .Include(r => r.Tournament)
                .FirstOrDefaultAsync(r => r.Id == id);

            var user = await GetLoggedUserAsync();

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (match == null)
            {
                return RedirectToAction("List", "Match");
            }

            List<TeamPlayer> teamPlayers = new();
            List<Player> players = new();
            List<Team> teams = new();
            bool participant = false;

            if (match.Mode == "single")
            {
                players = await _context.Players
                    .Where(p => p.MatchId == match.Id)
                    .Include(p => p.User)
                    .GroupBy(p => p.UserId)
                    .Select(g => g.First())
                    .ToListAsync();

                foreach (var p in players)
                {
                    if (p.UserId == user.Id)
                    {
                        participant = true;
                        break;
                    }
                }
            }
            else if (match.Mode == "team")
            {
                teamPlayers = await _context.TeamPlayers
                    .Where(p => p.MatchId == match.Id)
                    .Include(p => p.User)
                    .Include(p => p.Team)
                    .GroupBy(p => new { p.UserId, p.TeamId })
                    .Select(g => g.First())
                    .ToListAsync();

                teams = await _context.TeamPlayers
                    .Where(p => p.MatchId == match.Id)
                    .Select(p => p.Team)
                    .Distinct()
                    .ToListAsync();

                foreach (var tp in teamPlayers)
                {
                    if (tp.UserId == user.Id)
                    {
                        participant = true;
                        break;
                    }
                }
            }

            var model = new CurrentMatchModel
            {
                Match = match,
                LoggedUser = user,
                TeamPlayers = teamPlayers,
                Players = players,
                Participant = participant,
                Teams = teams
            };
            return View(model);
        }



        [HttpPost]
        public async Task<IActionResult> Join(int id)
        {
            var user = await GetLoggedUserAsync();

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var match = await _context.Matches.FirstOrDefaultAsync(r => r.Id == id);

            if (match == null)
            {
                return Json(new { success = false, message = "Match not found." });
            }

            if (match.Stage != -1)
            {
                return Json(new { success = false, message = "Match already started or ended." });
            }

            var placeholderTeam = await _context.Teams.FirstOrDefaultAsync(t => t.Score == -100);

            if (placeholderTeam == null)
            {
                return Json(new { success = false, message = "The placeholder team could not be found. As an administrator add the placeholder team in the database." });
            }

            if(await joinedMatchCount(user) > 5){
                return Json(new { success = false, message = "You are taking part in 5 matches already. End those matches in order to join another one." });
            }

            if (match.Mode == "single")
            {
                await _context.Players
                    .AddAsync(new Player
                    {
                        User = user,
                        Match = match,
                        Score = 0,
                        Eliminated = false,
                        Reward = 1
                    });
            }
            else if (match.Mode == "team")
            {
                await _context.TeamPlayers
                    .AddAsync(new TeamPlayer
                    {
                        User = user,
                        Match = match,
                        Eliminated = false,
                        Reward = 1,
                        Team = placeholderTeam
                    });
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Successfully joined the match." });

        }

        [HttpPost]
        public async Task<IActionResult> Leave(int id)
        {
            var user = await GetLoggedUserAsync();

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var match = await _context.Matches.FirstOrDefaultAsync(r => r.Id == id);

            if (match == null)
            {
                return Json(new { success = false, message = "Match not found." });
            }

            if (match.Stage != -1)
            {
                return Json(new { success = false, message = "Match already started or ended." });
            }

            if (match.Mode == "single")
            {
                var joinMatch = await _context.Players
                    .FirstOrDefaultAsync(j => j.Match.Id == id && j.User.Id == user.Id);

                if (joinMatch != null)
                {
                    _context.Players.Remove(joinMatch);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Successfully left the Match!" });

                }

            }
            else if (match.Mode == "team")
            {
                var joinMatch = await _context.TeamPlayers
                    .FirstOrDefaultAsync(j => j.Match.Id == id && j.User.Id == user.Id);
            }

            return Json(new { success = false, message = "Failed to leave the Match." });

        }

        [HttpPost]
        public async Task<IActionResult> Create(string name, string mode, DateOnly startDate, TimeOnly startTime, int gameId)
        {

            if(name.Length > 50){
                return Json(new { success = false, message = "The match's name is too long, please choose a shorter one." });
            } 

            var user = await GetLoggedUserAsync();

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if(await joinedMatchCount(user) > 5){
                return Json(new { success = false, message = "You are taking part in 5 matches already. End those matches in order to join another one." });
            } 

            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
            {
                return View();
            }

            var placeholderTeam = await _context.Teams.FirstOrDefaultAsync(t => t.Score == -100);

            if (placeholderTeam == null)
            {
                return Json(new { success = false, message = "Placeholder team not found. Contact an administrator to create one." });
            }

            DateTime s = startDate.ToDateTime(startTime);

            var match = new Match
            {
                Name = name,
                StartDate = s,
                StopDate = null,
                Stage = -1,
                Mode = mode,
                Host = user,
                Game = game,
                Tournament = null
            };

            await _context.Matches.AddAsync(match);

            if (match.Mode == "single")
            {
                _context.Players.Add(new Player
                {
                    User = user,
                    Match = match,
                    Score = 0,
                    Eliminated = false,
                    Reward = 1
                });
            }
            else if (match.Mode == "team")
            {
                _context.TeamPlayers.Add(new TeamPlayer
                {
                    User = user,
                    Match = match,
                    Team = placeholderTeam,
                    Eliminated = false,
                    Reward = 1
                });
            }
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Match successfully created.", matchId = match.Id });

        }

        [HttpPost]
        public async Task<IActionResult> Start(int id)
        {
            var match = await _context.Matches
                .Include(r => r.Game)
                .Include(r => r.Host)
                .FirstOrDefaultAsync(r => r.Id == id);

            var user = await GetLoggedUserAsync();

            if (match == null)
                return Json(new { success = false, message = "Match not found." });

            if (match.Host != user)
                return Json(new { success = false, message = "You are not the host of this room." });

            if (match.Mode == "single")
            {
                var players = await _context.Players
                    .CountAsync(p => p.MatchId == id);

                if (players < 2) return Json(new { success = false, message = "Not enough players to start." });

            }
            else
            {
                var teams = await _context.TeamPlayers
                    .Where(tp => tp.MatchId == id)
                    .Select(tp => tp.TeamId)
                    .Distinct()
                    .CountAsync();
                if (teams < 2) return Json(new { success = false, message = "Not enough teams to start." });
            }

            match.Stage = 0;
            match.StartDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Match successfully started." });

        }

        [HttpPost]
        public async Task<IActionResult> End(int id)
        {
            var match = await _context.Matches
                .Include(r => r.Tournament)
                .Include(r => r.Host)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null)
            {
                return Json(new { success = false, message = "Match not found" });
            }

            var user = await GetLoggedUserAsync();

            if (user != match.Host)
            {
                return Json(new { success = false, message = "You are not the host of this room." });
            }

            if (match.Mode == "single")
            {
                var playersInMatch = await _context.Players
                    .Where(p => p.MatchId == match.Id)
                    .ToListAsync();

                var maxScore = playersInMatch.Max(p => p.Score);

                var winners = playersInMatch.Where(p => p.Score == maxScore).ToList();
                if (winners.Count > 1)
                {
                    return Json(new { success = false, message = "There can only be one winner." });
                }

                foreach (var player in playersInMatch)
                {
                    if (player.Score < maxScore)
                    {
                        player.Eliminated = true;
                    }
                }
            }
            else // team mode
            {
                var teamPlayersInMatch = await _context.TeamPlayers
                    .Include(tp => tp.Team)
                    .Where(tp => tp.MatchId == match.Id)
                    .ToListAsync();

                var maxScore = teamPlayersInMatch
                    .Select(tp => tp.Team.Score)
                    .Max();

                var winningTeamIds = teamPlayersInMatch
                    .Where(tp => tp.Team.Score == maxScore)
                    .Select(tp => tp.TeamId)
                    .Distinct()
                    .ToList();

                if (winningTeamIds.Count > 1)
                {
                    return Json(new { success = false, message = "There can only be one winner." });
                }

                foreach (var tp in teamPlayersInMatch)
                {
                    if (tp.Team.Score < maxScore)
                    {
                        tp.Eliminated = true;
                    }
                }
            }

            match.StopDate = DateTime.Now;

            if (match.Tournament == null)
            {
                match.Stage = -2;
            }
            else
            {
                match.Stage = match.Tournament.Stage;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Match successfully ended." });
        }


        [HttpPost]
        public async Task<IActionResult> Delete(int id) // function for deleting a match
        {
            var match = await _context.Matches
                    .Include(r => r.Host)
                    .FirstOrDefaultAsync(r => r.Id == id);

            var user = await GetLoggedUserAsync();

            if (match == null)
            {
                return Json(new { success = false, message = "Match not found." }); // checking the existence of the match
            }

            if (match.Tournament != null)
            {
                return Json(new { success = false, message = "Match cannot be deleted since it is part of a tournament." }); // checking if the match is part of a tournament
            }

            if (match.Host != user)
            {
                return Json(new { success = false, message = "You are not the host of this match." }); // checking if the user is the match host
            }

            if (match.Mode == "single") // removing the players from the match
            {
                var joinedPlayers = await _context.Players
                    .Where(j => j.Match.Id == id)
                    .ToListAsync();
                if (joinedPlayers.Any() && joinedPlayers != null)
                {
                    foreach (var player in joinedPlayers)
                    {
                        _context.Players.Remove(player);
                    }
                }
            }
            else if (match.Mode == "team") // removing the team players from the match
            {
                var joinedPlayers = await _context.TeamPlayers
                    .Where(j => j.Match.Id == id)
                    .Include(j => j.Team)
                    .ToListAsync();

                var teams = await _context.TeamPlayers
                    .Where(j => j.Match.Id == match.Id)
                    .Distinct()
                    .Select(t => t.Team)
                    .ToListAsync();

                if (joinedPlayers.Any() && joinedPlayers != null)
                {
                    foreach (var player in joinedPlayers)
                    {
                        _context.TeamPlayers.Remove(player);
                    }
                }

                foreach (var t in teams)
                {
                    if (t.Score != -100) // removing all the teams from the room except the placeholder one
                    {
                        _context.Teams.Remove(t);
                    }
                }
            }

            _context.Matches.Remove(match);

            await _context.SaveChangesAsync();

            return RedirectToAction("List", "Match");
        }

        [HttpPost]
        public async Task<IActionResult> AddPoint(int matchId, int participantId)
        {
            var match = await _context.Matches
                .Include(m => m.Host)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            var user = await GetLoggedUserAsync();

            if (match == null)
                return Json(new { success = false, message = "Match not found." });

            if (match.Stage != 0)
            {
                return Json(new { success = false, message = "The match is not being played." });
            }

            if (match.Host != user)
            {
                return Json(new { success = false, message = "You are not the host of this room." });
            }

            if (match.Mode == "single")
            {
                var player = await _context.Players
                                .Include(p => p.Match)
                                .FirstOrDefaultAsync(p => p.Id == participantId && p.MatchId == match.Id);

                if (player == null)
                {
                    return Json(new { success = false, message = "Player not found." });
                }

                if (player.Score < 100)
                {
                    player.Score += 1;
                }

            }
            else if (match.Mode == "team")
            {
                var team = await _context.Teams
                                .FirstOrDefaultAsync(p => p.Id == participantId);

                if (team == null)
                {
                    return Json(new { success = false, message = "Team not found." });
                }

                if (team.Score < 100)
                {
                    team.Score += 1;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Point added successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> RemovePoint(int id, int participantId)
        {
            var match = await _context.Matches
                .Include(m => m.Host)
                .FirstOrDefaultAsync(m => m.Id == id);

            var user = await GetLoggedUserAsync();

            if (match == null)
                return Json(new { success = false, message = "Match not found." });

            if (match.Stage != 0)
            {
                return Json(new { success = false, message = "The match is not being played." });
            }

            if (match.Host != user)
            {
                return Json(new { success = false, message = "You are not the host of this room." });
            }

            if (match.Mode == "single")
            {
                var player = await _context.Players
                                .Include(p => p.Match)
                                .FirstOrDefaultAsync(p => p.Id == participantId && p.MatchId == match.Id);

                if (player == null)
                {
                    return Json(new { success = false, message = "Player not found." });
                }

                if (player.Score > 0)
                {
                    player.Score -= 1;
                }

            }
            else if (match.Mode == "team")
            {
                var team = await _context.Teams
                                .FirstOrDefaultAsync(p => p.Id == participantId);

                if (team == null)
                {
                    return Json(new { success = false, message = "Team not found." });
                }

                if (team.Score > 0)
                {
                    team.Score -= 1;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Point removed successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> SaveChanges(int roomId, Dictionary<int, string> teamAssignments, List<string> teamNames)
        {
            var user = await GetLoggedUserAsync();
            if (user == null)
                return Json(new { success = false, message = "Not authenticated." });

            var teamPlayers = await _context.TeamPlayers
                .Include(tp => tp.Team)
                .Where(tp => tp.MatchId == roomId)
                .ToListAsync();

            if (!teamPlayers.Any())
                return Json(new { success = false, message = "Match not found or no team-players for this match." });

            var match = await _context.Matches
                .FirstOrDefaultAsync(m => m.Id == roomId);

            if (match == null || match.HostId != user.Id)
                return Json(new { success = false, message = "Match not found or not authorized." });

            var trimmedNames = teamNames
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .ToList();

            if (trimmedNames.Count != trimmedNames.Distinct().Count())
            {
                return Json(new { success = false, message = "Duplicate team names are not allowed." });
            }

            var placeholderTeam = await _context.Teams
                .FirstOrDefaultAsync(t => t.Score == -100);

            if (placeholderTeam == null)
            {
                return Json(new { success = false, message = "Placeholder team not found. Create a placeholder team with Score = -100." });
            }

            var existingTeams = await _context.Teams
                .Where(t => t.Score != -100)
                .ToListAsync();

            var teamsToDelete = existingTeams
                .Where(t => !trimmedNames.Contains(t.Name.Trim()))
                .ToList();

            foreach (var teamToDelete in teamsToDelete)
            {
                var membersToMove = teamPlayers
                    .Where(tp => tp.TeamId == teamToDelete.Id)
                    .ToList();

                foreach (var tp in membersToMove)
                {
                    tp.TeamId = placeholderTeam.Id;
                    tp.Eliminated = false;
                    _context.TeamPlayers.Update(tp);
                }

                bool stillInUse = await _context.TeamPlayers
                    .AnyAsync(tp => tp.TeamId == teamToDelete.Id && tp.MatchId != roomId);

                if (!stillInUse)
                {
                    _context.Teams.Remove(teamToDelete);
                }
            }

            await _context.SaveChangesAsync();

            var teamMap = new Dictionary<string, Team>();
            foreach (var teamName in trimmedNames)
            {
                var newTeam = new Team
                {
                    Name = teamName,
                    Score = 0
                };
                _context.Teams.Add(newTeam);
                await _context.SaveChangesAsync();
                teamMap[teamName] = newTeam;
            }

            foreach (var tp in teamPlayers)
            {
                tp.TeamId = placeholderTeam.Id;
                tp.Eliminated = false;
                _context.TeamPlayers.Update(tp);
            }

            foreach (var assignment in teamAssignments)
            {
                int teamPlayerId = assignment.Key;
                string teamName = assignment.Value?.Trim();

                if (string.IsNullOrEmpty(teamName)) continue;
                if (!teamMap.ContainsKey(teamName)) continue;

                var teamPlayer = teamPlayers.FirstOrDefault(tp => tp.Id == teamPlayerId);
                if (teamPlayer == null) continue;

                teamPlayer.TeamId = teamMap[teamName].Id;
                teamPlayer.Eliminated = false;
                _context.TeamPlayers.Update(teamPlayer);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Teams and assignments saved successfully." });
        }

        private async Task<int> joinedMatchCount(User user) {
            // matches the user joined as a single player
            var joinsSingle = await _context.Players
                .Include(j => j.Match).ThenInclude(m => m.Host)
                .Where(j =>
                    j.Match.Tournament == null &&
                    j.User.Id == user.Id)
                .Select(j => j.Match)
                .Distinct()
                .ToListAsync();

            // matches the user joined as part of a team
            var joinsTeam = await _context.TeamPlayers
                .Include(j => j.Match).ThenInclude(m => m.Host)
                .Where(j =>
                    j.Match.Tournament == null &&
                    j.User.Id == user.Id)
                .Select(j => j.Match)
                .Distinct()
                .ToListAsync();

            // unify joinedMatches
            var joinedMatches = joinsSingle
                .Concat(joinsTeam)
                .GroupBy(m => m.Id)
                .Select(g => g.First())
                .ToList();

            if (joinedMatches.Any())
            {
                return joinedMatches.Count;
            }
            else
            {
                return 0;
            }

        }
    }
}
