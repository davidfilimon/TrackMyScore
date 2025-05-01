using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class Followers
    {
        public int Id { get; set; }
        [Required]
        [ForeignKey("FollowerId")]
        public User Follower { get; set; }
        [Required]
        [ForeignKey("FollowingId")]
        public User Following { get; set; }
        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

        public Followers()
        {
            
        }

    }
}
