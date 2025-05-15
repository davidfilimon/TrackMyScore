using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class Participant
    {
        public int Id { get; set; }
        public string Role { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int MatchId { get; set; }
        public Match Match { get; set; }

        public int? TeamId { get; set; }
        public Team? Team { get; set; }
        
        [Required]
        public int Score { get; set; } = 0;
        public Participant()
        {
        }
    }
}
