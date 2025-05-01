using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class FavoriteGame
    {

        [Key]
        public int Id { get; set; }
        [Required]
        [ForeignKey("UserId")]
        public User User { get; set; }
        [Required]
        [ForeignKey("GameId")]
        public Game Game { get; set; }

        public FavoriteGame()
        {
            
        }

    }
}
