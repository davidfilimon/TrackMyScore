namespace TrackMyScore.Models
{
    public class UserGamesModel
    {
        public User User { get; set; }
        public List<Game> CustomGames { get; set; }

        public UserGamesModel()
        {
            
        }
    }
}
