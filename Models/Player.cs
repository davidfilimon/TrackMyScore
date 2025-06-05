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
        public int MatchId { get; set; }
        public Match Match { get; set; }

        public int Score { get; set; } = 0;
        public int Reward { get; set; } = 1;
        public bool Eliminated { get; set; } = false;        

        public Player()
        {
            
        }

    }
}
