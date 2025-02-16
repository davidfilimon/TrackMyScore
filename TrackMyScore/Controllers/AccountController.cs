﻿using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TrackMyScore.Database;
using TrackMyScore.Models;
using TrackMyScore.Services;

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

            if(email != null)
            {
                RedirectToAction("Login", "Account");
            }

            var userId = id ?? (await _context.Users.FirstOrDefaultAsync(u => u.Email == email))?.Id; // searches for logged user if there is no id given

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if(user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["UserId"] = user.Id;

            return View(user);
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
