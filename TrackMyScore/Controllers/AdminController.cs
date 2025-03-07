using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TrackMyScore.Database;

namespace TrackMyScore.Controllers
{
    [Route("a/[action]")]
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
    }
}
