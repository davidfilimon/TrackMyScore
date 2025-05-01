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
            var games = await _context.Games.ToListAsync();

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

            if(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description) || string.IsNullOrEmpty(difficulty) || maxPlayers == 0)
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
                Author = user.Username,
                IsOfficial = false
            };

            _context.Games.Add(game);
            _context.SaveChanges();

            return RedirectToAction("List", "Games");

        }

        public async Task<IActionResult> ToggleFavorite(int id)
        {
            int userId = int.Parse(Request.Cookies["userId"]);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if(user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id);

            if(game == null)
            {
                return NotFound();
            }

            var favoriteGame = await _context.FavoriteGames
                .FirstOrDefaultAsync(fg => fg.UserId == userId && fg.GameId == game.Id);

            if(favoriteGame != null)
            {
                _context.FavoriteGames.Remove(favoriteGame);
            }
            else
            {
                favoriteGame = new FavoriteGame(user.Id, game.Id);
                _context.FavoriteGames.Add(favoriteGame);
            }

            await _context.SaveChangesAsync();

            string referer = Request.Headers["Referer"].ToString();

            if(referer.Contains("List", StringComparison.OrdinalIgnoreCase))
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

    }
}
