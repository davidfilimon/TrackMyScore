using Microsoft.EntityFrameworkCore;
using System.Text;
using TrackMyScore.Database;
using TrackMyScore.Models;

namespace TrackMyScore.Services
{
    public class CreateAccountService
    {
        private readonly AppDbContext _context;

        public CreateAccountService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool success, string message)> Register(string username, string email, string password)
        {
            var existingUsername = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if(existingUsername != null)
            {
                return (false, "Another account is already using this username.");
            }

            var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if(existingEmail != null)
            {
                return (false, "Another account is already registered with that email address.");
            }

            var newUser = new User
            {
                Username = username,
                Email = email,
                Password = encrypt(password),
                AccountCreationDate = DateOnly.FromDateTime(DateTime.Today),
                RespectPoints = 0
            };

            _context.Add(newUser);

            await _context.SaveChangesAsync();

            return (true, "Account successfully created.");

        }

        private static string encrypt(string password)
        {
            try
            {
                byte[] encData_byte = new byte[password.Length];
                encData_byte = Encoding.UTF8.GetBytes(password);
                string encoded = Convert.ToBase64String(encData_byte);
                return encoded;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in base64Encode" + ex.Message);
            }
        }
    }
}

