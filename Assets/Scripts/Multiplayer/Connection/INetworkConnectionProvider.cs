namespace Rocket.Multiplayer.Connection
{
    /// <summary>
    /// The only layer that touches Mirror's transport/connection APIs directly. Room discovery
    /// (IRoomService/IMatchmakingService) hands this a resolved address/port; this never decides
    /// which room to join. Swapping LAN play for a real internet/NAT-traversal transport later
    /// only means writing a new implementation of this interface.
    /// </summary>
    public interface INetworkConnectionProvider
    {
        /// <summary>Honest, user-facing description of this provider's reach (e.g. LAN-only today).</summary>
        string RequirementNote { get; }

        void Host();
        void Join(string address, ushort port);
        void Stop();
        bool IsConnected();
    }
}
