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

        [Required]
        public int RespectPoints { get; set; }
        
        [Required]
        public bool isAdmin { get; set; }

        public int Picture { set; get; } = 0;


        public ICollection<Followers> followers;
        public ICollection<Followers> followings;

        public User()
        {
        }   

    }
}
