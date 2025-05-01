using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class FavoriteGame
    {

        [Key]
        public int Id { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        [ForeignKey("Game")]
        public int GameId { get; set; }

        public FavoriteGame()
        {
            
        }

        public FavoriteGame(int userId, int gameId)
        {
            UserId = userId;
            GameId = gameId;
        }

    }
}
