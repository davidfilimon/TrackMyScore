using TrackMyScore.Models;

public class RoomListModel{

    public List<JoinRoom> joinedRooms;
    public List<Room> myRooms;
    public List<Room> roomList;
    public Dictionary<Room, List<User>> joinedPlayers;
    public int playerCount;

    public RoomListModel(List<JoinRoom> joinedRooms, List<Room> roomList, Dictionary<Room, List<User>> joinedPlayers, int playerCount){
        this.joinedRooms = joinedRooms;
        this.roomList = roomList;
        this.joinedPlayers = joinedPlayers;
        this.playerCount = playerCount;
    }


}