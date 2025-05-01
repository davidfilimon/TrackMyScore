using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TrackMyScore.Database;
using TrackMyScore.Models;
using TrackMyScore.Services;
using Microsoft.AspNetCore.Http;

namespace TrackMyScore.Controllers
{
    public class AccountController : Controller
    {
        private AppDbContext _context;
        private readonly CreateAccountService _createAccountService;
        private readonly AuthenticationService _authenticationService;


        public AccountController(AppDbContext context, AuthenticationService authenticationService, CreateAccountService createAccountSerivce)
        {
            _context = context;
            _authenticationService = authenticationService;
            _createAccountService = createAccountSerivce;
        }

        [HttpGet]
        public async Task<IActionResult> Profile(int? id)
        {
            var email = HttpContext.Session.GetString("email");  

            if (email == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var loggedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if(loggedUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var loggedUserId = loggedUser.Id;
            ViewData["loggedUserId"] = loggedUserId;

            var profileUserId = id ?? loggedUserId; // searches for logged user if there is no id given

            var profileUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == profileUserId); 

            if (profileUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["UserId"] = profileUser.Id;

            var games = await _context.Games
                .Where(g => g.Author == profileUser)
                .ToListAsync();

            var matches = await _context.Participants
                .Include(p => p.Match)
                    .ThenInclude(m => m.Room)
                        .ThenInclude(g => g.Game)
                        .Where(p => p.User.Id == profileUser.Id)
                    .ToListAsync();

            var model = new UserGamesModel
            {
                User = profileUser,
                CustomGames = games,
                Matches = matches,
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            ViewData["LoginError"] = null;
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

            // cookies for storing login info and not having to log in every single time
            var cookieOptions = new CookieOptions() 
            {
                // Expires = DateTime.UtcNow.AddDays(7), // adding 7 days as an expiration date for the cookies, so you have to log in each week
                Secure = true, // send only through https
                HttpOnly = true, // protection against js access
                IsEssential = true // gdpr
            };

            // setting the cookies values
            Response.Cookies.Append("userId", user.Id.ToString(), cookieOptions);
            Response.Cookies.Append("email", user.Email, cookieOptions);
            Response.Cookies.Append("username", user.Username, cookieOptions);

            // setting viewdata for the views

            ViewData["userId"] = Request.Cookies["userId"];
            ViewData["username"] = Request.Cookies["username"];
            ViewData["email"] = Request.Cookies["email"];


            // setting up the values for the current session

            HttpContext.Session.SetInt32("userId", user.Id);
            HttpContext.Session.SetString("email", user.Email);
            HttpContext.Session.SetString("username", user.Username);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            // deleting data
            HttpContext.Session.Clear();
            Response.Cookies.Delete("email");
            Response.Cookies.Delete("username");    

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Search(string query)
        {
            if(query.IsNullOrEmpty()){
                return View("Results", new List<User>()); // returns an empty list
            }

            ViewData["SearchQuery"] = query;

            var results = await _context.Users.Where(u => u.Username.Contains(query)).ToListAsync(); // list of results

            return View("Results", results); // sends a list of results to the view

        }


    }
}
