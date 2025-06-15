using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TrackMyScore.Database;
using TrackMyScore.Models;

namespace TrackMyScore.Controllers
{
    public class GamesController : Controller
    {

        private AppDbContext _context;
        private const double RECOMMENDATION_PERCENTAGE = 70;

        public GamesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var email = Request.Cookies["email"];

            List<Game> favoriteGames = new();

            if (!email.IsNullOrEmpty()) {

                int userId = _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email).Id;

                favoriteGames = await RecommendedGames(userId);

            }

            var games = await _context.Games
                .Include(g => g.Author)
                .ToListAsync();

            var model = new GamesViewModel
            {
                AllGames = games,
                RecommendedGames = favoriteGames
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult AddGame()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddGame(string name, string description, int maxPlayers, string difficulty)
        {

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description) || string.IsNullOrEmpty(difficulty) || maxPlayers == 0)
            {
                ViewData["Error"] = "All fields are required to register a game.";
                return View();
            }

            string email = HttpContext.Session.GetString("email");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var game = new Game
            {
                Title = name,
                Description = description,
                MaxPlayers = maxPlayers,
                Difficulty = difficulty,
                Author = user,
                IsOfficial = false
            };

            _context.Games.Add(game);

            var favoriteGame = new FavoriteGame
            {
                User = user,
                Game = game
            };

            _context.FavoriteGames.Add(favoriteGame);

            _context.SaveChanges();

            return RedirectToAction("List", "Games");

        }

        public async Task<IActionResult> ToggleFavorite(int id) // method for toggling favorite games
        {
            int userId = int.Parse(Request.Cookies["userId"]);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id);

            if (game == null)
            {
                return NotFound();
            }

            var favoriteGame = await _context.FavoriteGames
                .FirstOrDefaultAsync(fg => fg.User.Id == userId && fg.Game.Id == game.Id);

            if (favoriteGame != null)
            {
                _context.FavoriteGames.Remove(favoriteGame);
            }
            else
            {
                favoriteGame = new FavoriteGame
                {
                    User = user,
                    Game = game
                };
                _context.FavoriteGames.Add(favoriteGame);
            }

            await _context.SaveChangesAsync();

            string referer = Request.Headers["Referer"].ToString();

            if (referer.Contains("List", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("List", "Games");
            }

            return RedirectToAction("Details", "Games", new { id });

        }

        [HttpGet]

        public IActionResult Details(int id) // method to access details of a game
        {
            var game = _context.Games.FirstOrDefault(g => g.Id == id);

            if (game == null || game.Deleted)
            {
                return RedirectToAction("List", "Game");
            }

            return View(game);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id) // method for deleting a game
        {
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id);

            if (game == null)
            {
                return Json(new { success = false, message = "Game not found." });
            }

            if (await AlreadyPlayed(game))
            {
                game.Deleted = true;
            }
            else // if the game is already played only check the status of deleted instead of deleting all the data
            {
                var favoriteUsers = await _context.FavoriteGames
                .Where(t => t.Game == game)
                .ToListAsync();

                foreach (var favoriteUser in favoriteUsers)
                {
                    _context.FavoriteGames.Remove(favoriteUser);
                }


                _context.Games.Remove(game);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("List", "Games");

        }

        private async Task<bool> AlreadyPlayed(Game game) // method to check if there are already matches played with this game in order to fully remove them or just disable them
        {
            var rooms = await _context.Matches
                .Where(r => r.Game == game)
                .ToListAsync();

            if (rooms.Any())
            {
                return true;
            }

            return false;
        }

        // method for selecting recommended games list
        public async Task<List<Game>> RecommendedGames(int userId)
        {
            var currentUserFavoriteGameIds = await _context.FavoriteGames
                .Where(fg => fg.UserId == userId && fg.Game.IsOfficial)
                .Select(fg => fg.GameId)
                .ToListAsync(); // search for current user's favorite games

            if (!currentUserFavoriteGameIds.Any())
            {
                return new List<Game>();
            }

            var similarUsers = await _context.FavoriteGames
                .Where(fg => currentUserFavoriteGameIds.Contains(fg.GameId) && fg.UserId != userId) // serach for similar users where they have the same games as the users
                .GroupBy(fg => fg.UserId) // group by user ids
                .Select(g => new
                {
                    UserId = g.Key,
                    SharedFavoriteGamesCount = g.Count()
                }) // select the number of shared favorite games
                .OrderByDescending(x => x.SharedFavoriteGamesCount) // order from the most to least 
                .ToListAsync();

            var similarUserIds = similarUsers
                .Select(su => su.UserId)
                .ToList(); // select the user ids to list

            if (!similarUserIds.Any())
            {
                return new List<Game>(); 
            }

            var recommendedGameCandidates = await _context.FavoriteGames
                .Include(fg => fg.Game) 
                .Where(fg => similarUserIds.Contains(fg.UserId) &&
                            fg.Game.IsOfficial &&
                            !currentUserFavoriteGameIds.Contains(fg.GameId)) // select the games by the user ids found and if the game is official and not already in the favorites list
                .GroupBy(fg => fg.GameId)
                .Select(g => new
                {
                    GameId = g.Key, // select the gameid
                    Game = g.First().Game, // select the game
                    FavoritedBySimilarUsersCount = g.Count() // select the number of people that has the game as favorite
                })
                .ToListAsync();

            var recommendedGames = new List<Game>(); // empty list for favorited games
            var totalSimilarUsers = similarUserIds.Count; // number of users that have similar games 

            foreach (var candidate in recommendedGameCandidates)
            {
                if (totalSimilarUsers > 0) // check if the number is more than 0
                {
                    double percentage = (double)candidate.FavoritedBySimilarUsersCount / totalSimilarUsers * 100; // calculate the percentage

                    if (percentage >= RECOMMENDATION_PERCENTAGE)
                    {
                        recommendedGames.Add(candidate.Game); // if the percentage is higher than the preset percentage add it to the list
                    }
                }
            }

            return recommendedGames.Distinct().ToList(); // return the distinct list of games
        }

    }
}
