using TrackMyScore.Models;

public class CurrentRoomModel
{
    public Room Room { get; set; }
    public List<User> Players { get; set; }
    public User LoggedUser { get; set; }
    public List<JoinRoom>? JoinedPlayers { get; set; } 
    public Match? CurrentMatch { get; set; }
    public List<Participant>? Participants { get; set; }
    public List<Team>? Teams { get; set; }

}
