namespace TrackMyScore.Models
{
    public class TournamentModel
    {
        
        public User LoggedUser { get; set; }
        public Tournament Tournament { get; set; }
        public List<Player>? SinglePlayers { get; set; }
        public List<TeamPlayer>? TeamPlayers { get; set; }
        public List<Match> Matches { get; set; }
        public List<Team>? Teams { get; set; }
        public List<User> MutualFollowers { get; set; }
        public Match ModelMatch { get; set; }
        public string TournamentWinner { get; set; }

        public TournamentModel()
        {

        }
        

    }
}
