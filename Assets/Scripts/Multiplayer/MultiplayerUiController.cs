using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Rocket.Multiplayer
{
    public class MultiplayerUiController : MonoBehaviour
    {
        [SerializeField] private bool enableImmediateModeGui;

        private enum ScreenState
        {
            MainMenu,
            HostGame,
            PublicGames,
            JoinPrivate,
            Settings,
            Lobby
        }

        private RocketNetworkRoomManager roomManager;
        private ScreenState currentScreen = ScreenState.MainMenu;

        private string roomName = "My Arena";
        private NetworkRoomMode hostMode = NetworkRoomMode.Lan;
        private RoomVisibility hostVisibility = RoomVisibility.Public;
        private int maxPlayers = 4;
        private string portText = "7777";
        private string roomKeyText = string.Empty;
        private string directAddressText = "localhost";
        private string directPortText = "7777";
        private NetworkRoomMode joinMode = NetworkRoomMode.Lan;
        private string statusMessage = string.Empty;
        private string errorMessage = string.Empty;
        private Vector2 publicListScroll;
        private string playerNameText;
        private string advertisedAddressText;

        private void Awake()
        {
            roomManager = GetComponent<RocketNetworkRoomManager>();
            playerNameText = MultiplayerLocalSettings.PlayerName;
            advertisedAddressText = MultiplayerLocalSettings.AdvertisedAddress;
        }

        private void OnEnable()
        {
            roomManager.StatusChanged += HandleStatusChanged;
            roomManager.ErrorRaised += HandleErrorRaised;
            roomManager.LobbyStateChanged += HandleLobbyStateChanged;
            roomManager.RoomsChanged += RepaintRooms;
        }

        private void OnDisable()
        {
            if (roomManager == null)
            {
                return;
            }

            roomManager.StatusChanged -= HandleStatusChanged;
            roomManager.ErrorRaised -= HandleErrorRaised;
            roomManager.LobbyStateChanged -= HandleLobbyStateChanged;
            roomManager.RoomsChanged -= RepaintRooms;
        }

        private void OnGUI()
        {
            if (!enableImmediateModeGui)
            {
                return;
            }

            if (SceneManagerBridge.IsArenaScene())
            {
                DrawArenaOverlay();
                return;
            }

            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), string.Empty);

            Rect panelRect = new Rect(Screen.width * 0.5f - 260f, 30f, 520f, Screen.height - 60f);
            GUILayout.BeginArea(panelRect, GUI.skin.window);

            switch (currentScreen)
            {
                case ScreenState.MainMenu:
                    DrawMainMenu();
                    break;
                case ScreenState.HostGame:
                    DrawHostGame();
                    break;
                case ScreenState.PublicGames:
                    DrawPublicGames();
                    break;
                case ScreenState.JoinPrivate:
                    DrawJoinPrivate();
                    break;
                case ScreenState.Settings:
                    DrawSettings();
                    break;
                case ScreenState.Lobby:
                    DrawLobby();
                    break;
            }

            GUILayout.FlexibleSpace();
            GUILayout.Space(12f);
            GUILayout.Label($"Status: {statusMessage}");
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                GUILayout.Label($"Error: {errorMessage}");
            }

            GUILayout.EndArea();
        }

        private void DrawMainMenu()
        {
            DrawTitle("MULTIPLAYER");

            if (GUILayout.Button("HOST GAME", GUILayout.Height(48f)))
            {
                ClearMessages();
                currentScreen = ScreenState.HostGame;
            }

            if (GUILayout.Button("JOIN PUBLIC GAME", GUILayout.Height(48f)))
            {
                ClearMessages();
                roomManager.RefreshPublicRooms(NetworkRoomMode.Lan);
                currentScreen = ScreenState.PublicGames;
            }

            if (GUILayout.Button("JOIN PRIVATE GAME", GUILayout.Height(48f)))
            {
                ClearMessages();
                currentScreen = ScreenState.JoinPrivate;
            }

            if (GUILayout.Button("SETTINGS", GUILayout.Height(48f)))
            {
                ClearMessages();
                currentScreen = ScreenState.Settings;
            }

            if (GUILayout.Button("QUIT", GUILayout.Height(48f)))
            {
                Application.Quit();
            }
        }

        private void DrawHostGame()
        {
            DrawTitle("HOST GAME");
            DrawLabeledTextField("Room Name", ref roomName);

            hostMode = DrawEnumToolbar("Network Mode", hostMode);
            hostVisibility = DrawEnumToolbar("Visibility", hostVisibility);

            DrawLabeledTextField("Max Players", ref maxPlayers, 1, 4);
            DrawLabeledTextField("Port", ref portText);

            GUILayout.Space(8f);
            GUILayout.Label(roomManager.ConnectionRequirementNote, GUI.skin.box);

            if (GUILayout.Button("CREATE GAME", GUILayout.Height(42f)))
            {
                if (!ushort.TryParse(portText, out ushort port))
                {
                    errorMessage = "Port must be a valid number.";
                }
                else
                {
                    ClearMessages();
                    HostedRoomConfiguration config = new HostedRoomConfiguration
                    {
                        RoomName = roomName,
                        Mode = hostMode,
                        Visibility = hostVisibility,
                        MaxPlayers = Mathf.Clamp(maxPlayers, 1, 4),
                        Port = port
                    };

                    roomManager.HostRoom(config);
                    currentScreen = ScreenState.Lobby;
                }
            }

            if (GUILayout.Button("BACK", GUILayout.Height(36f)))
            {
                currentScreen = ScreenState.MainMenu;
            }
        }

        private void DrawPublicGames()
        {
            DrawTitle("PUBLIC GAMES");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("LAN"))
            {
                roomManager.RefreshPublicRooms(NetworkRoomMode.Lan);
            }

            if (GUILayout.Button("ONLINE"))
            {
                roomManager.RefreshPublicRooms(NetworkRoomMode.Online);
            }
            GUILayout.EndHorizontal();

            publicListScroll = GUILayout.BeginScrollView(publicListScroll, GUILayout.Height(360f));
            IReadOnlyDictionary<long, RoomInfo> rooms = roomManager.Discovery.DiscoveredRooms;
            if (rooms.Count == 0)
            {
                GUILayout.Label("No discoverable rooms found.");
            }
            else
            {
                foreach (RoomInfo room in rooms.Values)
                {
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label($"{room.RoomName}   {room.PlayerCount}/{room.MaxPlayers}   {room.Mode}");
                    GUILayout.Label($"{room.Address}:{room.Port}");
                    if (GUILayout.Button("JOIN"))
                    {
                        roomManager.JoinByAddress(room.Address, room.Port, room.Mode);
                        currentScreen = ScreenState.Lobby;
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("REFRESH", GUILayout.Height(40f)))
            {
                roomManager.RefreshPublicRooms(NetworkRoomMode.Lan);
            }

            if (GUILayout.Button("BACK", GUILayout.Height(36f)))
            {
                currentScreen = ScreenState.MainMenu;
            }
        }

        private void DrawJoinPrivate()
        {
            DrawTitle("JOIN GAME");
            DrawLabeledTextField("Room Key", ref roomKeyText);
            DrawLabeledTextField("Address", ref directAddressText);
            DrawLabeledTextField("Port", ref directPortText);

            joinMode = DrawEnumToolbar("Network Mode", joinMode);

            if (GUILayout.Button("JOIN WITH ROOM KEY", GUILayout.Height(42f)))
            {
                ClearMessages();
                roomManager.JoinByRoomKey(roomKeyText, joinMode);
                currentScreen = ScreenState.Lobby;
            }

            if (GUILayout.Button("JOIN WITH ADDRESS", GUILayout.Height(42f)))
            {
                if (!ushort.TryParse(directPortText, out ushort port))
                {
                    errorMessage = "Port must be a valid number.";
                }
                else
                {
                    ClearMessages();
                    roomManager.JoinByAddress(directAddressText, port, joinMode);
                    currentScreen = ScreenState.Lobby;
                }
            }

            if (GUILayout.Button("BACK", GUILayout.Height(36f)))
            {
                currentScreen = ScreenState.MainMenu;
            }
        }

        private void DrawSettings()
        {
            DrawTitle("SETTINGS");
            DrawLabeledTextField("Player Name", ref playerNameText);
            DrawLabeledTextField("Advertised Address", ref advertisedAddressText);

            MobileInputUI.DisplayMode mobileMode = MultiplayerLocalSettings.MobileDisplayMode;
            mobileMode = DrawEnumToolbar("Mobile Controls", mobileMode);

            if (GUILayout.Button("SAVE", GUILayout.Height(42f)))
            {
                MultiplayerLocalSettings.PlayerName = RocketNetworkRoomManager.SanitizePlayerName(playerNameText);
                MultiplayerLocalSettings.AdvertisedAddress = advertisedAddressText;
                MultiplayerLocalSettings.MobileDisplayMode = mobileMode;
                statusMessage = "Settings saved.";
            }

            if (GUILayout.Button("BACK", GUILayout.Height(36f)))
            {
                currentScreen = ScreenState.MainMenu;
            }
        }

        private void DrawLobby()
        {
            DrawTitle("LOBBY");

            RoomInfo roomInfo = roomManager.GetAdvertisedRoomInfo();
            GUILayout.Label($"Room: {roomInfo.RoomName}");
            GUILayout.Label($"Players: {roomInfo.PlayerCount}/{roomInfo.MaxPlayers}");
            GUILayout.Label($"Network: {roomInfo.Mode}");
            GUILayout.Label($"Ping: {(NetworkTime.rtt * 1000f):0} ms");

            if (!string.IsNullOrWhiteSpace(roomManager.CurrentRoomKey))
            {
                GUILayout.Space(8f);
                GUILayout.Label("Room Key");
                GUILayout.TextField(roomManager.CurrentRoomKey);
            }

            GUILayout.Space(8f);
            GUILayout.Label("Players");

            List<RocketRoomPlayer> players = roomManager.GetLobbyPlayers();
            foreach (RocketRoomPlayer player in players)
            {
                GUILayout.Label($"{player.PlayerNumber:00}  {player.PlayerName}");
            }

            if (NetworkServer.active && GUILayout.Button("ENTER ARENA", GUILayout.Height(42f)))
            {
                roomManager.EnterArena();
            }

            if (GUILayout.Button("LEAVE ROOM", GUILayout.Height(36f)))
            {
                roomManager.LeaveCurrentSession();
                currentScreen = ScreenState.MainMenu;
            }
        }

        private void DrawArenaOverlay()
        {
            GUILayout.BeginArea(new Rect(20f, 20f, 320f, 80f), GUI.skin.window);
            GUILayout.Label($"Network: {(NetworkServer.active ? (NetworkClient.isConnected ? "Host" : "Server") : "Client")}");
            GUILayout.Label($"Ping: {(NetworkTime.rtt * 1000f):0} ms");
            GUILayout.EndArea();
        }

        private void DrawTitle(string title)
        {
            GUILayout.Space(8f);
            GUILayout.Label(title, GUILayout.Height(36f));
            GUILayout.Space(12f);
        }

        private void DrawLabeledTextField(string label, ref string value)
        {
            GUILayout.Label(label);
            value = GUILayout.TextField(value ?? string.Empty, GUILayout.Height(28f));
        }

        private void DrawLabeledTextField(string label, ref int value, int min, int max)
        {
            string textValue = value.ToString();
            DrawLabeledTextField(label, ref textValue);
            if (int.TryParse(textValue, out int parsed))
            {
                value = Mathf.Clamp(parsed, min, max);
            }
        }

        private T DrawEnumToolbar<T>(string label, T current) where T : System.Enum
        {
            GUILayout.Label(label);
            GUILayout.BeginHorizontal();
            foreach (T option in System.Enum.GetValues(typeof(T)))
            {
                bool isSelected = EqualityComparer<T>.Default.Equals(option, current);
                if (GUILayout.Toggle(isSelected, option.ToString(), GUI.skin.button, GUILayout.Height(30f)))
                {
                    current = option;
                }
            }
            GUILayout.EndHorizontal();
            return current;
        }

        private void HandleStatusChanged(string status)
        {
            statusMessage = status;
        }

        private void HandleErrorRaised(string error)
        {
            errorMessage = error;
        }

        private void HandleLobbyStateChanged()
        {
            if (NetworkClient.isConnected || NetworkServer.active)
            {
                currentScreen = SceneManagerBridge.IsArenaScene() ? currentScreen : ScreenState.Lobby;
            }
        }

        private void RepaintRooms()
        {
        }

        private void ClearMessages()
        {
            statusMessage = string.Empty;
            errorMessage = string.Empty;
        }
    }
}
