using Mirror;

namespace Rocket.Multiplayer
{
    public class DirectMirrorConnectionProvider : INetworkConnectionProvider
    {
        private readonly RocketNetworkRoomManager roomManager;
        private readonly TelepathyTransport transport;

        public DirectMirrorConnectionProvider(RocketNetworkRoomManager roomManager, TelepathyTransport transport)
        {
            this.roomManager = roomManager;
            this.transport = transport;
        }

        public string ProviderName => "Mirror Direct Host/Client";

        public string RequirementNote =>
            "LAN works directly. Internet hosting requires a reachable public address and port forwarding when using Telepathy.";

        public void Host(HostedRoomConfiguration configuration)
        {
            transport.port = configuration.Port;
            roomManager.maxConnections = configuration.MaxPlayers;
            roomManager.ApplyHostedConfiguration(configuration);
            roomManager.StartHost();
        }

        public void JoinByAddress(string address, ushort port, NetworkRoomMode mode)
        {
            transport.port = port;
            roomManager.networkAddress = address;
            roomManager.SetPendingJoinMode(mode);
            roomManager.StartClient();
        }

        public void Stop()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                roomManager.StopHost();
            }
            else if (NetworkClient.isConnected || NetworkClient.active)
            {
                roomManager.StopClient();
            }
            else if (NetworkServer.active)
            {
                roomManager.StopServer();
            }
        }
    }
}
