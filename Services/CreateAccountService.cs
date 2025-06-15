using Microsoft.EntityFrameworkCore;
using System.Text;
using TrackMyScore.Database;
using TrackMyScore.Models;
namespace TrackMyScore.Services
{
    public class CreateAccountService
    {
        private readonly AppDbContext _context;
        private readonly ValidationService _validationService;

        public CreateAccountService(AppDbContext context, ValidationService validationService)
        {
            _context = context;
            _validationService = validationService;
        }

        public async Task<(bool success, string message)> Register(string username, string email, string password)
        { // registering the account
            var existingUsername = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (!_validationService.invalidEmail(email) || !_validationService.invalidPassword(password))
            {
                return (false, "Invalid email or password. Password must be at least 8 characters long."); // invalid password check
            }

            if (existingUsername != null)
            {
                return (false, "Another account is already using this username."); // username existence check
            }

            var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if(existingEmail != null) // email existence check
            {
                return (false, "Another account is already registered with that email address.");
            }

            string hash = PasswordHasher.Hash(password);

            var newUser = new User
            {
                Username = username,
                Email = email,
                Password = hash,
                AccountCreationDate = DateOnly.FromDateTime(DateTime.Today),
                RespectPoints = 0
            }; // user creation

            _context.Users.Add(newUser);

            await _context.SaveChangesAsync();

            return (true, "Account successfully created.");

        }
    }
}

