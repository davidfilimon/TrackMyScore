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
        [ForeignKey("RoomId")]
        public Room Room { get; set; }
        [Required]
        [ForeignKey("ParticipantId")]
        public Participant Participant { get; set; }
        [ForeignKey("TeamId")]
        public Team? Teams { get; set; }

        public Match()
        {
            
        }
    }
}
