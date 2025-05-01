using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class Game
    {

        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public int MaxPlayers { get; set; }
        [Required]
        public string Difficulty { get; set; }
        [Required]
        [ForeignKey("UserId")]
        public User Author { get; set; }
        public bool IsOfficial {  get; set; }

        public Game()
        {
        }

    }
}
