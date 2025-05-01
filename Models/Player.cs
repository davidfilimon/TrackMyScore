using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class Player
    {
 
        public int Id { get; set; }
        [Required]
        [ForeignKey("UserId")]
        public User User { get; set; }
        [Required]
        [ForeignKey("TournamentId")]
        public Tournament Tournament { get; set; }
        [Required]
        public bool Eliminated { get; set; }
        [ForeignKey("TeamId")]
        public Team? Team { get; set; }
        [Required]
        public int RespectPoints { get; set; }
        public Player()
        {
            
        }

    }
}
