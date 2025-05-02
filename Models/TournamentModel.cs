namespace TrackMyScore.Models
{
    public class TournamentModel
    {
        
        public User Host { get; set; }
        public Tournament Tournament { get; set; }
        public List<User> Players { get; set; }
        public List<Room> Rooms { get; set; }
        public Dictionary<Team?, List<User>> Teams { get; set; }

        public TournamentModel(User host, Tournament tournament, List<User> players, List<Room> rooms, Dictionary<Team?, List<User>> teams)
        {
            Host = host;
            Tournament = tournament;
            Players = players;
            Rooms = rooms;
            Teams = teams;
        }

    }
}
