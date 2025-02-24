﻿using Microsoft.EntityFrameworkCore;
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
                .FirstOrDefaultAsync(fg => fg.UserId == user.Id && fg.GameId == id);

            return favoriteGame == null;

        }

    }
}
