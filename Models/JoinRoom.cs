using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class JoinRoom
    {

        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int RoomId { get; set; }
        public Room Room { get; set; }
        
        public JoinRoom()
        {
            
        }
    }
}
