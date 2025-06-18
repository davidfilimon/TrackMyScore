using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TrackMyScore.Database;
using TrackMyScore.Models;

namespace TrackMyScore.Controllers
{
    [Route("[controller]/[action]/{id?}")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMemoryCache _cache;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, IMemoryCache cache, AppDbContext context)
        {
            _logger = logger;
            _cache = cache;
            _context = context;
        }
        [Route("/")]
        public IActionResult Index()
        {
            var email = Request.Cookies["email"];
            var username = Request.Cookies["username"];

            if (email != null && username != null)
            {
                HttpContext.Session.SetString("username", username);
                HttpContext.Session.SetString("email", email);
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Where(u => u.Id > 0)
                .ToListAsync();

            return Ok(users); // returns a json with all users
        }
    }
}
