using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using TrackMyScore.Database;
using TrackMyScore.Models;
using TrackMyScore.Services;

namespace TrackMyScore.Controllers
{
    [Route("[controller]/[action]/{id?}")]
    public class TournamentController : Controller
    {
        private readonly AppDbContext _context;
        private static readonly char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
        private static readonly Random random = new Random();
        private readonly UserService _userService;

        private string GenerateTournamentCode(){ // method for generating a new code of 6 characters for tournaments
            return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public TournamentController(AppDbContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
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
        public async Task<IActionResult> Create(string name, int playerNumber, int roomCount, string reward, string location, DateOnly startDate, TimeOnly startTime, int gameId, string mode)
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
                    Location = location,
                    Winner = ""
                };

                await _context.Tournaments.AddAsync(newTournament);

                for(int i = 1; i<= roomCount; i++){ // creating rooms for single mode tournament
                    var newRoom = new Room{
                        Name = "Room " + i + " - Tournament: " + name,
                        Player = host,
                        Game = game,
                        Tournament = newTournament,
                        Location = location,
                        Stage = -1,
                        Mode = "single",
                        Type = mode,
                    };
                    await _context.Rooms.AddAsync(newRoom);
                }
                await _context.SaveChangesAsync();
                return RedirectToAction("CurrentTournament", "Tournament", new {id = newTournament.Id});


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
                    Winner = "",
                    Location = location
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
                        Location = location
                    };               
                    await _context.Rooms.AddAsync(newRoom);
                }
                await _context.SaveChangesAsync(); // saving changes

                return RedirectToAction("CurrentTournament", "Tournament", new {id = newTournament.Id}); // redirecting to newly created tournament view
            }

            return NotFound(); // returns not found if doesnt create the tournament
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {

            var user = await GetLoggedUserAsync();
            var tournaments = await _context.Tournaments
                .Include(t => t.Host)
                .Include(t => t.Game)
                .Where(t => t.Host.Id != user.Id)
                .ToListAsync();

            var myTournaments = await _context.Tournaments
                .Include(t => t.Host)
                .Include(t => t.Game)
                .Where(t => t.Host.Id == user.Id)
                .ToListAsync();

            TournamentListView tournamentList = new TournamentListView(tournaments, myTournaments);

            return View(tournamentList);
        }


        [HttpGet]
        public IActionResult Play()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CurrentTournament(int id) {
            var user = await GetLoggedUserAsync();
            var mutualFollowers = await GetMutualFollowersAsync(user);
            List<Team> teams = null;

            var tournament = await _context.Tournaments
                .Include(t => t.Host)
                .Include(t => t.Game)
                .FirstOrDefaultAsync(t => t.Id == id);
    
            if(tournament == null) {
                return NotFound();
            }

            var players = await _context.Players
                .Where(p => p.Tournament.Id == id)
                .Include(p => p.Team)
                .ToListAsync();

            if(tournament.Type == "team"){
                foreach(var player in players){
                    teams = await _context.Players
                        .Where(p => p.User.Id == player.Id && p.Tournament.Id == id)
                        .Select(p => p.Team)
                        .Distinct()
                        .ToListAsync();
                }
            }
    
            var rooms = await _context.Rooms
                .Where(r => r.Tournament.Id == id)
                .ToListAsync();

            var model = new TournamentModel(user, tournament, players, rooms, teams, mutualFollowers);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> JoinSingle(int id, string code)
        {
                var user = await GetLoggedUserAsync();

                if (user == null) 
                    return RedirectToAction("Login", "Account");

                var tournament = await _context.Tournaments
                    .Include(t => t.Game)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tournament == null){
                    return Json(new { success = false, message = "Tournament not found."});
                }

                if (string.IsNullOrEmpty(code) || tournament.Code != code){
                    return Json(new { success = false, message = "Invalid tournament code."});
                }

                var existingPlayer = await _context.Players
                    .AnyAsync(p => p.Tournament.Id == id && p.User.Id == user.Id);
                if (existingPlayer){
                    return Json(new { success = false, message = "You already joined this tournament."});
                }

                if (tournament.Type == "single")
                {
                    var player = new Player {
                        User = user,
                        Tournament = tournament,
                        Eliminated = false,
                        RespectPoints = 1
                    };
                    _context.Players.Add(player);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Joined successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> JoinTeam(int id, string code, List<int>? teammates, string? teamName){

            var user = await GetLoggedUserAsync();

            if (user == null) 
                return RedirectToAction("Login", "Account");

            var tournament = await _context.Tournaments
                .Include(t => t.Game)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null){
                return Json(new { success = false, message = "Tournament not found."});
            }

            if (string.IsNullOrEmpty(code) || tournament.Code != code){
                return Json(new { success = false, message = "Invalid tournament code."});
            }

            var existingPlayer = await _context.Players
                .AnyAsync(p => p.Tournament.Id == id && p.User.Id == user.Id);
            if (existingPlayer){
                return Json(new { success = false, message = "You already joined this tournament."});
            }

            if (tournament.Type == "team")
            {
                if (string.IsNullOrEmpty(teamName) || teammates == null || !teammates.Any()){
                    return Json(new { success = false, message = "Team name and at least one teammate required."});
                }

                var team = new Team { Name = teamName };
                _context.Teams.Add(team);

                    var allMembers = new List<User> { user };
                var teammateUsers = await _context.Users
                    .Where(u => teammates.Contains(u.Id))
                    .ToListAsync();
                allMembers.AddRange(teammateUsers);

                foreach (var member in allMembers)
                {
                    _context.Players.Add(new Player {
                        User = member,
                        Tournament = tournament,
                        Team = team,
                        Eliminated = false,
                        RespectPoints = 1
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Joined successfully" });  
        }

        private async Task<List<User>> GetMutualFollowersAsync(User user){
            var following = await _context.Followers
                .Where(f => f.Follower.Id == user.Id)
                .Select(f => f.Following)
                .ToListAsync();

            var followers = await _context.Followers
                .Where(f => f.Following.Id == user.Id)
                .Select(f => f.Follower)
                .ToListAsync();

            return following.Intersect(followers).ToList();
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
        public async Task<IActionResult> Delete(int id){ // delete tournament

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
        public async Task<IActionResult> Start(int id){ // start tournament

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
                    .Include(p => p.User)
                    .ToListAsync();

                foreach(var room in rooms){
                    playerNumber = 0;
                    foreach(var player in players){
                        if(playerNumber == 2){
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
                    }

                    await _context.SaveChangesAsync();
                }


            } else if (tournament.Type == "team") {
                var rooms = await _context.Rooms
                    .Where(r => r.Tournament.Id == id)
                    .ToListAsync();

                var teams = await _context.Players
                    .Where(p => p.Tournament.Id == id)
                    .Include(p => p.Team)
                    .Include(p => p.User)
                    .GroupBy(p => p.Team)
                    .ToListAsync();
    
                foreach(var room in rooms){
                    int teamsInRoom = 0;
                    foreach(var team in teams){
                        if(teamsInRoom >= 2){ // max 2 teams per room
                            break;
                        }

                        foreach(var player in team){
                            await _context.JoinRooms.AddAsync(
                                new JoinRoom{
                                    User = player.User,
                                    Room = room
                                }
                            );
                        }
                        teamsInRoom++;
                    }
                }

                await _context.SaveChangesAsync();
            }

            return Json(new {success = true, message = "Tournament started."});
        }

        [HttpPost]
        public async Task<IActionResult> EndBracket(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Tournament)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null)
            {
                return Json(new { success = false, message = "Room not found." });
            }

            var tournament = room.Tournament;
            var match = await _context.Matches
                .FirstOrDefaultAsync(m => m.Room.Id == roomId);

            if (match == null)
            {
                return Json(new { success = false, message = "Match not found." });
            }

            RoomController roomController = new RoomController(_context, _userService);
            await roomController.End(match.Id);

            var winner = match.Winner;

            // Find all rooms in the current stage
            var currentStageRooms = await _context.Rooms
                .Where(r => r.Tournament.Id == tournament.Id && r.Stage == room.Stage)
                .ToListAsync();

            // Check if all rooms in current stage have completed
            bool allRoomsCompleted = currentStageRooms.All(r => 
                _context.Matches.Any(m => m.Room.Id == r.Id && m.Room.Stage == -2));

            if (!allRoomsCompleted)
            {
                // Wait for other matches in this stage to complete
                return Json(new { success = true, message = "Match ended. Waiting for other matches in this stage to complete." });
            }

            // If this is the final stage (stage 0), set tournament winner
            if (tournament.IsActive == false)
            {
                tournament.Winner = winner;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Tournament ended. Winner: " + winner });
            }

            // Create rooms for next stage (half as many rooms)
            int nextStage = room.Stage - 1;
            int roomsToCreate = currentStageRooms.Count / 2;

            for (int i = 0; i < roomsToCreate; i++)
            {
                var newRoom = new Room
                {
                    Name = "Room " + i + " - " + tournament.Name,
                    Tournament = tournament,
                    Stage = nextStage,
                    Game = tournament.Game,
                    Player = tournament.Host
                };
                await _context.Rooms.AddAsync(newRoom);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Next stage rooms created." });
        }
       
    }

    // tournament start, end, brackets = room / 2, location for tournament (optional) // dont edit this
}

