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
                .Include(r => r.Tournament)
                .ToListAsync();

            var joinedPlayers = await _context.JoinRooms
                .GroupBy(j => j.Room.Id)
                .Select(group => new
                {
                    RoomId = group.Key,
                    Count = group.Count()
                }).ToDictionaryAsync(g => g.RoomId, g => g.Count);

            var myRooms = await _context.Rooms
             .Where(r => r.Player == loggedUser && r.Tournament == null)
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
                    .Select(g => new {
                        Team = g.Key,
                        Score = g.First().Score
                    })
                    .ToDictionaryAsync(x => x.Team, x => x.Score);
            }

            
            
            

            var model = new CurrentRoomModel
            {
                Room = room,
                Players = joinedPlayersList,
                LoggedUser = user,
                UserJoined = userJoined,
                CurrentMatch = currentMatch,
                Participants = participants,
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
        public async Task<IActionResult> End(int matchId)
        {
            var match = await _context.Matches
                .FirstOrDefaultAsync(r => r.Id == matchId);

            if(match == null)
            {
                return Json(new { success = false, message = "Match not found" });
            }

            var room = match.Room;

            if (room == null)
            {
                return Json(new { success = false, message = "Room not found." });
            }

            if (room.Mode == "single")
            {

                var maxScore = await _context.Participants
                    .Where(p => p.Match.Id == match.Id)
                    .Select(p => p.Score)
                    .MaxAsync();

                var winners = await _context.Participants
                    .Include(p => p.User)
                    .Where(p => p.Match.Id == match.Id && p.Score == maxScore)
                    .ToListAsync();

                if (winners == null)
                {
                    return Json(new { success = false, message = "Participant not found." });
                }

                if(winners.Count > 1){
                    return Json(new { success = false, message = "There is a tie between two players." });
                }

                match.Winner = winners[0].User.Username;
            }
            else if (room.Mode == "team")
            {
                var maxScore = await _context.Participants
                    .Where(p => p.Match.Id == match.Id)
                    .Select(p => p.Score)
                    .MaxAsync();

                var winningTeam = await _context.Participants
                    .Where(p => p.Match.Id == match.Id && p.Score == maxScore)
                    .Select(p => p.Team)
                    .ToListAsync();

                if (winningTeam == null)
                {
                    return Json(new { success = false, message = "Team not found" });
                }

                if(winningTeam.Count > 1){
                    return Json(new { success = false, message = "There is a tie between the teams." });
                }

                match.Winner = winningTeam[0].Name;
            }     
            match.EndDate = DateTime.Now;
            match.Room.Stage = -2;

            await _context.SaveChangesAsync();
            var url = $"Room/CurrentRoom/{match.Room.Id}";
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

        [HttpPost]
        public async Task<IActionResult> AddPoint(int matchId, int participantId){
        
        var participant = await _context.Participants
            .Where(p => p.Match.Id == matchId)
            .Include(p => p.Match)
            .FirstOrDefaultAsync(p => p.Id == participantId);

        if(participant == null)
            return Json(new { success = false, message = "Participant not found." });

        var match = await _context.Matches
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if(match == null)
            return Json(new { success = false, message = "Match not found." });

        if(match.Room.Mode == "single"){
            participant.Score++;
        } else if(match.Room.Mode == "team"){

            var team = await _context.Participants
                .FirstOrDefaultAsync(p => p.Team.Id == participantId);

            var teamMembers = await _context.Participants
                .Where(p => p.Team.Id == team.Team.Id)
                .ToListAsync();

            foreach(var member in teamMembers){
                member.Score++;
            }
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Point added successfully." });
    }

        [HttpPost]
        public async Task<IActionResult> RemovePoint(int matchId, int participantId){

            var participant = await _context.Participants
                .Where(p => p.Match.Id == matchId)
                .Include(p => p.Match)
                .FirstOrDefaultAsync(p => p.Id == participantId);

            if(participant == null)
                return Json(new { success = false, message = "Participant not found." });

            participant.Score--;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Point removed successfully." });
        }

    }
}
