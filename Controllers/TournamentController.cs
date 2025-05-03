using Microsoft.AspNetCore.Identity;
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

        private string GenerateTournamentCode(){ // method for generating a new code of 6 characters for tournaments
            return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public TournamentController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<User> GetLoggedUserAsync() // getting logged user
        {
            string email = HttpContext.Session.GetString("email");

            if (email == null)
            {
                return null;
            }

            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        [HttpGet]
        public async Task<IActionResult> Create() // create tournament view page
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
        { // method for creating tournaments

            var startDateTime = startDate.ToDateTime(startTime); // converting date and time to datetime
            var host = await GetLoggedUserAsync();
            var code = GenerateTournamentCode();
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
            int currentTournamentId = -1;

            var existingCode = await _context.Tournaments.FirstOrDefaultAsync(c => c.Code == code);                 
            while (existingCode != null)
            {
                code = GenerateTournamentCode();
                existingCode = await _context.Tournaments.FirstOrDefaultAsync(c => c.Code == code);
            }

            if(mode == "single"){

                var newTournament = new Tournament // single mode tournament
                {
                    Name = name,
                    MaxPlayers = 0,
                    Reward = reward,
                    StartDate = startDateTime,
                    Code = code,
                    RoomCount = roomCount,
                    IsActive = true,
                    Game = game,
                    Type = mode,
                    Host = host,
                    Winner = ""
                };

                await _context.Tournaments.AddAsync(newTournament);

                for(int i = 1; i<= roomCount; i++){ // creating rooms for single mode tournament
                    var newRoom = new Room{
                        Name = "Room " + i + " - Tournament: " + name,
                        Player = host,
                        Game = game,
                        Tournament = newTournament,
                        Stage = -1,
                        Mode = "single",
                        Type = mode,
                    };
                    await _context.Rooms.AddAsync(newRoom);
                }

                currentTournamentId = newTournament.Id;

            } else if (mode == "team"){ // team mode tournament
                var newTournament = new Tournament
                {
                    Name = name,
                    MaxPlayers = playerNumber,
                    Reward = reward,
                    StartDate = startDateTime,
                    RoomCount = roomCount,
                    Code = code,
                    Game = game,
                    IsActive = true,
                    Type = mode,
                    Host = host,
                    Winner = ""
                };

                await _context.Tournaments.AddAsync(newTournament);

                for(int i = 1; i<= roomCount; i++){ // creating rooms for team mode tournament
                    var newRoom = new Room{
                        Name = "Room " + i + " - Tournament: " + name,
                        Player = host,
                        Game = game,
                        Tournament = newTournament,
                        Stage = -1,
                        Mode = "team",
                        Type = mode,
                        StartDate = startDateTime,
                        Location = "-"
                    };               
                    await _context.Rooms.AddAsync(newRoom);
                }
            await _context.Tournaments.AddAsync(newTournament);
            currentTournamentId = newTournament.Id;
            }

            if(currentTournamentId == -1){ // check if tournament was created
                return NotFound();
            }
            
            await _context.SaveChangesAsync(); // saving changes

            return RedirectToAction("CurrentTournament", "Tournament", new {currentTournamentId}); // redirecting to newly created tournament view
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
        public async Task<IActionResult> CurrentTournament(int id){ // method for tournament view
            var user = await GetLoggedUserAsync();
            var tournament = await _context.Tournaments.FirstOrDefaultAsync(t => t.Id == id);
            Dictionary<Team?, List<User>> teams = new Dictionary<Team?, List<User>>();

            if(tournament == null){
                return NotFound();
            }

            if(tournament.Type == "team"){
                var teamPlayers = await _context.Players
                    .Where(t => t.Tournament.Id == id)
                    .GroupBy(t => t.Team)
                    .Select(g => new {
                        Team = g.Key,
                        Players = g.ToList()
                    })
                    .ToListAsync();
    
                foreach(var teamGroup in teamPlayers){
                    if(teamGroup.Team != null && teamGroup.Players.Count > 0){
                        teams.Add(teamGroup.Team,
                        teamGroup.Players
                        .Select(p => p.User).ToList());
                    }
                }

            }

            List<User> players = await _context.Players
                .Where(p => p.Tournament.Id == id)
                .Select(p => p.User)
                .ToListAsync();
            
            List<Room> rooms = await _context.Rooms
                .Where(r => r.Tournament.Id == id)
                .ToListAsync();

            TournamentModel model = new TournamentModel(user, tournament, players, rooms, teams);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Join(int id, string code, List<User>? teammates, string? teamName){
            var user = await GetLoggedUserAsync();
            var tournament = await _context.Tournaments.FirstOrDefaultAsync(t => t.Id == id);
            
            if(tournament == null){
                return NotFound();
            }

            if(tournament.Code == code){ // joining tournament
                if(tournament.Type == "single"){ // single mode tournament

                        var joinedPlayersNumber = await _context.Players
                            .Where(t => t.Tournament.Id == id)
                            .CountAsync();

                        if(tournament.Game.MaxPlayers * tournament.RoomCount <= joinedPlayersNumber){
                            return Json(new {success = false, message = "Tournament is full."});
                        }

                        var player = new Player{
                            User = user,
                            Tournament = tournament
                        };

                        await _context.Players.AddAsync(player);
                        await _context.SaveChangesAsync();
                    }

                else if(tournament.Type == "team" && teammates != null && teammates.Count > 1 && teamName != ""){ // team mode tournament

                        var joinedTeamsCount = await _context.Players
                            .Where(t => t.Tournament.Id == id)
                            .GroupBy(t => t.Team)
                            .CountAsync();

                        if(tournament.MaxPlayers < teammates.Count){
                            return Json(new {success = false, message = "The team is too big."});
                        }

                        if(tournament.Game.MaxPlayers * tournament.RoomCount == joinedTeamsCount){
                            return Json(new {success = false, message = "Tournament is full."});
                        }

                        teammates.Add(user);

                        var team = new Team{
                            Name = teamName,
                        };

                        foreach(var teammate in teammates){
                            var player = new Player{
                                User = teammate,
                                Tournament = tournament,
                                Team = team
                            };
                            await _context.Players.AddAsync(player);
                        }

                        await _context.SaveChangesAsync();
                }

            }

            return Json(new {success = true, message = "Joined tournament."});

        }   

        [HttpPost]
        public async Task<IActionResult> Leave(int id){ // method for leaving the tournament

            var user = await GetLoggedUserAsync();
            var tournament = await _context.Tournaments
                .FirstOrDefaultAsync(t => t.Id == id);

            if(tournament == null){
                return NotFound();
            }

            if(tournament.Type == "single"){ // removing the player
                var player = await _context.Players
                    .FirstOrDefaultAsync(p => p.Tournament.Id == id && p.User.Id == user.Id);

                if(player == null){
                    return NotFound();
                }

                _context.Players.Remove(player);
                await _context.SaveChangesAsync();

                return Json(new {success = true, message = "Left tournament."});
            }

            if(tournament.Type == "team"){ // removing the team of the player and the teammates
                var player = await _context.Players
                    .FirstOrDefaultAsync(p => p.Tournament.Id == id && p.User.Id == user.Id);

                if(player == null){
                    return Json(new {success = false, message = "Player not found."});
                }   

                var team = await _context.Teams
                    .FirstOrDefaultAsync(t => t.Id == player.Team.Id);

                if(team == null){
                    return Json(new {success = false, message = "Team not found."});
                }

                var teamPlayers = await _context.Players
                    .Where(p => p.Tournament.Id == id && p.Team.Id == team.Id)
                    .ToListAsync();

                foreach(var p in teamPlayers){
                    _context.Players.Remove(p);
                }

                _context.Teams.Remove(team);
                await _context.SaveChangesAsync();

                return Json(new {success = true, message = "Left tournament."});
            }

            return Json(new {success = false, message = "Tournament not found or you are not taking part in it."});
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id){

            var user = await GetLoggedUserAsync();

            if(user == null){
                return Json(new {success = false, message = "User not found."});
            }           

            var tournament = await _context.Tournaments
                .Include(t => t.Host)
                .FirstOrDefaultAsync(t => t.Id == id);

            if(tournament == null){
                return Json(new {success = false, message = "Tournament not found."});
            }

            if(user != tournament.Host){
                return Json(new {success = false, message = "You are not the host of this tournament."});
            }

            if(!tournament.IsActive){
                return Json(new {success = false, message = "Tournament already ended."});
            }

            var players = await _context.Players
                .Where(p => p.Tournament.Id == id)
                .ToListAsync();

            var rooms = await _context.Rooms
                .Where(r => r.Tournament.Id == id)
                .ToListAsync();

            foreach(var player in players){
                _context.Players.Remove(player);
            }

            foreach(var room in rooms){
                _context.Rooms.Remove(room);
            }

            _context.Tournaments.Remove(tournament);
            await _context.SaveChangesAsync();
            return Json(new {success = true, message = "Tournament deleted."});
        }

        [HttpPost]
        public async Task<IActionResult> Start(int id){

            var tournament = await _context.Tournaments
                .Include(t => t.Host)
                .FirstOrDefaultAsync(t => t.Id == id);
            var user = await GetLoggedUserAsync();
            int playerNumber = 0;

            if(tournament == null){
                return Json(new {success = false, message = "Tournament not found."});
            }

            if(tournament.Host != user){
                return Json(new {success = false, message = "You are not the host of this tournament."});
            }

            if(tournament.Type == "single"){
                var rooms = await _context.Rooms
                    .Where(r => r.Tournament.Id == id)
                    .ToListAsync();

                var players = await _context.Players
                    .Where(p => p.Tournament.Id == id)
                    .ToListAsync();

                foreach(var room in rooms){
                    playerNumber = 0;
                    foreach(var player in players){
                        if(room.Game.MaxPlayers == playerNumber){
                            break;
                        }

                        var addToRooms = await _context.JoinRooms
                            .AddAsync(
                                new JoinRoom{
                                    User = player.User,
                                    Room = room
                                }
                            );

                        playerNumber++;
                    } // Room is full

                    await _context.SaveChangesAsync();
                }


            } else if (tournament.Type == "team") {
                int teamNumber = 0;

                var rooms = await _context.Rooms
                    .Where(r => r.Tournament.Id == id)
                    .ToListAsync();

                Dictionary<Team?, List<User>> teams = new Dictionary<Team?, List<User>>();

                teams = await _context.Players
                    .Where(p => p.Tournament.Id == id)
                    .GroupBy(p => p.Team)
                    .ToDictionaryAsync(g => g.Key,
                        g => g.ToList().Select(p => p.User).ToList());

                foreach(var room in rooms){
                    teamNumber = 0;
                    foreach(var team in teams){
                        if(room.Game.MaxPlayers <= teamNumber){
                            break;
                        }
                        foreach(var u in team.Value){
                            await _context.JoinRooms
                                .AddAsync(
                                    new JoinRoom{
                                        User = u,
                                        Room = room
                                    }
                                );
                        }
                        teamNumber++;
                    } // Room is full
                }
            }

            await _context.SaveChangesAsync();

            return Json(new {success = true, message = "Tournament started."});
        }

        // tournament start, end, brackets = room / 2, location for tournament (optional)
    }
}
