using TrackMyScore.Models;

public class MatchListModel
{

    public List<Match> JoinedMatches;
    public List<Match> AvailableMatches;
    public List<Match> HostedMatches;
    public Dictionary<int, int?> PlayerCount;

   public MatchListModel()
    {

    }
}
