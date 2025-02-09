using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TrackMyScore.Database;
using TrackMyScore.Models;
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


        public AccountController(AppDbContext context, IMemoryCache cache, AuthenticationService authenticationService, CreateAccountService createAccountSerivce)
        {
            _context = context;
            _cache = cache;
            _authenticationService = authenticationService;
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

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            User user = await _authenticationService.Login(email, password);

            if(user == null)
            {
                ViewData["LoginError"] = "Email or password error";
                return View();
            }

            var cookieOptions = new CookieOptions() // cookies for storing login info and not having to log in every single time
            {
                // Expires = DateTime.UtcNow.AddDays(7), // adding 7 days as an expiration date for the cookies, so you have to log in each week
                Secure = true, // send only through https
                HttpOnly = true, // protection against js access
                IsEssential = true // gdpr
            };

            Response.Cookies.Append("email", user.Email, cookieOptions);
            Response.Cookies.Append("username", user.Username, cookieOptions);

            HttpContext.Session.SetString("email", user.Email);
            HttpContext.Session.SetString("username", user.Username);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("email");
            Response.Cookies.Delete("username");

            return RedirectToAction("Index", "Home");
        }
    }
}
