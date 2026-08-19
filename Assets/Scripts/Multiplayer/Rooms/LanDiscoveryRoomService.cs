using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rocket.Multiplayer.Connection;
using Rocket.Multiplayer.Core;
using Rocket.Multiplayer.Rooms.Discovery;
using Rocket.Multiplayer.Utilities;
using UnityEngine;

namespace Rocket.Multiplayer.Rooms
{
    /// <summary>
    /// IRoomService backed by real LAN UDP discovery (RoomNetworkDiscovery). Room keys are opaque
    /// random strings resolved to connection info via a live key-lookup broadcast, not decoded from
    /// the key itself - a future backend-based IRoomService would resolve the same key over HTTP
    /// instead, without any caller (UI/Lobby/Matchmaking) needing to change.
    /// </summary>
    public class LanDiscoveryRoomService : IRoomService
    {
        readonly CustomNetworkManager networkManager;
        readonly RoomNetworkDiscovery discovery;
        readonly INetworkConnectionProvider connectionProvider;
        readonly MultiplayerConfig config;
        readonly MonoBehaviour coroutineHost;

        readonly List<RoomInfo> collectedRooms = new List<RoomInfo>();
        Coroutine searchRoutine;
        bool searchingForKey;
        string searchingKey;

        public event Action<RoomInfo> RoomFound;
        public event Action SearchCompleted;
        public event Action SearchTimedOut;
        public event Action<RoomInfo> RoomJoined;
        public event Action<string> RoomError;

        public LanDiscoveryRoomService(CustomNetworkManager networkManager, RoomNetworkDiscovery discovery,
            INetworkConnectionProvider connectionProvider, MultiplayerConfig config, MonoBehaviour coroutineHost)
        {
            this.networkManager = networkManager;
            this.discovery = discovery;
            this.connectionProvider = connectionProvider;
            this.config = config;
            this.coroutineHost = coroutineHost;
            discovery.ResponseReceived += OnResponseReceived;
        }

        public RoomInfo CreateRoom(RoomCreateRequest request)
        {
            string roomKey = RoomKeyGenerator.GenerateUnique(config, collectedRooms.Select(r => r.RoomKey));
            int maxPlayers = request.MaxPlayers > 0 ? request.MaxPlayers : config.MaxPlayers;
            string roomName = string.IsNullOrWhiteSpace(request.RoomName) ? config.DefaultRoomName : request.RoomName;

            networkManager.BeginNewRoomSession(roomName, roomKey, request.Visibility);
            networkManager.maxConnections = maxPlayers;
            connectionProvider.Host();

            RoomInfo info = new RoomInfo
            {
                RoomId = networkManager.CurrentRoomId,
                RoomKey = roomKey,
                RoomName = roomName,
                Visibility = request.Visibility,
                CurrentPlayers = 1,
                MaxPlayers = maxPlayers,
                Status = RoomStatus.Waiting
            };
            RoomJoined?.Invoke(info);
            return info;
        }

        public void FindPublicRooms(float timeoutSeconds) => StartSearch(DiscoveryQueryMode.PublicSearch, null, timeoutSeconds);

        public void FindRoomByKey(string key, float timeoutSeconds) => StartSearch(DiscoveryQueryMode.KeyLookup, key, timeoutSeconds);

        void StartSearch(DiscoveryQueryMode mode, string key, float timeoutSeconds)
        {
            CancelSearch();
            collectedRooms.Clear();
            searchingForKey = mode == DiscoveryQueryMode.KeyLookup;
            searchingKey = key;

            discovery.PendingQueryMode = mode;
            discovery.PendingQueryKey = key ?? string.Empty;
            discovery.ConfigureListenPort(config.DiscoveryBroadcastPort);
            discovery.StartDiscovery();

            searchRoutine = coroutineHost.StartCoroutine(SearchTimeoutRoutine(timeoutSeconds));
        }

        IEnumerator SearchTimeoutRoutine(float timeoutSeconds)
        {
            yield return new WaitForSeconds(timeoutSeconds);
            discovery.StopDiscovery();
            searchRoutine = null;

            if (collectedRooms.Count == 0)
                SearchTimedOut?.Invoke();
            else
                SearchCompleted?.Invoke();
        }

        public void CancelSearch()
        {
            if (searchRoutine != null)
            {
                coroutineHost.StopCoroutine(searchRoutine);
                searchRoutine = null;
            }
            discovery.StopDiscovery();
        }

        void OnResponseReceived(RoomDiscoveryResponse response)
        {
            if (response.HostUri == null || string.IsNullOrEmpty(response.RoomKey))
                return;

            // The host always answers with its full live state (a NetworkMessage struct can't be
            // omitted to filter server-side) - visibility/key matching is enforced here instead.
            if (searchingForKey)
            {
                if (!string.Equals(response.RoomKey, searchingKey, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            else if (response.Visibility == RoomVisibility.Private)
            {
                return;
            }

            RoomInfo info = new RoomInfo
            {
                RoomId = response.RoomId,
                RoomKey = response.RoomKey,
                RoomName = response.RoomName,
                Visibility = response.Visibility,
                CurrentPlayers = response.CurrentPlayers,
                MaxPlayers = response.MaxPlayers,
                Status = response.Status,
                HostAddress = response.HostUri.Host,
                HostPort = response.HostUri.IsDefaultPort ? config.NetworkPort : response.HostUri.Port
            };

            collectedRooms.RemoveAll(r => r.RoomId == info.RoomId);
            collectedRooms.Add(info);
            RoomFound?.Invoke(info);

            if (searchingForKey)
            {
                CancelSearch();
                SearchCompleted?.Invoke();
            }
        }

        public void JoinRoom(RoomInfo room)
        {
            if (room == null)
            {
                RoomError?.Invoke("Room not found.");
                return;
            }

            if (room.Status == RoomStatus.Full)
            {
                RoomError?.Invoke("Room is full.");
                return;
            }

            if (!room.CanJoin)
            {
                RoomError?.Invoke("Room is no longer available.");
                return;
            }

            connectionProvider.Join(room.HostAddress, (ushort)room.HostPort);
            RoomJoined?.Invoke(room);
        }

        public void LeaveRoom()
        {
            discovery.StopDiscovery();
            networkManager.LeaveSessionIntentionally();
            connectionProvider.Stop();
        }

        // Live-read model: room state always comes fresh from the host's discovery response,
        // so there is no stored registry to update or remove entries from.
        public void UpdateRoom(RoomInfo room) { }
        public void RemoveRoom(string roomId) { }
    }
}
