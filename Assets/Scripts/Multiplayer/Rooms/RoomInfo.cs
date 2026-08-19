using System;
using Rocket.Multiplayer.Core;

namespace Rocket.Multiplayer.Rooms
{
    /// <summary>
    /// User-facing room record (not to be confused with Mirror's own NetworkRoomPlayer/NetworkRoomManager
    /// "room" terminology, which is internal lobby-scene plumbing). Populated from a live discovery
    /// response, never persisted - a room's true state is always read fresh from its host.
    /// </summary>
    [Serializable]
    public class RoomInfo
    {
        public string RoomId;
        public string RoomKey;
        public string RoomName;
        public RoomVisibility Visibility;
        public int CurrentPlayers;
        public int MaxPlayers;
        public RoomStatus Status;
        public string HostAddress;
        public int HostPort;

        public bool CanJoin => Status == RoomStatus.Waiting && CurrentPlayers < MaxPlayers;
    }
}
