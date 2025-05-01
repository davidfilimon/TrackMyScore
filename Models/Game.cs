using System.ComponentModel.DataAnnotations;

namespace TrackMyScore.Models
{
    public class Game
    {

        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public int MaxPlayers { get; set; }
        [Required]
        public string Difficulty { get; set; }
        [Required]
        public string Author { get; set; }
        public bool IsOfficial {  get; set; }

        public Game()
        {
            IsOfficial = false;
        }

        public Game(string title, string description, int maxPlayers, string difficulty, string author, bool isOfficial)
        {
            Title = title;
            Description = description;
            MaxPlayers = maxPlayers;
            Difficulty = difficulty;
            Author = author;
            IsOfficial = isOfficial;
        }

    }
}
