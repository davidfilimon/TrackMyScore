using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class Tournament
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public int MaxPlayers { get; set; }
        [Required]
        [ForeignKey("RewardId")]
        public Reward Reward { get; set; }

        [Required]
        public Players Players { get; set; }
        
        public Tournament()
        {
            
        }
    }
}
