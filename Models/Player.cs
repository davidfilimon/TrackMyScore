using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class Player
    {
 
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }
        public User User { get; set; }
        
        [Required]
        public int TournamentId{ get; set; }
        public Tournament Tournament { get; set; }
        [Required]
        public bool Eliminated { get; set; }
        
        public int? TeamId { get; set; }
        public Team? Team { get; set; }

        [Required]
        public int RespectPoints { get; set; }
        public Player()
        {
            
        }

    }
}
