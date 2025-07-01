using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
        private const int GAME_PLAYER_RECOMMENDATION = 3;

        public GamesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var user = await GetLoggedUserAsync();

            List<Game> favoriteGames = new();

            if (user != null)
            {
                favoriteGames = await RecommendedGames(user.Id);
            }
            
            var games = await _context.Games
                .Include(g => g.Author)
                .Where(g => !g.Deleted)
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
        public async Task<IActionResult> AddGame(string name, string description, int maxPlayers, string difficulty)
        { // method for adding a game

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description) || string.IsNullOrEmpty(difficulty) || maxPlayers == 0)
            {
                ViewData["Error"] = "All fields are required to register a game.";
                return View();
            }

            if (name.Length > 50)
            {
                ViewData["Error"] = "The name of the game is too long, please insert a shorter name.";
                return View();
            }

            string email = HttpContext.Session.GetString("email");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var gameExists = await _context.Games.AnyAsync(u => u.Author == user && u.Title == name);

            if (gameExists)
            {
                ViewData["Error"] = "You already have added a game with the same name to your library. Please choose another one and try again.";
                return View();
            }

            if (maxPlayers > 32 || maxPlayers < 2)
            {
                ViewData["Error"] = "Invalid player number. Please either lower or higher the player count.";
                return View();
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
            var user = await GetLoggedUserAsync();

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
                .FirstOrDefaultAsync(fg => fg.User.Id == user.Id && fg.Game.Id == game.Id);

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
        public async Task<IActionResult> Details(int id)
        {
            var game = await _context.Games
                .Include(g => g.Author)
                .FirstOrDefaultAsync(g => g.Id == id);             

            if (game == null || game.Deleted)
            {
                return RedirectToAction("List", "Game");
            }

            var model = new GameDetailView
            {
                Game = game,
                TopUsers = new List<UserStatViewModel>()
            };

            if (game.IsOfficial)
            {
                var userStats = new Dictionary<int, (string Username, int Played, int Won)>();

                // single matches
                var singleMatches = await _context.Matches
                    .Where(m => m.GameId == id && m.Stage == -2 && m.Tournament == null && m.Mode == "single")
                    .ToListAsync();

                foreach (var match in singleMatches)
                {
                    var players = await _context.Players
                        .Include(p => p.User)
                        .Where(p => p.MatchId == match.Id)
                        .ToListAsync();

                    if (!players.Any()) continue;

                    int maxScore = players.Max(p => p.Score);
                    var winners = players.Where(p => p.Score == maxScore).ToList();

                    foreach (var player in players)
                    {
                        int uid = player.User.Id;
                        if (!userStats.ContainsKey(uid))
                            userStats[uid] = (player.User.Username, 0, 0);

                        var stats = userStats[uid];
                        stats.Played++;
                        if (winners.Any(w => w.User.Id == uid))
                            stats.Won++;
                        userStats[uid] = stats;
                    }
                }

                // team matches
                var teamMatches = await _context.Matches
                    .Where(m => m.GameId == id && m.Stage == -2 && m.Tournament == null && m.Mode == "team")
                    .ToListAsync();

                foreach (var match in teamMatches)
                {
                    var players = await _context.TeamPlayers
                        .Include(tp => tp.User)
                        .Include(tp => tp.Team)
                        .Where(tp => tp.MatchId == match.Id)
                        .ToListAsync();

                    if (!players.Any()) continue;

                    var teamScores = players
                        .GroupBy(tp => tp.Team)
                        .Select(g => new { Team = g.Key, Score = g.Key.Score })
                        .ToList();

                    int maxScore = teamScores.Max(ts => ts.Score);
                    var winningTeams = teamScores
                        .Where(ts => ts.Score == maxScore)
                        .Select(ts => ts.Team.Id)
                        .ToList();

                    foreach (var tp in players)
                    {
                        int uid = tp.User.Id;
                        if (!userStats.ContainsKey(uid))
                            userStats[uid] = (tp.User.Username, 0, 0);

                        var stats = userStats[uid];
                        stats.Played++;
                        if (winningTeams.Contains(tp.TeamId))
                            stats.Won++;
                        userStats[uid] = stats;
                    }
                }

                // prepare top 5
                model.TopUsers = userStats.Values
                    .OrderByDescending(u => u.Won)
                    .ThenByDescending(u => u.Played)
                    .Take(5)
                    .Select(u => new UserStatViewModel
                    {
                        Username = u.Username,
                        Played = u.Played,
                        Won = u.Won
                    })
                    .ToList();
            }

            return View(model);
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
            var favoritesList = await _context.FavoriteGames
                .Where(fg => fg.UserId == userId && fg.Game.IsOfficial)
                .Select(fg => fg.GameId)
                .ToListAsync(); // search for current user's favorite games

            if (!favoritesList.Any())
            {
                return new List<Game>();
            }

            var similarPlayers = await _context.FavoriteGames
                .Where(fg => favoritesList.Contains(fg.GameId) && fg.UserId != userId) // search for similar users where they have the same games as the users
                .GroupBy(fg => fg.UserId) // group by user ids
                .Select(g => new
                {
                    UserId = g.Key,
                    sharedCount = g.Count()
                }) // select the number of shared favorite games
                .OrderByDescending(x => x.sharedCount) // order from the most to least 
                .ToListAsync();

            var similarPlayerIds = similarPlayers
                .Select(su => su.UserId)
                .ToList(); // select the user ids to list

            if (!similarPlayerIds.Any())
            {
                return new List<Game>();
            }

            var recGames = await _context.FavoriteGames
                .Include(fg => fg.Game)
                .Where(fg => similarPlayerIds.Contains(fg.UserId) &&
                            fg.Game.IsOfficial &&
                            !fg.Game.Deleted &&
                            !favoritesList.Contains(fg.GameId)) // select the games by the user ids found and if the game is official and not already in the favorites list
                .GroupBy(fg => fg.GameId)
                .Select(g => new
                {
                    GameId = g.Key, // select the gameid
                    Game = g.First().Game, // select the game
                    playerCount = g.Count() // select the number of people that has the game as favorite
                })
                .ToListAsync();

            var recommendedGames = new List<Game>(); // empty list for favorited games
            var totalSimilarPlayers = similarPlayerIds.Count; // number of users that have similar games 

            foreach (var g in recGames)
            {
                if (totalSimilarPlayers > 0 && g.playerCount >= GAME_PLAYER_RECOMMENDATION) // check if the number is more than 0
                {
                    double percentage = (double)g.playerCount / totalSimilarPlayers * 100; // calculate the percentage

                    if (percentage >= RECOMMENDATION_PERCENTAGE)
                    {
                        recommendedGames.Add(g.Game); // if the percentage is higher than the preset percentage add it to the list
                    }
                }
            }

            return recommendedGames.Distinct().ToList(); // return the distinct list of games
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
