using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class Tournament
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public int RoomCount { get; set; }

        [Required]
        public int MaxPlayers { get; set; }

        [Required]
        public string Code { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public int Stage { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public string Reward { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public int HostId { get; set; }
        public User Host { get; set; }
        
        [Required]
        public string Location { get; set; }

        [Required]
        public int GameId { get; set; }
        public Game Game { get; set; }
        
        public string? Winner { get; set; }
        
        public Tournament()
        {
            
        }
    }
}
