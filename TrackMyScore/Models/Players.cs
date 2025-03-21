﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class Players
    {
 
        public int Id { get; set; }
        [Required]
        [ForeignKey("UserId")]
        public User User { get; set; }
        [Required]
        [ForeignKey("TournamentId")]
        public Tournament Tournament { get; set; }

        [Required]
        public int RespectPoints { get; set; }
        public Players()
        {
            
        }

    }
}
