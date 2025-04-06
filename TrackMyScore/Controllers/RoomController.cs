using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackMyScore.Database;
using TrackMyScore.Models;
using TrackMyScore.Services;

namespace TrackMyScore.Controllers
{
    public class RoomController : Controller
    {

        private readonly AppDbContext _context;
        private readonly UserService _userService;

        public RoomController(AppDbContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Play()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {

            string email = HttpContext.Session.GetString("email");

            if(email == null)
            {
                RedirectToAction("Account", "Login");
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

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

        [HttpPost]
        public async Task<IActionResult> Create(string name, string type, string location, DateOnly startDate, TimeOnly startTime, int gameId, int tournamentId)
        {

            Room room;

            string email = HttpContext.Session.GetString("email");

            if(email == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            
            if(user == null)
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

            return RedirectToAction("Create", "Room");

        }
    }
}
