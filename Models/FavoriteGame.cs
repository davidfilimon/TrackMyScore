using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class FavoriteGame
    {

        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int GameId { get; set; }
        public Game Game { get; set; }

        public FavoriteGame()
        {
            
        }

    }
}
