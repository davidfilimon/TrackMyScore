using System.Text;

namespace TrackMyScore.Services
{
    public class ValidationService
    {
        public bool invalidEmail(string email)
        { // check if the email is invalid
            return !string.IsNullOrEmpty(email) && email.Contains('@');
        }

        public bool invalidPassword(string password)
        { // check if the password is invalid
            return !string.IsNullOrEmpty(password) && password.Length >= 8;
        }
        
    }
}
