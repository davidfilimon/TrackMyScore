using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class Followers
    {
        public int Id { get; set; }
        
        [Required]
        public int FollowerId { get; set; }
        public User Follower { get; set; }

        [Required]
        public int FollowingId { get; set; }
        public User Following { get; set; }
        
        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

        public Followers()
        {
            
        }

    }
}
