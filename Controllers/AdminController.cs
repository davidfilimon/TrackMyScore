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

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            string email = HttpContext.Session.GetString("email");

            if (email.IsNullOrEmpty())
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || !user.isAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(string name, string description, int maxPlayers, string difficulty)
        {
            string email = HttpContext.Session.GetString("email");

            if (email.IsNullOrEmpty())
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !user.isAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description) || string.IsNullOrEmpty(difficulty) || maxPlayers == 0)
            {
                ViewData["Error"] = "All fields are required to register a game.";
                return View();
            }

            var game = new Game
            {
                Title = name,
                Description = description,
                MaxPlayers = maxPlayers,
                Difficulty = difficulty,
                Author = user.Username,
                IsOfficial = true
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            return RedirectToAction("List", "Games");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleOfficial(int id)
        {
            string email = HttpContext.Session.GetString("email");

            if (email.IsNullOrEmpty())
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !user.isAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id);

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
            }
         
            return RedirectToAction("Details", "Games", new { id });

        }
    }
}
