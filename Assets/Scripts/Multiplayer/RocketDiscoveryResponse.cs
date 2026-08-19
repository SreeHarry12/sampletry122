using System;
using System.Net;
using Mirror;

namespace Rocket.Multiplayer
{
    public struct RocketDiscoveryResponse : NetworkMessage
    {
        public IPEndPoint EndPoint { get; set; }
        public Uri uri;
        public long serverId;
        public string roomName;
        public int playerCount;
        public int maxPlayers;
        public byte mode;
        public byte visibility;
    }
}
