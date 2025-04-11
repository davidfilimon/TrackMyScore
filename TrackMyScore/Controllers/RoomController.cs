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
            var usedRoomIds = await _context.Matches
                .Select(m => m.Room.Id)
                .ToListAsync();

            var roomList = await _context.Rooms
                .Where(r => !usedRoomIds.Contains(r.Id))
                .Include(r => r.Player)
                .Include(g => g.Game)
                .ToListAsync();

            var joinedPlayers = await _context.JoinRooms
                .GroupBy(j => j.Room.Id)
                .Select(group => new
                {
                    RoomId = group.Key,
                    Count = group.Count()
                }).ToDictionaryAsync(g => g.RoomId, g => g.Count);


            ViewBag.JoinedPlayers = joinedPlayers;
            ViewBag.RoomList = roomList;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {

            var user = await GetLoggedUserAsync();

            if(user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var games = await _context.FavoriteGames
                                .Where(f => f.UserId == user.Id)
                                .Join(_context.Games,  
                                    favorite => favorite.GameId,
                                    game => game.Id,
                                    (favorite, game) => game)  
                                .ToListAsync();

            ViewBag.User = user;
            ViewBag.Games = games;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CurrentRoom(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.Player)
                .Include(r => r.Game)
                .FirstOrDefaultAsync(r => r.Id == id);

            var joinedPlayers = await _context.JoinRooms
                .Where(j => j.Room.Id == id)
                .CountAsync();

            ViewBag.JoinedPlayers = joinedPlayers;

            var user = await GetLoggedUserAsync();

            if (user != null)
            {
                ViewBag.LoggedUser = user;

                var userJoined = await _context.JoinRooms
                    .AnyAsync(u => u.User.Id == user.Id && u.Room.Id == id);

                ViewBag.userJoined = userJoined;

            }
            else
            {
                return RedirectToAction("Login", "Account"); 
            }

            return View(room);
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

            if(room != null)
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

                if(joinRoom != null)
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
                Player = user,
                Game = game
            };

            await _context.Rooms.AddAsync(room);
            await _context.SaveChangesAsync();

            return RedirectToAction("CurrentRoom", "Room", new {id = room.Id});

        }
    }
}
