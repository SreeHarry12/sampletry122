using Mirror;
using Rocket.Multiplayer.Core;

namespace Rocket.Multiplayer.Rooms.Discovery
{
    /// <summary>
    /// Mode/Key describe what the client is looking for, but the current LAN host always replies
    /// with its full live state regardless (RoomDiscoveryResponse is a struct and so can't be
    /// omitted to filter server-side) - LanDiscoveryRoomService filters responses client-side
    /// instead. Kept here so a future server-side room service can filter before replying.
    /// </summary>
    public struct RoomDiscoveryRequest : NetworkMessage
    {
        public DiscoveryQueryMode Mode;
        public string Key;
    }
}
