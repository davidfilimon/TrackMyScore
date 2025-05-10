namespace TrackMyScore.Models
{
    public class TournamentModel
    {
        
        public User LoggedUser { get; set; }
        public Tournament Tournament { get; set; }
        public List<Player> Players { get; set; }
        public List<Room> Rooms { get; set; }
        public List<Team>? Teams { get; set; }
        public List<User> MutualFollowers { get; set; }

        public TournamentModel(User loggedUser, Tournament tournament, List<Player> players, List<Room> rooms, List<Team>? teams, List<User> mutualFollowers)
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
