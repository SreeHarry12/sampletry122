namespace Rocket.Multiplayer
{
    public struct HostedRoomConfiguration
    {
        public string RoomName;
        public NetworkRoomMode Mode;
        public RoomVisibility Visibility;
        public int MaxPlayers;
        public ushort Port;
    }
}
