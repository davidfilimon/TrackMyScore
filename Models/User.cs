using System.ComponentModel.DataAnnotations;

namespace TrackMyScore.Models
{
    public class User
    {
        
        public int Id { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public DateOnly AccountCreationDate { get; set; }
        public int RespectPoints { get; set; }
        public bool isAdmin { get ; set; }

        public User()
        {
            isAdmin = false;
            RespectPoints = 0;
        }

        public User(string username, string password, string email, DateOnly currentDate)
        {
            Username = username;
            Password = password;
            Email = email;
            AccountCreationDate = currentDate;
        }

    }
}
