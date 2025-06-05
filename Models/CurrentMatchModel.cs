using TrackMyScore.Models;

public class CurrentMatchModel
{
    public Match Match { get; set; }
    public User LoggedUser { get; set; }
    public List<Player>? Players { get; set; }
    public List<TeamPlayer>? TeamPlayers { get; set; }
    public List<Team>? Teams { get; set; }
    public bool Participant { get; set; }

}
