using TrackMyScore.Models;

public class GameDetailView
{
    public Game Game { get; set; }

    public List<UserStatViewModel> TopUsers { get; set; }
}

public class UserStatViewModel
{
    public string Username { get; set; }
    public int Played { get; set; }
    public int Won { get; set; }
}
