using System.ComponentModel.DataAnnotations;

namespace TrackMyScore.Models
{
    public class Follower
    {
        public int Id { get; set; }
        [Required]
        public int FollowerId { get; set; }
        [Required]
        public int FollowingId { get; set; }
        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

        public Follower()
        {
            
        }

    }
}
