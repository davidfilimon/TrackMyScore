using System.Text;

namespace TrackMyScore.Services
{
    public class ValidationService
    {
        public bool invalidEmail(string email)
        {
            return !string.IsNullOrEmpty(email) && email.Contains('@');
        }

        public bool invalidPassword(string password)
        {
            return !string.IsNullOrEmpty(password) && password.Length >= 8;
        }
        
    }
}
