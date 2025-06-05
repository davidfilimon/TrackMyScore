namespace TrackMyScore.Models
{
    public class UserGamesModel
    {
        // class for only passing data into the view
        public User User { get; set; }
        public List<Game> CustomGames { get; set; }
        public List<Tournament> Tournaments { get; set; }
        public List<Match> Matches { get; set; }
        public Dictionary<int, string> MatchWinners{ get; set; }
        public Dictionary<int, string> TournamentWinners { get; set; }
        public int TotalMatchesPlayed { get; set; }
        public int MatchesWon { get; set; }
        public int TotalTournamentsPlayed { get; set; }
        public int TournamentsWon { get; set; }

        public UserGamesModel()
        {

        }
    }
}
