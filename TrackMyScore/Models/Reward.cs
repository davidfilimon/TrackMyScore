using System.ComponentModel.DataAnnotations;

namespace TrackMyScore.Models
{
    public class Reward
    {
        
        public int Id { get; set; }
        [Required]
        public int Value { get; set; }

        public Reward()
        {
            
        }
    }
}
