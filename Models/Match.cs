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
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Winner { get; set; }
        [Required]
        [ForeignKey("RoomId")]
        public Room Room { get; set; }     

        public Match()
        {
        }
    }
}
