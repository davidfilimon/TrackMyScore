﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackMyScore.Models
{
    public class Team
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }
        public int Score { get; set; } = 0;
        public Team()
        {

        }

    }
}
