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
        public async Task<IActionResult> RoomList()
        {

            User loggedUser = await GetLoggedUserAsync();

            var roomList = await _context.Rooms
                .Where(r => r.Stage == -1 && r.Tournament == null)
                .Include(r => r.Player)
                .Include(r => r.Game)
                .ToListAsync();

            var joinedPlayers = await _context.JoinRooms
                .GroupBy(j => j.Room.Id)
                .Select(group => new
                {
                    RoomId = group.Key,
                    Count = group.Count()
                }).ToDictionaryAsync(g => g.RoomId, g => g.Count);

            var myRooms = await _context.Rooms
             .Where(r => r.Player == loggedUser)
             .Include(r => r.Game)
             .ToListAsync();

            var joinedRooms = await _context.JoinRooms
                .Where(r => r.User == loggedUser)
                .Include(r => r.Room)
                .Include(r => r.User)
                .Include(r => r.Room.Game)
                .ToListAsync();

            ViewBag.JoinedRooms = joinedRooms;
            ViewBag.MyRooms = myRooms;
            ViewBag.JoinedPlayers = joinedPlayers;
            ViewBag.RoomList = roomList;

            return View();
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
                .Include(r => r.Player)
                .Include(r => r.Game)
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

            if(currentMatch != null)
            {
                participants = await _context.Participants
                    .Where(p => p.Match.Id == currentMatch.Id)
                    .Include(p => p.Team)
                    .ToListAsync();
            }

            var model = new CurrentRoomModel
            {
                Room = room,
                Players = joinedPlayersList,
                LoggedUser = user,
                UserJoined = userJoined,
                CurrentMatch = currentMatch,
                Participants = participants
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
                Player = user,
                Game = game
            };

            await _context.Rooms.AddAsync(room);
            await _context.SaveChangesAsync();

            return RedirectToAction("CurrentRoom", "Room", new { id = room.Id });

        }

        [HttpPost]
        public async Task<IActionResult> Start(int roomId, Dictionary<int, string> teamAssignments, Dictionary<int, string> roles)
        {

            Room room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null)
            {
                return Json(new { success = false, message = "Room not found." });
            }

            room.Stage = 0;
            room.Mode = "team";

            Match match = new Match
            {
                StartDate = DateTime.Now,
                EndDate = null,
                Room = room,
                Type = room.Type,
            };

            await _context.Matches.AddAsync(match);

            foreach (var assignment in teamAssignments)
            {
                var playerId = assignment.Key;

                User player = await _context.Users.FirstOrDefaultAsync(u => u.Id == playerId);

                var teamName = assignment.Value;
                var role = roles.ContainsKey(playerId) ? roles[playerId] : string.Empty;

                if (room != null && player != null && teamName != null)
                {

                    Team team = new Team
                    {
                        Name = teamName,
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

            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Match started." });
        }
        
        [HttpPost]
        public async Task<IActionResult> StartIndividual(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Player)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || room.Player == null)
                return Json(new { success = false });

            var joinedPlayers = await _context.JoinRooms
                .Where(r => r.Room.Id == roomId)
                .Include(j => j.User)
                .ToListAsync();

            if (!joinedPlayers.Any())
                return RedirectToAction("RoomList", "Room");

            var playerIds = joinedPlayers.Select(j => j.User.Id).Append(room.Player.Id).ToList();
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
        public async Task<IActionResult> End(int roomId)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if(room == null)
            {
                return Json(new { success = false, message = "Room not found" });

            }

            var match = await _context.Matches
                    .Include(r => r.Room)
                    .FirstOrDefaultAsync(r => r.Room.Id == roomId);

            if (match == null)
            {
                return Json(new { success = false, message = "Match not found" });
            }

            if (room.Mode == "single")
            {
                var winner = await _context.Participants
                    .Include(p => p.User)
                    .Where(p => p.Match.Id == match.Id)
                    .OrderByDescending(p => p.Score)
                    .FirstOrDefaultAsync();

                if (winner == null)
                {
                    return Json(new { success = false, message = "Participant not found" });
                }
                match.Winner = winner.User.Username;
            }
            else if (room.Mode == "team")
            {
                var winningTeam = await _context.Participants
                    .Where(p => p.Match.Id == match.Id)
                    .OrderByDescending(p => p.Score)
                    .Select(p => p.Team)
                    .FirstOrDefaultAsync();

                if (winningTeam == null)
                {
                    return Json(new { success = false, message = "Team not found" });
                }

                match.Winner = winningTeam.Name;
            }     
            match.EndDate = DateTime.Now;
            match.Room.Stage = -2;

            await _context.SaveChangesAsync();
            var url = Url.Action("CurrentRoom", "Room", new { roomId });
            return Json(new
            {
                success = true,
                url
            });

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

            return RedirectToAction("RoomList", "Room");
        }

    }
}
