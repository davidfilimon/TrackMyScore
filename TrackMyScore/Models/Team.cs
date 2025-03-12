using System.ComponentModel.DataAnnotations;

namespace TrackMyScore.Models
{
    public class Team
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public Participant Participant { get; set; }
    }
}
