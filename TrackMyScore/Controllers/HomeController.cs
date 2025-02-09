using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TrackMyScore.Models;

namespace TrackMyScore.Controllers
{
    [Route("[controller]/[action]/{id?}")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMemoryCache _cache;
        private readonly string cacheKey = "accountCacheKey";

        public HomeController(ILogger<HomeController> logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }
        [Route("/")]
        public IActionResult Index()
        {
            var email = Request.Cookies["email"];
            var username = Request.Cookies["username"];

            if(email != null && username != null)
            {
                HttpContext.Session.SetString("username", username);
                HttpContext.Session.SetString("email", email);
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
