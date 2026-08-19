using System;

namespace Rocket.Multiplayer.Rooms
{
    /// <summary>
    /// Room discovery/CRUD abstraction, deliberately separate from Mirror's own networking
    /// (see INetworkConnectionProvider) and from the app's Mirror-facing CustomNetworkManager.
    /// The concrete implementation today (LanDiscoveryRoomService) resolves rooms via real LAN
    /// UDP broadcast; a future internet backend can implement this same interface (e.g. over
    /// HTTP/WebSocket) without any caller needing to change.
    /// </summary>
    public interface IRoomService
    {
        event Action<RoomInfo> RoomFound;
        event Action SearchCompleted;
        event Action SearchTimedOut;
        event Action<RoomInfo> RoomJoined;
        event Action<string> RoomError;

        RoomInfo CreateRoom(RoomCreateRequest request);
        void FindPublicRooms(float timeoutSeconds);
        void FindRoomByKey(string key, float timeoutSeconds);
        void CancelSearch();
        void JoinRoom(RoomInfo room);
        void LeaveRoom();

        /// <summary>
        /// No-op in the LAN implementation: room state is always read live from the host's
        /// discovery responses rather than a stored registry. Kept for a future backend-backed
        /// implementation that needs explicit CRUD.
        /// </summary>
        void UpdateRoom(RoomInfo room);

        /// <summary>Same rationale as UpdateRoom - a dead/full host simply stops answering discovery.</summary>
        void RemoveRoom(string roomId);
    }
}
