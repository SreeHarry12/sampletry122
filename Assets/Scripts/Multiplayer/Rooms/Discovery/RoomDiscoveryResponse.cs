using System;
using System.Net;
using Mirror;
using Rocket.Multiplayer.Core;

namespace Rocket.Multiplayer.Rooms.Discovery
{
    public struct RoomDiscoveryResponse : NetworkMessage
    {
        public string RoomId;
        public string RoomKey;
        public string RoomName;
        public RoomVisibility Visibility;
        public int CurrentPlayers;
        public int MaxPlayers;
        public RoomStatus Status;
        public Uri HostUri;

        /// <summary>Filled in client-side from the actual UDP sender, not serialized over the wire.</summary>
        public IPEndPoint EndPoint { get; set; }
    }
}
