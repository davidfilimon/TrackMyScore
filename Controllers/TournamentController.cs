using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackMyScore.Database;
using TrackMyScore.Models;

namespace TrackMyScore.Controllers
{
    [Route("[controller]/[action]/{id?}")]
    public class TournamentController : Controller
    {

        private readonly AppDbContext _context;
        private static readonly char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
        private static readonly Random random = new Random();

        private string GenerateRoomCode(){
            return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public TournamentController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<User> GetLoggedUserAsync()
        {
            string email = HttpContext.Session.GetString("email");

            if (email == null)
            {
                return null;
            }

            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await GetLoggedUserAsync();

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var games = await _context.FavoriteGames
                                .Where(f => f.User.Id == user.Id)
                                .Select(f => f.Game)
                                .ToListAsync();

            return View(games);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string name, int playerNumber, int roomCount, string reward, DateOnly startDate, TimeOnly startTime, int gameId, string mode)
        {

            var startDateTime = startDate.ToDateTime(startTime);
            var host = await GetLoggedUserAsync();
            var code = GenerateRoomCode();
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
            int currentTournamentId = -1;

            var existingCode = await _context.Tournaments.FirstOrDefaultAsync(c => c.Code == code);                 
            while (existingCode != null)
            {
                code = GenerateRoomCode();
                existingCode = await _context.Tournaments.FirstOrDefaultAsync(c => c.Code == code);
            }

            if(mode == "single"){
                var newTournament = new Tournament
                {
                    Name = name,
                    MaxPlayers = 0,
                    Reward = reward,
                    StartDate = startDateTime,
                    Code = code,
                    RoomCount = roomCount,
                    IsActive = false,
                    Game = game,
                    Type = mode,
                    Host = host,
                    Winner = ""
                };

                await _context.Tournaments.AddAsync(newTournament);
                currentTournamentId = newTournament.Id;

            } else if (mode == "team"){
                var newTournament = new Tournament
                {
                    Name = name,
                    MaxPlayers = playerNumber,
                    Reward = reward,
                    StartDate = startDateTime,
                    RoomCount = roomCount,
                    Code = code,
                    Game = game,
                    IsActive = false,
                    Type = mode,
                    Host = host,
                    Winner = ""
                };
            await _context.Tournaments.AddAsync(newTournament);
            currentTournamentId = newTournament.Id;
            }
            
            await _context.SaveChangesAsync();

            return RedirectToAction("CurrentTournament", "Tournament", new {currentTournamentId});
        }

        public IActionResult Join()
        {
            return View();
        }


        [HttpGet]
        public IActionResult Play()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CurrentTournament(int id){
            var user = await GetLoggedUserAsync();
            var tournament = await _context.Tournaments.FirstOrDefaultAsync(t => t.Id == id);

            if(tournament == null){
                return NotFound();
            }

            List<Player> players = await _context.Players
                .Where(p => p.Tournament.Id == id)
                .ToListAsync();
            
            List<Room> rooms = await _context.Rooms
                .Where(r => r.Tournament.Id == id)
                .ToListAsync();

            TournamentModel model = new TournamentModel(user, tournament, players, rooms);

            return View(model);
        }
    }
}
