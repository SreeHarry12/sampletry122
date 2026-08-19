using kcp2k;
using Mirror;
using Rocket.Multiplayer.Connection;
using Rocket.Multiplayer.Lobby;
using Rocket.Multiplayer.Matchmaking;
using Rocket.Multiplayer.Rooms;
using Rocket.Multiplayer.Rooms.Discovery;
using Rocket.Multiplayer.UI;
using UnityEngine;

namespace Rocket.Multiplayer.Core
{
    /// <summary>
    /// Single entry point for the whole multiplayer feature. Lives once in Main.unity, wires the
    /// Mirror networking objects (kept alive across the Main/Arena scene change) and the menu/lobby UI
    /// (also kept alive so it can still show a "host disconnected" message from within the Arena scene),
    /// then hands control to MultiplayerUiController.
    /// </summary>
    public class MultiplayerBootstrap : MonoBehaviour
    {
        static MultiplayerBootstrap instance;

        [Header("Player Prefab Sources (existing project assets)")]
        [Tooltip("Root GameObject of the app-level lobby player prefab (RoomPlayer component).")]
        [SerializeField] GameObject roomPlayerPrefabSource;
        [Tooltip("Character1_Prefab from Cute_Characters_Pack_1 - the networked gameplay player.")]
        [SerializeField] GameObject characterPlayerPrefabSource;

        void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            MultiplayerConfig config = Resources.Load<MultiplayerConfig>("Multiplayer/MultiplayerConfig");
            if (config == null)
            {
                Debug.LogError("MultiplayerBootstrap: no MultiplayerConfig found at Resources/Multiplayer/MultiplayerConfig.");
                return;
            }

            if (roomPlayerPrefabSource == null || characterPlayerPrefabSource == null)
            {
                Debug.LogError("MultiplayerBootstrap: room player / character player prefab sources are not assigned.");
                return;
            }

            BuildNetworking(config, out CustomNetworkManager networkManager, out KcpTransport transport, out RoomNetworkDiscovery discovery);

            INetworkConnectionProvider connectionProvider = new MirrorConnectionProvider(networkManager, transport);
            IRoomService roomService = new LanDiscoveryRoomService(networkManager, discovery, connectionProvider, config, this);
            IMatchmakingService matchmakingService = new LocalMatchmakingService(roomService, config);
            LobbyManager lobbyManager = new LobbyManager(networkManager);

            MultiplayerUiController ui = gameObject.AddComponent<MultiplayerUiController>();
            ui.Initialize(networkManager, roomService, matchmakingService, lobbyManager, config);
        }

        void BuildNetworking(MultiplayerConfig config, out CustomNetworkManager networkManager, out KcpTransport transport, out RoomNetworkDiscovery discovery)
        {
            // Configure every field before the GameObject is activated, so NetworkManager.Awake()
            // (which reads transport/dontDestroyOnLoad immediately) sees the final values rather than
            // component defaults.
            GameObject networkGo = new GameObject("NetworkSystem");
            networkGo.SetActive(false);

            transport = networkGo.AddComponent<KcpTransport>();
            transport.port = config.NetworkPort;

            discovery = networkGo.AddComponent<RoomNetworkDiscovery>();
            discovery.transport = transport;
            discovery.ConfigureListenPort(config.DiscoveryBroadcastPort);

            networkManager = networkGo.AddComponent<CustomNetworkManager>();
            networkManager.transport = transport;
            networkManager.Config = config;
            networkManager.Discovery = discovery;
            networkManager.RoomScene = "Assets/Scenes/Main.unity";
            networkManager.GameplayScene = "Assets/Scenes/Arena.unity";
            networkManager.showRoomGUI = false;
            networkManager.dontDestroyOnLoad = true;
            networkManager.maxConnections = config.MaxPlayers;
            networkManager.autoCreatePlayer = true;
            networkManager.playerPrefab = characterPlayerPrefabSource;

            RoomPlayer roomPlayerComponent = roomPlayerPrefabSource.GetComponent<RoomPlayer>();
            if (roomPlayerComponent == null)
                Debug.LogError("MultiplayerBootstrap: roomPlayerPrefabSource has no RoomPlayer component.");
            else
                networkManager.roomPlayerPrefab = roomPlayerComponent;

            networkGo.SetActive(true);
        }
    }
}
