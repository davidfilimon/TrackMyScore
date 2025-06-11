using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Threading.Tasks;
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

            if (user == null || decrypt(user.Password) != password) // check if user's passwords match at login
            {
                return null;
            }

            return user;
        }

        public bool PasswordMatch(User user, string oldPassword)
        {
            if (user == null || decrypt(user.Password) != oldPassword) // check if the passwords match when changing the account's password
            {
                return false;
            }

            return true;
        }

        public bool ChangePassword(User user, string password)
        { // change the password

            if (user == null)
            {
                return false;
            }

            user.Password = encrypt(password);
            _context.SaveChanges();

            return true; 
        }

        private static string decrypt(string encoded) // password decryption method from utf-8
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

        private static string encrypt(string password)
        { // password encryption method to utf-8
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
