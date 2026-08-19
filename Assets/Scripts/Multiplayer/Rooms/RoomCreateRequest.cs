using Rocket.Multiplayer.Core;

namespace Rocket.Multiplayer.Rooms
{
    public class RoomCreateRequest
    {
        public string RoomName;
        public RoomVisibility Visibility;
        public int MaxPlayers;

        public RoomCreateRequest(string roomName, RoomVisibility visibility, int maxPlayers)
        {
            RoomName = roomName;
            Visibility = visibility;
            MaxPlayers = maxPlayers;
        }
    }
}
