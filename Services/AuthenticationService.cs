using Microsoft.EntityFrameworkCore;
using System.Text;
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

        public async Task<User> Login(string email, string password)
        {
            User user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if(user == null || decrypt(user.Password) != password)
            {
                return null;
            }

            return user;
        }

        private static string decrypt(string encoded)
        {
            UTF8Encoding encoder = new UTF8Encoding();
            Decoder utf8Decode = encoder.GetDecoder();
            byte[] todecode_byte = Convert.FromBase64String(encoded);
            int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
            char[] decodedChar = new char[charCount];
            utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decodedChar, 0);
            string result = new string(decodedChar);
            return result;
        }

    }
}
