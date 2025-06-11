using Microsoft.EntityFrameworkCore;
using TrackMyScore.Database;
using TrackMyScore.Models;

namespace TrackMyScore.Services
{
    public class UserService
    {

        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _contextAccessor;

        public UserService(AppDbContext context, IHttpContextAccessor contextAccessor)
        {
            _context = context;
            _contextAccessor = contextAccessor;
        }

        public async Task<User?> GetCurrentUserAsync()
        { // getting the logged user method
            var email = _contextAccessor.HttpContext?.Session.GetString("email");

            if (email == null)
            {
                return null;
            }

            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> IsGameFavorite(int id)
        { // check if the game is already in the favorites list
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return false;
            }

            var favoriteGame = await _context.FavoriteGames
                .FirstOrDefaultAsync(fg => fg.User.Id == user.Id && fg.Game.Id == id);

            return favoriteGame == null;

        }

        public bool IsAdmin(string email)
        { // check if the user is an admin
            if (email == null) return false;

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null) return false;

            if (user.isAdmin) return true;

            return false;
        }

        public async Task<bool> IsAuthor(string email, int gameId)
        { // check if the logged user is the author

            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
            {
                return false;
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (game.Author == user && user != null)
            {
                return true;
            }

            return false;
 

        }

    }
}
