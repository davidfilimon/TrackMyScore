using TrackMyScore.Models;

public class RoomListModel
{
    public List<JoinRoom> JoinedRooms { get; }

    public List<Room> RoomList { get; }

    public Dictionary<int, List<User>> JoinedPlayersInRoomList { get; }

    public Dictionary<int, List<User>> JoinedPlayers { get; }

    public RoomListModel(
        List<JoinRoom> joinedRooms,
        List<Room> roomList,
        Dictionary<int, List<User>> joinedPlayers,
        Dictionary<int, List<User>> joinedPlayersInRoomList)
    {
        JoinedRooms = joinedRooms;
        RoomList = roomList;
        JoinedPlayers = joinedPlayers;
        JoinedPlayersInRoomList = joinedPlayersInRoomList;
    }
}
