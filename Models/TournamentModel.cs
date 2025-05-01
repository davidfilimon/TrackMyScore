namespace TrackMyScore.Models
{
    public class TournamentModel
    {
        
        public User Host { get; set; }
        public Tournament Tournament { get; set; }
        public List<Player> Players { get; set; }
        public List<Room> Rooms { get; set; }

        public TournamentModel(User host, Tournament tournament, List<Player> players, List<Room> rooms)
        {
            Host = host;
            Tournament = tournament;
            Players = players;
            Rooms = rooms;
        }

    }
}
