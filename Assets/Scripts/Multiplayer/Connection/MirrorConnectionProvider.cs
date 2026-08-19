using System;
using kcp2k;
using Mirror;
using Rocket.Multiplayer.Core;

namespace Rocket.Multiplayer.Connection
{
    public class MirrorConnectionProvider : INetworkConnectionProvider
    {
        readonly CustomNetworkManager networkManager;
        readonly KcpTransport transport;

        public string RequirementNote =>
            "Rooms are discovered and connected over the local network (LAN) only. Internet play " +
            "would require a reachable public address (port forwarding) or a future NAT-traversal " +
            "connection provider implementing INetworkConnectionProvider - Mirror itself does not " +
            "provide internet-wide matchmaking or NAT traversal.";

        public MirrorConnectionProvider(CustomNetworkManager networkManager, KcpTransport transport)
        {
            this.networkManager = networkManager;
            this.transport = transport;
        }

        public void Host()
        {
            transport.Port = networkManager.ConfiguredPort;
            networkManager.StartHost();
        }

        public void Join(string address, ushort port)
        {
            try
            {
                transport.Port = port;
                Uri uri = new Uri($"kcp://{address}:{port}");
                networkManager.StartClient(uri);
            }
            catch (Exception ex)
            {
                networkManager.RaiseClientJoinFailed(JoinFailureReason.ConnectFailed, ex.Message);
            }
        }

        public void Stop()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
                networkManager.StopHost();
            else if (NetworkClient.active || NetworkClient.isConnected)
                networkManager.StopClient();
            else if (NetworkServer.active)
                networkManager.StopServer();
        }

        public bool IsConnected() => NetworkClient.isConnected;
    }
}
