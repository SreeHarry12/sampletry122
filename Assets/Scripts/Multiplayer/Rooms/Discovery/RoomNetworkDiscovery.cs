using System;
using System.Net;
using Mirror;
using Mirror.Discovery;
using Rocket.Multiplayer.Core;
using UnityEngine;

namespace Rocket.Multiplayer.Rooms.Discovery
{
    /// <summary>
    /// Real LAN UDP discovery for rooms - carries room name/key/visibility/player-count, unlike
    /// Mirror's stock NetworkDiscovery. Room state is always read live off CustomNetworkManager on
    /// each incoming request, so a room that fills up or whose host quits simply stops matching or
    /// stops responding on the very next request - no stale-room bookkeeping needed.
    /// </summary>
    public class RoomNetworkDiscovery : NetworkDiscoveryBase<RoomDiscoveryRequest, RoomDiscoveryResponse>
    {
        // Fixed non-zero handshake: this project never runs through the Editor's OnValidate
        // auto-generation step, so it must be set explicitly before Start() reads it.
        const long Handshake = 826735109273L;

        public DiscoveryQueryMode PendingQueryMode = DiscoveryQueryMode.PublicSearch;
        public string PendingQueryKey = string.Empty;

        public event Action<RoomDiscoveryResponse> ResponseReceived;

        void Awake()
        {
            secretHandshake = Handshake;
        }

        public void ConfigureListenPort(int port)
        {
            serverBroadcastListenPort = port;
        }

        protected override RoomDiscoveryRequest GetRequest()
        {
            return new RoomDiscoveryRequest
            {
                Mode = PendingQueryMode,
                Key = PendingQueryKey
            };
        }

        // RoomDiscoveryResponse is a struct (as all Mirror NetworkMessages must be), so this cannot
        // return null to suppress a reply - Visibility/key/fullness filtering happens client-side in
        // LanDiscoveryRoomService instead. This is only ever invoked while actively hosting (it's only
        // reachable via the listen loop AdvertiseServer() starts), so CustomNetworkManager.Instance and
        // transport are always valid here; if that assumption is ever wrong, the caller's own
        // try/catch around this discovery loop safely drops the request instead of crashing it.
        protected override RoomDiscoveryResponse ProcessRequest(RoomDiscoveryRequest request, IPEndPoint endpoint)
        {
            Debug.Log($"[RoomDiscovery][Host] Request received from {endpoint} (mode={request.Mode}, key={request.Key})");

            try
            {
                CustomNetworkManager manager = CustomNetworkManager.Instance;

                RoomDiscoveryResponse response = new RoomDiscoveryResponse
                {
                    RoomId = manager.CurrentRoomId,
                    RoomKey = manager.CurrentRoomKey,
                    RoomName = manager.CurrentRoomName,
                    Visibility = manager.CurrentVisibility,
                    CurrentPlayers = manager.roomSlots.Count,
                    MaxPlayers = manager.maxConnections,
                    Status = manager.CurrentStatus,
                    HostUri = transport.ServerUri()
                };

                Debug.Log($"[RoomDiscovery][Host] Replying with room '{response.RoomName}' key={response.RoomKey} uri={response.HostUri}");
                return response;
            }
            catch (Exception ex)
            {
                // NetworkDiscoveryBase silently swallows exceptions thrown here (no reply is sent),
                // which is otherwise indistinguishable from the packet never arriving at all.
                Debug.LogError($"[RoomDiscovery][Host] ProcessRequest threw, no reply will be sent: {ex}");
                throw;
            }
        }

        protected override void ProcessResponse(RoomDiscoveryResponse response, IPEndPoint endpoint)
        {
            Debug.Log($"[RoomDiscovery][Client] Response received from {endpoint}: room='{response.RoomName}' key={response.RoomKey} uri={response.HostUri}");

            response.EndPoint = endpoint;

            // The self-reported host URI may carry an unresolvable hostname; we know the real
            // sender address because we just received a packet from it, so use that instead.
            if (response.HostUri != null)
            {
                UriBuilder realUri = new UriBuilder(response.HostUri)
                {
                    Host = endpoint.Address.ToString()
                };
                response.HostUri = realUri.Uri;
            }

            ResponseReceived?.Invoke(response);
        }
    }
}
