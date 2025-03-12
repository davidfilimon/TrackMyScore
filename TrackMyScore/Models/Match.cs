using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class Match
    {
        public int Id { get; set; }
        [Required]
        public string Type { get; set; }
        [Required]        
        public string Score { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
        [Required]
        [ForeignKey("Id")]
        public Room Room { get; set; }
        
        public Team Teams { get; set; }

        public Match()
        {
            
        }
    }
}
