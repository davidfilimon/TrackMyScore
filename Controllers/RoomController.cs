using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackMyScore.Database;
using TrackMyScore.Models;
using TrackMyScore.Services;

namespace TrackMyScore.Controllers
{
    [Route("[controller]/[action]/{id?}")]
    public class RoomController : Controller
    {

        private readonly AppDbContext _context;
        private readonly UserService _userService;

        public RoomController(AppDbContext context, UserService userService)
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
        public IActionResult Play()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            User loggedUser = await GetLoggedUserAsync();

            var roomList = await _context.Rooms
                .Where(r => r.Stage == -1 && r.Tournament == null)
                .Where(r => r.Host != loggedUser) 
                .Where(r => !_context.JoinRooms.Any(jr => jr.Room == r && jr.User == loggedUser)) 
                .Include(r => r.Host)
                .Include(r => r.Game)
                .Include(r => r.Tournament)
                .ToListAsync();

            var joinedRooms = await _context.JoinRooms
                .Where(r => r.User == loggedUser && r.Room.Tournament == null)
                .Include(r => r.Room)
                .Include(r => r.User)
                .Include(r => r.Room.Game)
                .Include(r => r.Room.Tournament)
                .ToListAsync();
            
            var joinedPlayers = new Dictionary<Room, List<User>>();

            foreach(var room in joinedRooms){
                var players = await _context.JoinRooms
                    .Where(r => r.Room.Id == room.Room.Id)
                    .Include(r => r.User)
                    .Select(r => r.User)
                    .ToListAsync();
                
                joinedPlayers.Add(room.Room, players);
            }
            

            var roomListModel = new RoomListModel(joinedRooms, roomList, joinedPlayers, joinedPlayers.Count);

            return View(roomListModel);
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
        public async Task<IActionResult> CurrentRoom(int id)
        {
            Room room = await _context.Rooms
                .Include(r => r.Host)
                .Include(r => r.Game)
                .Include(r => r.Tournament)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return NotFound();
            }

            List<User> joinedPlayersList = await _context.JoinRooms
                .Where(j => j.Room == room)
                .Include(j => j.User)
                .Select(j => j.User)
                .ToListAsync();

            var user = await GetLoggedUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userJoined = await _context.JoinRooms
                .AnyAsync(u => u.User.Id == user.Id && u.Room.Id == id);

            var currentMatch = await _context.Matches
                .FirstOrDefaultAsync(m => m.Room.Id == room.Id) ?? null;

            List<Participant> participants = new List<Participant>();

            Dictionary<Team,int>? teams = null;

            if(currentMatch != null)
            {
                participants = await _context.Participants
                    .Where(p => p.Match.Id == currentMatch.Id)
                    .Include(p => p.Team)
                    .ToListAsync();

                teams = await _context.Participants
                    .Where(p => p.Match.Id == currentMatch.Id)
                    .GroupBy(p => p.Team)
                    .Where(g => g.Key != null)
                    .Select(g => new {
                        Team = g.Key,
                        Score = g.First().Score
                    })
                    .ToDictionaryAsync(x => x.Team!, x => x.Score);
            }      

            var model = new CurrentRoomModel
            {
                Room = room,
                Players = joinedPlayersList,
                LoggedUser = user,
                UserJoined = userJoined,
                CurrentMatch = currentMatch,
                Participants = participants,
                Teams = teams,
                JoinedPlayersList = joinedPlayersList
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

            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);

            if (room != null)
            {
                var joinRoom = new JoinRoom
                {
                    Room = room,
                    User = user
                };

                await _context.JoinRooms.AddAsync(joinRoom);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Successfully joined the room!" });
            }

            return Json(new { success = false, message = "Failed to join the room." });

        }

        [HttpPost]
        public async Task<IActionResult> Leave(int id)
        {
            var user = await GetLoggedUserAsync();

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);

            if (room != null)
            {
                var joinRoom = await _context.JoinRooms
                    .FirstOrDefaultAsync(j => j.Room.Id == id && j.User.Id == user.Id);

                if (joinRoom != null)
                {
                    _context.JoinRooms.Remove(joinRoom);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Successfully left the room!" });

                }

            }

            return Json(new { success = false, message = "Failed to leave the room." });

        }

        [HttpPost]
        public async Task<IActionResult> Create(string name, string type, string location, DateOnly startDate, TimeOnly startTime, int gameId, int tournamentId)
        {

            Room room;

            var user = await GetLoggedUserAsync();

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
            {
                return View();
            }

            DateTime s = startDate.ToDateTime(startTime);

            room = new Room
            {
                Name = name,
                Type = type,
                Location = location,
                StartDate = s,
                Stage = -1,
                Mode = "pending",
                Host = user,
                Game = game
            };

            var joinRoom = new JoinRoom
            {
                Room = room,
                User = user
            };

            await _context.Rooms.AddAsync(room);
            await _context.JoinRooms.AddAsync(joinRoom);
            await _context.SaveChangesAsync();

            return RedirectToAction("CurrentRoom", "Room", new { id = room.Id });

        }

        [HttpPost]
        public async Task<IActionResult> Start(int roomId, Dictionary<int, string> teamAssignments, Dictionary<int, string> roles)
        {
            var room = await _context.Rooms
                .Include(r => r.Game)
                .Include(r => r.Host)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null)
                return Json(new { success = false, message = "Room not found." });

            if (room.Host == null)
                return Json(new { success = false, message = "Room host not found." });

            if (teamAssignments == null || roles == null)
                return Json(new { success = false, message = "Team assignments or roles not provided." });

            room.Stage = 0;
            room.Mode = "team";

            var match = new Match
            {
                StartDate = DateTime.Now,
                Room = room,
                Type = room.Mode
            };

            await _context.Matches.AddAsync(match);

            foreach (var assignment in teamAssignments)
            {
                if (string.IsNullOrEmpty(assignment.Value)) continue;

                var player = await _context.Users.FindAsync(assignment.Key);
                if (player == null) continue;

                string role = roles.TryGetValue(assignment.Key, out var r) ? r : "Player";

                Team team = new Team
                {
                    Name = assignment.Value
                };

                Participant participant = new Participant
                {
                    Role = role,
                    Match = match,
                    User = player,
                    Team = team,
                    Score = 0
                };
                await _context.Teams.AddAsync(team);
                await _context.Participants.AddAsync(participant);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Match started." });
        }
        
        [HttpPost]
        public async Task<IActionResult> StartIndividual(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Host)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || room.Host == null)
                return Json(new { success = false });

            var joinedPlayers = await _context.JoinRooms
                .Where(r => r.Room.Id == roomId)
                .Include(j => j.User)
                .ToListAsync();

            if (!joinedPlayers.Any())
                return RedirectToAction("List", "Room");

            var playerIds = joinedPlayers.Select(j => j.User.Id).Append(room.Host.Id).ToList();
            var players = await _context.Users
                .Where(u => playerIds.Contains(u.Id))
                .ToListAsync();

            room.Stage = 0;
            room.Mode = "single";

            var match = new Match
            {
                StartDate = DateTime.Now,
                EndDate = null,
                Room = room,
                Type = room.Type
            };

            await _context.Matches.AddAsync(match);


            foreach (var p in players)
            {
                var participant = new Participant
                {
                    User = p,
                    Match = match,
                    Role = string.Empty,
                    Team = null,
                    Score = 0
                };
                await _context.Participants.AddAsync(participant);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Match started." });
        }

        [HttpPost]
        public async Task<IActionResult> End(int matchId)
        {
            var match = await _context.Matches
                .Include(m => m.Room)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match?.Room == null)
            {
                return Json(new { success = false, message = "Match not found" });
            }

            if (match.Room.Mode == "single")
            {
                var maxScore = await _context.Participants
                    .Where(p => p.Match.Id == match.Id)
                    .Select(p => p.Score)
                    .MaxAsync();

                var winners = await _context.Participants
                    .Where(p => p.Match.Id == match.Id && p.Score == maxScore)
                    .Include(p => p.User)
                    .ToListAsync();

                if (winners == null || winners.Count == 0)
                {
                    return Json(new { success = false, message = "No winners found" });
                }

                if (winners.Count > 1)
                {
                    return Json(new { success = false, message = "There is a tie between players." });
                }

                match.Winner = winners[0].User?.Username;
            }
            else if (match.Room.Mode == "team")
            {
                var maxScore = await _context.Participants
                    .Where(p => p.Match.Id == match.Id)
                    .Select(p => p.Score)
                    .MaxAsync();

                var winningTeams = await _context.Participants
                    .Where(p => p.Match.Id == match.Id && p.Score == maxScore && p.Team != null)
                    .Select(p => p.Team)
                    .Distinct()
                    .ToListAsync();

                if (winningTeams == null || winningTeams.Count == 0)
                {
                    return Json(new { success = false, message = "No winning team found" });
                }

                if (winningTeams.Count > 1)
                {
                    return Json(new { success = false, message = "There is a tie between teams." });
                }

                match.Winner = winningTeams[0].Name;
            }

            match.EndDate = DateTime.Now;
            match.Room.Stage = -2;

            await _context.SaveChangesAsync();
            return RedirectToAction("CurrentRoom", "Room", new { id = match.Room.Id });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int roomId)
        {

            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
            if (room != null)
            {
                var joinedPlayers = await _context.JoinRooms
                    .Where(j => j.Room.Id == roomId)
                    .ToListAsync();
                if (joinedPlayers.Any() && joinedPlayers != null)
                {
                    foreach (var player in joinedPlayers)
                    {
                        _context.JoinRooms.Remove(player);
                    }
                }
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("List", "Room");
        }

        [HttpPost]
        public async Task<IActionResult> AddPoint(int matchId, int participantId)
        {
            var match = await _context.Matches
                .Include(m => m.Room)
                .FirstOrDefaultAsync(m => m.Id == matchId);
        
            if (match?.Room == null)
                return Json(new { success = false, message = "Match not found." });
        
            if (match.Room.Mode == "single")
            {
                var participant = await _context.Participants
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == participantId && p.Match.Id == matchId);
        
                if (participant == null)
                    return Json(new { success = false, message = "Participant not found." });
        
                participant.Score++;
            }
        
        
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Point added successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> RemovePoint(int matchId, int participantId)
        {
            var match = await _context.Matches
                .Include(m => m.Room)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match?.Room == null)
                return Json(new { success = false, message = "Match not found." });

            if (match.Room.Mode == "single")
            {
                var participant = await _context.Participants
                    .FirstOrDefaultAsync(p => p.Id == participantId && p.Match.Id == matchId);

                if (participant == null)
                    return Json(new { success = false, message = "Participant not found." });

                if (participant.Score > 0)
                    participant.Score--;
            }
            
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Point removed successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> AddPointTeam(int matchId, int teamId)
        {
            var match = await _context.Matches
                .Include(m => m.Room)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match?.Room == null)
                return Json(new { success = false, message = "Match not found." });

            if (match.Room.Mode != "team")
                return Json(new { success = false, message = "Match is not in team mode." });

            var teamMembers = await _context.Participants
                .Where(p => p.Match.Id == matchId && 
                       p.Team != null && 
                       p.Team.Id == teamId)
                .ToListAsync();

            if (!teamMembers.Any())
                return Json(new { success = false, message = "No team members found." });

            foreach (var member in teamMembers)
            {
                member.Score++;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Points added to team successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> RemovePointTeam(int matchId, int teamId)
        {
            var match = await _context.Matches
                .Include(m => m.Room)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match?.Room == null)
                return Json(new { success = false, message = "Match not found." });

            if (match.Room.Mode != "team")
                return Json(new { success = false, message = "Match is not in team mode." });

            var teamMembers = await _context.Participants
                .Where(p => p.Match.Id == matchId && 
               p.Team != null && 
               p.Team.Id == teamId &&
               p.Score > 0)
            .ToListAsync();

            if (!teamMembers.Any())
                return Json(new { success = false, message = "No team members found with points to remove." });

            foreach (var member in teamMembers)
            {
                member.Score--;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Points removed from team successfully." });
        }
    }
}
