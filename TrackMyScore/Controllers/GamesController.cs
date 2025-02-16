using Microsoft.AspNetCore.Mvc;
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

        public IActionResult List(List<Game> games)
        {       
            foreach(Game game in _context.Games)
            {
                games.Add(game);
            }

            return View(games);
        }
    }
}
