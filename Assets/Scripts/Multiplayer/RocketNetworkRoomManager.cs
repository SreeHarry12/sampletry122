using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rocket.Multiplayer
{
    public class RocketNetworkRoomManager : NetworkRoomManager
    {
        private const string ArenaSceneName = "Arena";
        private const string RoomPlayerResourcePath = "Multiplayer/RocketRoomPlayer";

        public static RocketNetworkRoomManager Instance { get; private set; }

        public event Action LobbyStateChanged;
        public event Action RoomsChanged;
        public event Action<string> StatusChanged;
        public event Action<string> ErrorRaised;

        public NetworkRoomMode CurrentMode { get; private set; } = NetworkRoomMode.Lan;
        public RoomVisibility CurrentVisibility { get; private set; } = RoomVisibility.Public;
        public string CurrentRoomName { get; private set; } = "My Arena";
        public string CurrentRoomKey { get; private set; } = string.Empty;
        public string ConnectionRequirementNote => connectionProvider?.RequirementNote ?? string.Empty;
        public RocketNetworkDiscovery Discovery { get; private set; }

        private INetworkConnectionProvider connectionProvider;
        private TelepathyTransport telepathyTransport;
        private MultiplayerUiController uiController;
        private string lastStatus = string.Empty;
        private string lastError = string.Empty;

        public override void Awake()
        {
            base.Awake();

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            telepathyTransport = GetComponent<TelepathyTransport>();
            if (telepathyTransport == null)
            {
                telepathyTransport = gameObject.AddComponent<TelepathyTransport>();
            }

            if (roomPlayerPrefab == null)
            {
                roomPlayerPrefab = Resources.Load<RocketRoomPlayer>(RoomPlayerResourcePath);
            }

            string activeSceneName = SceneManager.GetActiveScene().name;
            offlineScene = activeSceneName;
            onlineScene = activeSceneName;
            RoomScene = activeSceneName;
            GameplayScene = ArenaSceneName;
            dontDestroyOnLoad = true;
            showRoomGUI = false;
            minPlayers = 1;
            autoCreatePlayer = true;

            connectionProvider = new DirectMirrorConnectionProvider(this, telepathyTransport);
            Discovery = GetComponent<RocketNetworkDiscovery>();
            if (Discovery == null)
            {
                Discovery = gameObject.AddComponent<RocketNetworkDiscovery>();
            }
            Discovery.Initialize(this);
            Discovery.OnRoomsChanged += HandleRoomsChanged;

            uiController = GetComponent<MultiplayerUiController>();
            if (uiController == null)
            {
                uiController = gameObject.AddComponent<MultiplayerUiController>();
            }

            HideLegacyHud();
        }

        public void HostRoom(HostedRoomConfiguration configuration)
        {
            CurrentMode = configuration.Mode;
            CurrentVisibility = configuration.Visibility;
            CurrentRoomName = string.IsNullOrWhiteSpace(configuration.RoomName) ? "My Arena" : configuration.RoomName.Trim();
            connectionProvider.Host(configuration);
            SetStatus($"Hosting {CurrentMode} room on port {configuration.Port}");
        }

        public void JoinByRoomKey(string roomKey, NetworkRoomMode preferredMode)
        {
            if (!RoomKeyCodec.TryDecode(roomKey, out string address, out ushort port, out NetworkRoomMode decodedMode))
            {
                RaiseError("Invalid room key.");
                return;
            }

            CurrentMode = preferredMode == NetworkRoomMode.Online ? NetworkRoomMode.Online : decodedMode;
            JoinByAddress(address, port, CurrentMode);
        }

        public void JoinByAddress(string address, ushort port, NetworkRoomMode mode)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                RaiseError("Connection address is empty.");
                return;
            }

            connectionProvider.JoinByAddress(address.Trim(), port, mode);
            SetStatus($"Connecting to {address}:{port}...");
        }

        public void RefreshPublicRooms(NetworkRoomMode mode)
        {
            CurrentMode = mode;

            if (mode == NetworkRoomMode.Lan)
            {
                Discovery.RefreshRooms();
                SetStatus("Searching for LAN rooms...");
            }
            else
            {
                Discovery.DiscoveredRooms.Clear();
                RoomsChanged?.Invoke();
                SetStatus("No online public room directory is configured yet. Direct private join works when the host is reachable.");
            }
        }

        public void LeaveCurrentSession()
        {
            connectionProvider.Stop();
            Discovery.StopRoomDiscovery();
            CurrentRoomKey = string.Empty;
            SetStatus("Disconnected.");
        }

        public void EnterArena()
        {
            if (!NetworkServer.active)
            {
                RaiseError("Only the host can enter the arena.");
                return;
            }

            ServerChangeScene(GameplayScene);
        }

        public List<RocketRoomPlayer> GetLobbyPlayers()
        {
            List<RocketRoomPlayer> players = new List<RocketRoomPlayer>();
            foreach (NetworkRoomPlayer slot in roomSlots)
            {
                if (slot is RocketRoomPlayer roomPlayer)
                {
                    players.Add(roomPlayer);
                }
            }

            return players;
        }

        public RoomInfo GetAdvertisedRoomInfo()
        {
            RoomInfo info = new RoomInfo
            {
                RoomName = CurrentRoomName,
                Mode = CurrentMode,
                Visibility = CurrentVisibility,
                PlayerCount = numPlayers,
                MaxPlayers = maxConnections,
                CanJoin = true
            };

            string advertisedAddress = GetAdvertisedAddress();
            info.Address = advertisedAddress;
            info.Port = telepathyTransport != null ? telepathyTransport.port : (ushort)7777;

            if (RoomKeyCodec.TryEncode(advertisedAddress, info.Port, CurrentMode, out string roomKey))
            {
                info.RoomKey = roomKey;
            }

            if (CurrentMode == NetworkRoomMode.Online && string.IsNullOrWhiteSpace(MultiplayerLocalSettings.AdvertisedAddress))
            {
                info.StatusMessage = "Online direct hosting requires a public IP and port forwarding when using Telepathy.";
            }
            else if (CurrentMode == NetworkRoomMode.Lan)
            {
                info.StatusMessage = "LAN room";
            }

            return info;
        }

        public void ApplyHostedConfiguration(HostedRoomConfiguration configuration)
        {
            CurrentMode = configuration.Mode;
            CurrentVisibility = configuration.Visibility;
            CurrentRoomName = configuration.RoomName;

            string advertisedAddress = GetAdvertisedAddress();
            CurrentRoomKey = RoomKeyCodec.TryEncode(advertisedAddress, configuration.Port, configuration.Mode, out string roomKey)
                ? roomKey
                : string.Empty;
        }

        public void SetPendingJoinMode(NetworkRoomMode mode)
        {
            CurrentMode = mode;
        }

        public void NotifyLobbyStateChanged()
        {
            LobbyStateChanged?.Invoke();
        }

        public static string SanitizePlayerName(string requestedName)
        {
            if (string.IsNullOrWhiteSpace(requestedName))
            {
                return "Player";
            }

            string trimmed = requestedName.Trim();
            return trimmed.Length > 16 ? trimmed[..16] : trimmed;
        }

        public override void OnStartHost()
        {
            base.OnStartHost();
            SetStatus("Host started.");
            NotifyLobbyStateChanged();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            SetStatus("Client started.");
            NotifyLobbyStateChanged();
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            SetStatus("Connected.");
            NotifyLobbyStateChanged();
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            SetStatus("Disconnected.");
            NotifyLobbyStateChanged();
        }

        public override void OnStopHost()
        {
            base.OnStopHost();
            SetStatus("Host stopped.");
            NotifyLobbyStateChanged();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            NotifyLobbyStateChanged();
        }

        public override void OnRoomStartServer()
        {
            base.OnRoomStartServer();
            SetStatus("Lobby ready.");
        }

        public override GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayerObject)
        {
            GameObject player = Instantiate(playerPrefab);

            int playerIndex = 0;
            int currentIndex = 0;
            NetworkRoomPlayer targetSlot = roomPlayerObject.GetComponent<NetworkRoomPlayer>();
            foreach (NetworkRoomPlayer slot in roomSlots)
            {
                if (slot == targetSlot)
                {
                    playerIndex = currentIndex;
                    break;
                }

                currentIndex++;
            }

            Transform spawnPoint = ArenaSpawnManager.Instance != null
                ? ArenaSpawnManager.Instance.GetSpawnPoint(Mathf.Max(playerIndex, 0))
                : null;

            if (spawnPoint != null)
            {
                player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            }

            if (roomPlayerObject.TryGetComponent(out RocketRoomPlayer roomPlayer) &&
                player.TryGetComponent(out NetworkThirdPersonController controller))
            {
                controller.ServerSetPlayerName(roomPlayer.PlayerName, roomPlayer.PlayerNumber);
            }

            return player;
        }

        public override void OnRoomServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnRoomServerAddPlayer(conn);

            if (conn.identity != null && conn.identity.TryGetComponent(out RocketRoomPlayer roomPlayer))
            {
                roomPlayer.ServerAssignNumber(roomSlots.Count);
            }

            NotifyLobbyStateChanged();
        }

        public override void OnRoomServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnRoomServerDisconnect(conn);
            NotifyLobbyStateChanged();
        }

        public override void OnRoomClientSceneChanged()
        {
            base.OnRoomClientSceneChanged();
            NotifyLobbyStateChanged();
        }

        private void HandleRoomsChanged()
        {
            RoomsChanged?.Invoke();
        }

        private string GetAdvertisedAddress()
        {
            if (!string.IsNullOrWhiteSpace(MultiplayerLocalSettings.AdvertisedAddress))
            {
                return MultiplayerLocalSettings.AdvertisedAddress;
            }

            return LocalNetworkUtility.GetLanAddress();
        }

        private void SetStatus(string status)
        {
            lastStatus = status;
            StatusChanged?.Invoke(status);
        }

        private void RaiseError(string error)
        {
            lastError = error;
            ErrorRaised?.Invoke(error);
        }

        private void HideLegacyHud()
        {
            GameObject legacyHud = GameObject.Find("CanvasNetworkManagerHUD");
            if (legacyHud != null)
            {
                legacyHud.SetActive(false);
            }
        }
    }
}
