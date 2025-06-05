using TrackMyScore.Models;

public class TeamPlayer
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; }

    public bool Eliminated { get; set; } = false;
    public int Reward { get; set; } = 1;

    public int TeamId { get; set; }
    public Team Team { get; set; }

    public TeamPlayer()
    {
        
    }
}