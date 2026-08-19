using System;
using Mirror;
using Rocket.Multiplayer.Player;
using Rocket.Multiplayer.Rooms.Discovery;

namespace Rocket.Multiplayer.Core
{
    /// <summary>
    /// Mirror plumbing hub: transport, room/gameplay scene transition, player spawn replacement.
    /// NOTE: "Room" here (NetworkRoomManager/NetworkRoomPlayer) is Mirror's own internal lobby-scene
    /// vs. gameplay-scene player-replacement concept - it is NOT the same as the user-facing "Room"
    /// (RoomInfo/RoomKey/Visibility), which is modeled separately under Rooms/IRoomService. This class
    /// only ever creates/joins Mirror's session; deciding *which* room/address to connect to is the
    /// job of IRoomService/IMatchmakingService, resolved before Host()/Join() is ever called.
    /// </summary>
    public class CustomNetworkManager : NetworkRoomManager
    {
        public static CustomNetworkManager Instance { get; private set; }

        public MultiplayerConfig Config;
        public RoomNetworkDiscovery Discovery;

        public string CurrentRoomId { get; private set; }
        public string CurrentRoomKey { get; private set; }
        public string CurrentRoomName { get; private set; }
        public RoomVisibility CurrentVisibility { get; private set; } = RoomVisibility.Public;

        public ushort ConfiguredPort => Config != null ? Config.NetworkPort : (ushort)7777;

        public RoomStatus CurrentStatus => roomSlots.Count >= maxConnections ? RoomStatus.Full : RoomStatus.Waiting;

        public event Action<NetworkConnectionToClient> ServerPlayerConnected;
        public event Action<NetworkConnectionToClient> ServerPlayerDisconnected;
        public event Action RoomPlayersChanged;
        public event Action<bool> InRoomSceneChanged;
        public event Action HostDisconnected;
        public event Action<JoinFailureReason, string> ClientJoinFailed;

        bool isLeavingIntentionally;

        public override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        public void BeginNewRoomSession(string roomName, string roomKey, RoomVisibility visibility)
        {
            CurrentRoomId = Guid.NewGuid().ToString("N");
            CurrentRoomKey = roomKey;
            CurrentRoomName = roomName;
            CurrentVisibility = visibility;
        }

        /// <summary>Host-only: called by the Lobby screen's "Enter Arena" button. Bypasses Mirror's
        /// built-in ready/minPlayers flow entirely - this project has no ready-up step yet.</summary>
        public void HostStartMatch()
        {
            if (NetworkServer.active)
                ServerChangeScene(GameplayScene);
        }

        public void LeaveSessionIntentionally()
        {
            isLeavingIntentionally = true;
            if (NetworkServer.active && NetworkClient.isConnected)
                StopHost();
            else if (NetworkClient.active || NetworkClient.isConnected)
                StopClient();
            else if (NetworkServer.active)
                StopServer();
        }

        public void RaiseClientJoinFailed(JoinFailureReason reason, string message)
        {
            ClientJoinFailed?.Invoke(reason, message);
        }

        public void RaiseRoomPlayersChanged()
        {
            RoomPlayersChanged?.Invoke();
        }

        // Ready/minPlayers flow is intentionally unused - HostStartMatch() drives the scene change.
        public override void OnRoomServerPlayersReady() { }

        public override void OnRoomStartHost()
        {
            if (Discovery == null)
                return;

            Discovery.ConfigureListenPort(Config != null ? Config.DiscoveryBroadcastPort : 47777);
            Discovery.AdvertiseServer();
        }

        public override void OnRoomStopHost()
        {
            Discovery?.StopDiscovery();
        }

        public override void OnRoomServerConnect(NetworkConnectionToClient conn)
        {
            ServerPlayerConnected?.Invoke(conn);
        }

        public override void OnRoomServerDisconnect(NetworkConnectionToClient conn)
        {
            ServerPlayerDisconnected?.Invoke(conn);
        }

        public override void OnRoomServerSceneChanged(string sceneName)
        {
            InRoomSceneChanged?.Invoke(Utils.IsSceneActive(RoomScene));
        }

        public override void OnRoomClientSceneChanged()
        {
            RaiseRoomPlayersChanged();
            InRoomSceneChanged?.Invoke(Utils.IsSceneActive(RoomScene));
        }

        public override void OnClientError(TransportError error, string reason)
        {
            RaiseClientJoinFailed(JoinFailureReason.ConnectFailed, reason);
        }

        public override void OnClientTransportException(Exception exception)
        {
            RaiseClientJoinFailed(JoinFailureReason.HostUnavailable, exception.Message);
        }

        public override void OnClientDisconnect()
        {
            bool wasIntentional = isLeavingIntentionally;
            isLeavingIntentionally = false;

            bool wasServer = NetworkServer.active;
            base.OnClientDisconnect();

            if (!wasIntentional && !wasServer)
                HostDisconnected?.Invoke();
        }

        // Mirror never returns a disconnected client to the room scene by itself - only
        // ServerChangeScene() moves between RoomScene/GameplayScene while connected.
        public override void OnStopClient()
        {
            base.OnStopClient();

            if (!Utils.IsSceneActive(RoomScene))
                UnityEngine.SceneManagement.SceneManager.LoadScene(RoomScene);
        }

        // Copies the lobby-chosen name onto the freshly spawned gameplay player.
        public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, UnityEngine.GameObject roomPlayer, UnityEngine.GameObject gamePlayer)
        {
            RoomPlayer lobbyPlayer = roomPlayer.GetComponent<RoomPlayer>();
            NetworkPlayerController controller = gamePlayer.GetComponent<NetworkPlayerController>();
            if (lobbyPlayer != null && controller != null)
                controller.ServerSetPlayerName(lobbyPlayer.PlayerName);

            return true;
        }

        // Suppress Mirror's legacy IMGUI room UI - this project uses its own UI layer.
        public override void OnGUI() { }
    }
}
