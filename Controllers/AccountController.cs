using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TrackMyScore.Database;
using TrackMyScore.Models;
using TrackMyScore.Services;
using Microsoft.AspNetCore.Http;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Options;

namespace TrackMyScore.Controllers
{
    public class AccountController : Controller
    {
        private AppDbContext _context; // db context
        private readonly CreateAccountService _createAccountService; // account creation service
        private readonly AuthenticationService _authenticationService; // authentication service
        private readonly SmtpOptions _smtp;


        public AccountController(AppDbContext context, AuthenticationService authenticationService, CreateAccountService createAccountSerivce, IOptions<SmtpOptions> smtpOptions)
        {
            _context = context;
            _authenticationService = authenticationService;
            _createAccountService = createAccountSerivce;
            _smtp = smtpOptions.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Profile(int? id) // method for accessing a profile
        {
            var loggedUser = await GetLoggedUserAsync(); // searching for logged user;
            
            ViewData["loggedUserId"] = loggedUser.Id; // send the id to the view through a viewdata

            var profileUserId = id ?? loggedUser.Id; // either access the profile by the requested id or go to your own profile
            var profileUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == profileUserId);
            if (profileUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["UserId"] = profileUser.Id; // send the profile user id to the view

            var games = await _context.FavoriteGames
                .Where(g => g.User == profileUser || g.Game.Author == profileUser)
                .Select(g => g.Game)
                .ToListAsync()
                ?? new List<Game>(); // search for the list of either favorite games or published games

            var singleMatches = await _context.Players
                .Where(p => p.User.Id == profileUser.Id)
                .Select(p => p.Match)
                .ToListAsync()
                ?? new List<Match>(); // search for single matches that the user took part in

            var teamMatches = await _context.TeamPlayers
                .Where(p => p.User.Id == profileUser.Id)
                .Select(p => p.Match)
                .ToListAsync()
                ?? new List<Match>(); // search for team matches that the user took part in

            var allMatches = singleMatches.Concat(teamMatches).ToList()
                            ?? new List<Match>(); // unify the match list

            var tournamentsSingle = await _context.Players
                .Include(p => p.Match)
                    .ThenInclude(p => p.Tournament)
                .Where(p => p.User.Id == profileUser.Id && p.Match.Tournament.IsActive == false && p.Match.Tournament.Stage > 1)
                .Select(p => p.Match.Tournament)
                .ToListAsync()
                ?? new List<Tournament>(); // search for single tournaments that the user took part in

            var tournamentsTeam = await _context.TeamPlayers
                .Include(p => p.Match)
                    .ThenInclude(p => p.Tournament)
                .Include(p => p.Team)
                .Where(p => p.User.Id == profileUser.Id && p.Match.Tournament.IsActive == false && p.Match.Tournament.Stage > 1)
                .Select(p => p.Match.Tournament)
                .ToListAsync()
                ?? new List<Tournament>(); // search for team tournaments that the user took part in

            var tournaments = tournamentsSingle.Concat(tournamentsTeam).ToList(); // unify the tournament list

            var matchWinners = new Dictionary<int, string>(); // searching for match winners
            var tournamentWinners = new Dictionary<int, string>(); // searching for tournament winners

            foreach (var match in singleMatches)
            {
                var playersInMatch = await _context.Players
                    .Include(p => p.User)
                    .Where(p => p.MatchId == match.Id)
                    .ToListAsync()
                    ?? new List<Player>();

                if (!playersInMatch.Any())
                {
                    matchWinners[match.Id] = "";
                    continue;
                }

                int maxScore = playersInMatch.Max(p => p.Score);
                var winnerName = playersInMatch.First(p => p.Score == maxScore);
                matchWinners[match.Id] = winnerName.User.Username; // populate the dictionary with the match id and the winner
            }

            foreach (var match in teamMatches)
            {
                var playersInMatch = await _context.TeamPlayers
                    .Include(p => p.User)
                    .Include(p => p.Team)
                    .Where(p => p.MatchId == match.Id)
                    .ToListAsync()
                    ?? new List<TeamPlayer>();
                if (!playersInMatch.Any())
                {
                    matchWinners[match.Id] = "";
                    continue;
                }

                int maxScore = playersInMatch.Max(p => p.Team.Score);
                var winnerTeam = playersInMatch.First(p => p.Team.Score == maxScore);
                matchWinners[match.Id] = winnerTeam.Team.Name; // populate the dictionary with the match id and the winners
            }

            var totalMatchesPlayed = allMatches.Count; // number of total matches
            int matchesWon = 0;

            foreach (var match in allMatches)
            {
                if (!matchWinners.ContainsKey(match.Id)) // check for the current match in match winners, if none found go to the next match
                {
                    continue;
                }

                string winner = matchWinners[match.Id];
                if (match.Mode == "single")
                {
                    if (winner == profileUser.Username) // if the user is the winner increment matches won
                    {
                        matchesWon++;
                    }
                }
                else if (match.Mode == "team")
                {
                    var teamPlayersInMatch = await _context.TeamPlayers
                        .Include(tp => tp.Team)
                        .Include(tp => tp.User)
                        .Where(tp => tp.MatchId == match.Id)
                        .ToListAsync()
                        ?? new List<TeamPlayer>(); // search for the players of the team

                    string winningTeamName = winner;
                    bool userWasOnWinningTeam = teamPlayersInMatch
                        .Any(tp => tp.UserId == profileUser.Id && tp.Team.Name == winningTeamName);

                    if (userWasOnWinningTeam)
                    {
                        matchesWon++; // increment the matches won if the player was part of the winning team
                    }
                }
            }

            int tournamentsWon = 0;

            foreach (var tourn in tournaments)
            {
                var finalMatches = await _context.Matches
                    .Where(m => m.TournamentId == tourn.Id && m.Stage == tourn.Stage)
                    .ToListAsync();

                var finalMatch = finalMatches.FirstOrDefault(); // search for the last match of the tournament
                string winnerName = "";

                if (finalMatch != null)
                {
                    if (finalMatch.Mode == "single")
                    {
                        var winnerPlayer = await _context.Players
                            .Include(p => p.User)
                            .Where(p => p.MatchId == finalMatch.Id && !p.Eliminated)
                            .Select(p => p.User.Username)
                            .FirstOrDefaultAsync(); // if the tournament is in single mode, take the one winner by selecting it from the last match
                        winnerName = winnerPlayer ?? "";
                    }
                    else // team mode
                    {
                        var winnerTeam = await _context.TeamPlayers
                            .Include(tp => tp.Team)
                            .Where(tp => tp.MatchId == finalMatch.Id && !tp.Eliminated)
                            .Select(tp => tp.Team.Name)
                            .FirstOrDefaultAsync(); // select the winning team from the last match of the tournament
                        winnerName = winnerTeam ?? "";
                    }
                }

                tournamentWinners[tourn.Id] = winnerName;

                if (finalMatch.Mode == "single")
                {
                    if (winnerName == profileUser.Username)
                        tournamentsWon++; // increment the won tournaments if the user won the last match
                }
                else
                {
                    var winningTeamPlayers = await _context.TeamPlayers
                        .Include(tp => tp.User)
                        .Include(tp => tp.Team)
                        .Where(tp => tp.MatchId == finalMatch.Id && tp.Team.Name == winnerName)
                        .ToListAsync();

                    if (winningTeamPlayers.Any(tp => tp.UserId == profileUser.Id))
                        tournamentsWon++; // increment the won tournaments if the user's team won the last match
                }
            }

            var model = new UserGamesModel
            { // send the view model
                User = profileUser,
                CustomGames = games,
                Matches = allMatches,
                Tournaments = tournaments,
                MatchWinners = matchWinners,
                TournamentWinners = tournamentWinners,
                TotalMatchesPlayed = totalMatchesPlayed,
                MatchesWon = matchesWon,
                TotalTournamentsPlayed = tournaments.Count,
                TournamentsWon = tournamentsWon
            };

            return View(model);
        }

        // view web pages
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
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword)
        { // register method through the create account service
            if (password == confirmPassword)
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
        public async Task<IActionResult> Login(string email, string password, bool rememberMe)
        { // login through the authentication service
            User user = await _authenticationService.Login(email, password);

            if (user == null)
            {
                ViewData["LoginError"] = "Email or password error";
                return View();
            }

            if (rememberMe)
            {
                // cookies for storing login info and not having to log in every single time if the remember me checkbox in checked
                var cookieOptions = new CookieOptions()
                {
                    // Expires = DateTime.UtcNow.AddDays(7), // adding 7 days as an expiration date for the cookies, so you have to log in each week
                    Secure = false, // send only through https if true
                    HttpOnly = true, // protection against js access
                    IsEssential = true // gdpr
                };

                // setting the cookies values
                Response.Cookies.Append("userId", user.Id.ToString(), cookieOptions);
                Response.Cookies.Append("email", user.Email, cookieOptions);
                Response.Cookies.Append("username", user.Username, cookieOptions);

            }

            // setting viewdata for the views

            ViewData["userId"] = user.Id.ToString();
            ViewData["username"] = user.Username;
            ViewData["email"] = user.Email;

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
            if (query.IsNullOrEmpty())
            {
                return View("Results", new List<User>()); // returns an empty list
            }

            ViewData["SearchQuery"] = query;

            var results = await _context.Users.Where(u => u.Username.Contains(query)).ToListAsync(); // list of results

            return View("Results", results); // sends a list of results to the view

        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(int userId, string username, int photoId, string oldPassword, string newPassword, string confirmPassword)
        { // edit profile method for changing either username, password, profile picture or any combination of them

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId); // check for profile

            var currentUser = await GetLoggedUserAsync();

            if (currentUser != user) // if profile and logged user dont match return error
            {
                return Json(new { success = false, message = "The profile that requested the change is not the same as the logged user." });
            }

            if (user == null)
            {
                return Json(new { success = false, message = "Profile not found." });
            }

            if (username != user.Username) // checking if the new username already exists
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (existingUser != null)
                {
                    return Json(new { success = false, message = "Username already in use." });
                }
                HttpContext.Session.SetString("username", username);
                user.Username = username;

            }

            if (!oldPassword.IsNullOrEmpty() && !newPassword.IsNullOrEmpty() && !confirmPassword.IsNullOrEmpty()) // change the password only if the user requested to
            {
                if (confirmPassword == newPassword)
                {

                    if (newPassword == oldPassword) // password validations
                    {
                        return Json(new { success = false, message = "The new password cannot be the old password." });
                    }

                    if (!_authenticationService.PasswordMatch(user, oldPassword)) // password validations through services
                    {
                        return Json(new { success = false, message = "Old password does not match." });
                    }

                    if (_authenticationService.ChangePassword(user, newPassword))
                    {
                        await _context.SaveChangesAsync();
                        return Json(new { success = true, message = "Password successfully changed." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Password did not change." });
                    }
                }

            }

            user.Picture = photoId; // change the photo to the chosen photo

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Account details edited successfully" });

        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email)) // check for email
            {
                ViewBag.Error = "Please enter your email address.";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email); // check for user with the received email
            if (user == null)
            {
                ViewBag.Error = "No account found for that email.";
                return View();
            }

            // generate a new password
            var newPassword = Guid.NewGuid().ToString("N").Substring(0, 8);

            if (!_authenticationService.ChangePassword(user, newPassword))
            {
                return Json(new { success = false, message = "User not found." });
            }
            await _context.SaveChangesAsync();

            using var client = new SmtpClient(_smtp.Host, _smtp.Port) // initializing client
            {
                Credentials = new NetworkCredential(_smtp.User, _smtp.Pass),
                EnableSsl = true
            };
            // mail message
            var msg = new MailMessage
            {
                From = new MailAddress(_smtp.User, "TrackMyScore"),
                Subject = "Your TrackMyScore password has been reset",
                Body = $@"
                Hello {user.Username},

                You have requested a password reset. Your new temporary password is:

                    {newPassword}

                Please log in with this password, then change it in your profile settings.

                — The TrackMyScore Team
                "
            };
            msg.To.Add(email);
            await client.SendMailAsync(msg);

            ViewBag.Success = "An email with your new password has been sent. Please check your inbox.";
            return View();
        }
        
        private async Task<User> GetLoggedUserAsync()
        { // method for getting the logged user
            string email = HttpContext.Session.GetString("email");

            if (email == null)
            {
                return null;
            }

            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

    }
}
