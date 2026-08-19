using System;
using System.Collections.Generic;
using System.Net;
using Mirror;
using Mirror.Discovery;

namespace Rocket.Multiplayer
{
    public class RocketNetworkDiscovery : NetworkDiscoveryBase<ServerRequest, RocketDiscoveryResponse>
    {
        public readonly Dictionary<long, RoomInfo> DiscoveredRooms = new Dictionary<long, RoomInfo>();

        public event Action OnRoomsChanged;

        private RocketNetworkRoomManager roomManager;

        public void Initialize(RocketNetworkRoomManager manager)
        {
            roomManager = manager;
        }

        public void RefreshRooms()
        {
            DiscoveredRooms.Clear();
            OnRoomsChanged?.Invoke();
            StartDiscovery();
        }

        public void StopRoomDiscovery()
        {
            StopDiscovery();
        }

        protected override ServerRequest GetRequest() => new ServerRequest();

        protected override RocketDiscoveryResponse ProcessRequest(ServerRequest request, IPEndPoint endpoint)
        {
            if (roomManager == null || !NetworkServer.active)
            {
                return default;
            }

            RoomInfo roomInfo = roomManager.GetAdvertisedRoomInfo();

            return new RocketDiscoveryResponse
            {
                serverId = ServerId,
                uri = transport.ServerUri(),
                roomName = roomInfo.RoomName,
                playerCount = roomInfo.PlayerCount,
                maxPlayers = roomInfo.MaxPlayers,
                mode = (byte)roomInfo.Mode,
                visibility = (byte)roomInfo.Visibility
            };
        }

        protected override void ProcessResponse(RocketDiscoveryResponse response, IPEndPoint endpoint)
        {
            response.EndPoint = endpoint;

            UriBuilder builder = new UriBuilder(response.uri)
            {
                Host = endpoint.Address.ToString()
            };

            RoomInfo roomInfo = new RoomInfo
            {
                RoomName = response.roomName,
                PlayerCount = response.playerCount,
                MaxPlayers = response.maxPlayers,
                Mode = (NetworkRoomMode)response.mode,
                Visibility = (RoomVisibility)response.visibility,
                Address = builder.Host,
                Port = (ushort)builder.Port,
                CanJoin = true,
                StatusMessage = "LAN discovery"
            };

            if (RoomKeyCodec.TryEncode(roomInfo.Address, roomInfo.Port, roomInfo.Mode, out string roomKey))
            {
                roomInfo.RoomKey = roomKey;
            }

            DiscoveredRooms[response.serverId] = roomInfo;
            OnRoomsChanged?.Invoke();
        }
    }
}
