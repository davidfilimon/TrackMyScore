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
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Game game)
        {
            if (ModelState.IsValid)
            {
                _context.Games.Add(game);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(List));
            }

            return View(game);
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

            return RedirectToAction("List");

        }

    }
}
