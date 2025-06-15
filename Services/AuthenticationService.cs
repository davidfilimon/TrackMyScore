using Microsoft.EntityFrameworkCore;
using TrackMyScore.Database;
using TrackMyScore.Models;

namespace TrackMyScore.Services
{
    public class AuthenticationService
    {
        private readonly AppDbContext _context;

        public AuthenticationService(AppDbContext context)
        {
            _context = context;
        }

        // method for logging in
        public async Task<User?> Login(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !PasswordHasher.Verify(password, user.Password))
                return null;
            return user;
        }

        // method for checking the password
        public bool PasswordMatch(User user, string oldPassword)
        {
            return user != null && PasswordHasher.Verify(oldPassword, user.Password);
        }

        // method for changing the password
        public async Task<bool> ChangePasswordAsync(User user, string newPassword)
        {
            try
            {
                user.Password = PasswordHasher.Hash(newPassword);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.Write($"Error:{ex.Message}");
                return false;
            }
            
        }
    }
}
