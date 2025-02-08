using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TrackMyScore.Database;

namespace TrackMyScore.Controllers
{

    [Route("[controller]/[action]/{id?}")]
    public class AccountController : Controller
    {
        private AppDbContext _context;
        private readonly IMemoryCache _cache;

        private readonly string cacheKey = "accountCacheKey";

        public AccountController(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpGet]
        public IActionResult Profile(int id)
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete(".AspNetCore.Session");
            _cache.Dispose();

            return RedirectToAction("Index", "Home");
        }
    }
}
