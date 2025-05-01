namespace TrackMyScore.Models
{
    public class UserGamesModel
    {
        // class for only passing data into the view
        public User User { get; set; }
        public List<Game> CustomGames { get; set; }
        public List<Participant> Matches { get; set; }

        public UserGamesModel()
        {
            
        }
    }
}
