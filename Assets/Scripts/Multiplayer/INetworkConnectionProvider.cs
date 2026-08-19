namespace Rocket.Multiplayer
{
    public interface INetworkConnectionProvider
    {
        string ProviderName { get; }
        string RequirementNote { get; }

        void Host(HostedRoomConfiguration configuration);
        void JoinByAddress(string address, ushort port, NetworkRoomMode mode);
        void Stop();
    }
}
