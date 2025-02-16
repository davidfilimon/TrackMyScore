namespace TrackMyScore.Models
{
    public class Game
    {

        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int MaxPlayers { get; set; }
        public string Difficulty { get; set; }

        public Game()
        {
      
        }

        public Game(string title, string description, int maxPlayers, string difficulty)
        {
            Title = title;
            Description = description;
            MaxPlayers = maxPlayers;
            Difficulty = difficulty;
        }

    }
}
