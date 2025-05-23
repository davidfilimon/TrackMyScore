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
        {
            var email = _contextAccessor.HttpContext?.Session.GetString("email");

            if (email == null)
            {
                return null;
            }

            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> IsGameFavorite(int id)
        {
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
        {
            if (email == null) return false;

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user.isAdmin && user != null) return true;

            return false;
        }

        public async Task<bool> IsAuthor(string email, int gameId)
        {

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
