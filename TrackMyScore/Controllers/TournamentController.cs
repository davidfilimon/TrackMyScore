using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackMyScore.Database;
using TrackMyScore.Models;

namespace TrackMyScore.Controllers
{
    public class TournamentController : Controller
    {

        private readonly AppDbContext _context;

        public TournamentController(AppDbContext context)
        {
            _context = context;
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
        public async Task<IActionResult> Create()
        {
            var user = await GetLoggedUserAsync();

            if (user == null)
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
        public IActionResult Join()
        {
            return View();
        }
    }
}
