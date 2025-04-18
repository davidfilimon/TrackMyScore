﻿using TrackMyScore.Models;

public class CurrentRoomModel
{
    public Room Room { get; set; }
    public List<User> Players { get; set; }
    public User LoggedUser { get; set; }
    public bool UserJoined { get; set; }
    public Match? CurrentMatch { get; set; }
    public List<Participant>? Participants { get; set; }
}
