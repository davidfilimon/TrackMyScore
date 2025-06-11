using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TrackMyScore.Database;
using TrackMyScore.Models;

namespace TrackMyScore.Controllers
{
    [Route("a/[action]/{id?}")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly StatsService _stats;

        public AdminController(AppDbContext context, StatsService stats)
        {
            _context = context;
            _stats = stats;
        }

        public async Task<IActionResult> Dashboard()
        { // admin dashboard for statistics
            string email = HttpContext.Session.GetString("email"); // admin validations

            if (email.IsNullOrEmpty())
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || !user.isAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = await _stats.GetStatsAsync(); // getting the stats

            return View(model);
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(string name, string description, int maxPlayers, string difficulty)
        { // adding a game as an admin
            string email = HttpContext.Session.GetString("email"); // getting the current user

            if (email.IsNullOrEmpty())
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !user.isAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description) || string.IsNullOrEmpty(difficulty) || maxPlayers <= 0) // field validations
            {
                ViewData["Error"] = "All fields are required to register a game.";
                return View();
            }

            var game = new Game // adding the game to the db
            {
                Title = name,
                Description = description,
                MaxPlayers = maxPlayers,
                Difficulty = difficulty,
                Author = user,
                IsOfficial = true
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            return RedirectToAction("List", "Games");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleOfficial(int id)
        { // changing the game status
            string email = HttpContext.Session.GetString("email"); // getting the current user

            if (email.IsNullOrEmpty())
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !user.isAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id); // getting the current game

            if(game != null)
            {
                if (!game.IsOfficial)
                {
                    game.IsOfficial = true;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    game.IsOfficial = false;
                    await _context.SaveChangesAsync();
                }
            } // toggle the game's official status
         
            return Json(new {success = true, message = "Successfully changed the game's status."});

        }
    }
}
