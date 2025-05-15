using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class Room
    {
        
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Type { get; set; }
        
        [Required]
        public string Location { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public int Stage { get; set; }

        [Required]
        public string Mode { get; set; }

        [Required]
        public int HostId { get; set; }
        public User Host { get; set; }

        [Required]
        public int GameId { get; set; }
        public Game Game { get; set; }

        public int? TournamentId { get; set; }
        public Tournament? Tournament { get; set; }

        public Room()
        {
        }

    }
}
