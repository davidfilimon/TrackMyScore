namespace TrackMyScore.Models
{
    public class TournamentModel
    {
        
        public User LoggedUser { get; set; }
        public Tournament Tournament { get; set; }
        public List<User> Players { get; set; }
        public List<Room> Rooms { get; set; }
        public Dictionary<Team, List<User>> Teams { get; set; }
        public List<User> MutualFollowers { get; set; }

        public TournamentModel(User loggedUser, Tournament tournament, List<User> players, List<Room> rooms, Dictionary<Team, List<User>> teams, List<User> mutualFollowers)
        {
            LoggedUser = loggedUser;
            Tournament = tournament;
            Players = players;
            Rooms = rooms;
            Teams = teams;
            MutualFollowers = mutualFollowers;
        }
        

    }
}
