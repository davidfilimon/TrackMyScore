using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackMyScore.Database;
using TrackMyScore.Models;

namespace TrackMyScore.Controllers
{
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
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string name, int playerNumber, int roomCount, string reward, DateOnly startDate, TimeOnly startTime, Game game, string mode)
        {

            var startDateTime = startDate.ToDateTime(startTime);
            var host = await GetLoggedUserAsync();
            var code = GenerateRoomCode();

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
            }
            

            await _context.SaveChangesAsync();

            return RedirectToAction("CurrentTournament", "Tournament"/*, new {newTournament.Id}*/);
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
    }
}
