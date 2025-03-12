using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class JoinRoom
    {

        public int Id { get; set; }
        [Required]
        [ForeignKey("UserId")]
        public User Player { get; set; }
        [Required]
        [ForeignKey("RoomId")]
        public Room Room { get; set; }
        public JoinRoom()
        {
            
        }
    }
}
