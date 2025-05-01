using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class Participant
    {
        public int Id { get; set; }
        public string Role { get; set; }

        [Required]
        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        [ForeignKey("MatchId")]
        public Match Match { get; set; }

        [ForeignKey("TeamId")]
        public Team? Team { get; set; }

        public Participant()
        {
        }
    }
}
