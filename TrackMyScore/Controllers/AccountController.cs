using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TrackMyScore.Database;
using TrackMyScore.Services;

namespace TrackMyScore.Controllers
{

    [Route("[controller]/[action]/{id?}")]
    public class AccountController : Controller
    {
        private AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly CreateAccountService _createAccountService;
        private readonly AuthenticationService _authenticationService;
        private readonly ValidationService _validationService;

        private readonly string cacheKey = "accountCacheKey";

        public AccountController(AppDbContext context, IMemoryCache cache, AuthenticationService authenticationService, ValidationService validationService, CreateAccountService createAccountSerivce)
        {
            _context = context;
            _cache = cache;
            _authenticationService = authenticationService;
            _validationService = validationService;
            _createAccountService = createAccountSerivce;
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
            ViewData["RegisterError"] = null;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword)
        {
            if(password == confirmPassword)
            {
                var (success, message) = await _createAccountService.Register(username, email, password);

                if (!success)
                {
                    ViewData["RegisterError"] = message;
                    return View();
                }

                return RedirectToAction("Login", "Account");

            }
            else
            {
                ViewData["RegisterError"] = "Passwords do not match.";
                return View();
            }

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
