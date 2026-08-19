using System;

namespace Rocket.Multiplayer
{
    [Serializable]
    public class RoomInfo
    {
        public string RoomName;
        public NetworkRoomMode Mode;
        public RoomVisibility Visibility;
        public int PlayerCount;
        public int MaxPlayers;
        public string Address;
        public ushort Port;
        public string RoomKey;
        public string StatusMessage;
        public bool CanJoin;
    }
}
