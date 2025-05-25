using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackMyScore.Database;
using TrackMyScore.Models;

namespace TrackMyScore.Controllers
{
    public class GamesController : Controller
    {

        private AppDbContext _context;

        public GamesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var games = await _context.Games
                .Include(g => g.Author)
                .ToListAsync();

            return View(games);
        }

        [HttpGet]
        public IActionResult AddGame()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddGame(string name, string description, int maxPlayers, string difficulty)
        {

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description) || string.IsNullOrEmpty(difficulty) || maxPlayers == 0)
            {
                ViewData["Error"] = "All fields are required to register a game.";
                return View();
            }

            string email = HttpContext.Session.GetString("email");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var game = new Game
            {
                Title = name,
                Description = description,
                MaxPlayers = maxPlayers,
                Difficulty = difficulty,
                Author = user,
                IsOfficial = false
            };

            _context.Games.Add(game);

            var favoriteGame = new FavoriteGame
            {
                User = user,
                Game = game
            };

            _context.FavoriteGames.Add(favoriteGame);

            _context.SaveChanges();

            return RedirectToAction("List", "Games");

        }

        public async Task<IActionResult> ToggleFavorite(int id)
        {
            int userId = int.Parse(Request.Cookies["userId"]);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id);

            if (game == null)
            {
                return NotFound();
            }

            var favoriteGame = await _context.FavoriteGames
                .FirstOrDefaultAsync(fg => fg.User.Id == userId && fg.Game.Id == game.Id);

            if (favoriteGame != null)
            {
                _context.FavoriteGames.Remove(favoriteGame);
            }
            else
            {
                favoriteGame = new FavoriteGame
                {
                    User = user,
                    Game = game
                };
                _context.FavoriteGames.Add(favoriteGame);
            }

            await _context.SaveChangesAsync();

            string referer = Request.Headers["Referer"].ToString();

            if (referer.Contains("List", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("List", "Games");
            }

            return RedirectToAction("Details", "Games", new { id });

        }

        [HttpGet]

        public IActionResult Details(int id)
        {
            var game = _context.Games.FirstOrDefault(g => g.Id == id);

            return View(game);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id);

            if (game == null)
            {
                return Json(new { success = false, message = "Game not found." });
            }

            var rooms = await _context.Rooms
                .Where(r => r.Game == game)
                .ToListAsync();

            if (rooms.Any())
            {
                return Json(new { success = false, message = "There are already rooms played with this game." });
            }

            var tournaments = await _context.Tournaments
                .Where(t => t.Game == game)
                .ToListAsync();

            if (tournaments.Any())
            {
                return Json(new { success = false, message = "There are already tournaments played with this game." });
            }

            var favoriteUsers = await _context.FavoriteGames
                .Where(t => t.Game == game)
                .ToListAsync();

            foreach (var favoriteUser in favoriteUsers)
            {
                _context.FavoriteGames.Remove(favoriteUser);
            }

            _context.Games.Remove(game);
            await _context.SaveChangesAsync();

            return RedirectToAction("List", "Games");
            
        }

    }
}
