using Mirror;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;

namespace Rocket.Multiplayer
{
    public class MainSceneMultiplayerUi : MonoBehaviour
    {
        private const ushort DefaultPort = 7777;

        private RocketNetworkRoomManager roomManager;
        private InputField playerNameInput;
        private InputField roomNameInput;
        private InputField addressInput;
        private InputField portInput;
        private Button hostInternetButton;
        private Button joinInternetButton;
        private Button leaveButton;
        private Button enterArenaButton;
        private Text statusText;
        private Text roomKeyText;

        private void Awake()
        {
            roomManager = FindFirstObjectByType<RocketNetworkRoomManager>(FindObjectsInactive.Include);

            playerNameInput = FindInput("PlayerNameInput");
            roomNameInput = FindInput("RoomNameInput");
            addressInput = FindInput("AddressInput");
            portInput = FindInput("PortInput");

            hostInternetButton = FindButton("HostInternetButton");
            joinInternetButton = FindButton("JoinInternetButton");
            leaveButton = FindButton("LeaveButton");
            enterArenaButton = FindButton("EnterArenaButton");

            statusText = FindText("StatusText");
            roomKeyText = FindText("RoomKeyText");
        }

        private void OnEnable()
        {
            if (roomManager == null)
            {
                return;
            }

            roomManager.StatusChanged += HandleStatusChanged;
            roomManager.ErrorRaised += HandleErrorRaised;
            roomManager.LobbyStateChanged += RefreshView;
        }

        private void Start()
        {
            if (playerNameInput != null)
            {
                playerNameInput.text = MultiplayerLocalSettings.PlayerName;
            }

            if (roomNameInput != null && string.IsNullOrWhiteSpace(roomNameInput.text))
            {
                roomNameInput.text = "My Arena";
            }

            if (addressInput != null)
            {
                addressInput.text = string.IsNullOrWhiteSpace(MultiplayerLocalSettings.AdvertisedAddress)
                    ? string.Empty
                    : MultiplayerLocalSettings.AdvertisedAddress;
            }

            if (portInput != null && string.IsNullOrWhiteSpace(portInput.text))
            {
                portInput.text = DefaultPort.ToString();
            }

            RegisterButton(hostInternetButton, HostInternetGame);
            RegisterButton(joinInternetButton, JoinInternetGame);
            RegisterButton(leaveButton, LeaveGame);
            RegisterButton(enterArenaButton, EnterArena);

            RefreshView();
            SetStatus("Ready.");
        }

        private void OnDisable()
        {
            if (roomManager == null)
            {
                return;
            }

            roomManager.StatusChanged -= HandleStatusChanged;
            roomManager.ErrorRaised -= HandleErrorRaised;
            roomManager.LobbyStateChanged -= RefreshView;
        }

        private void HostInternetGame()
        {
            if (roomManager == null)
            {
                SetStatus("Room manager not found.");
                return;
            }

            if (addressInput == null || string.IsNullOrWhiteSpace(addressInput.text))
            {
                SetStatus("Enter the public IPv4 address clients should use.");
                return;
            }

            string advertisedAddress = addressInput.text.Trim();
            if (!IPAddress.TryParse(advertisedAddress, out IPAddress parsedAddress) ||
                parsedAddress.AddressFamily != AddressFamily.InterNetwork ||
                IPAddress.IsLoopback(parsedAddress))
            {
                SetStatus("Enter a public IPv4 address. Internet play does not use localhost or LAN IPs here.");
                return;
            }

            if (!TryBuildConfiguration(NetworkRoomMode.Online, RoomVisibility.Private, out HostedRoomConfiguration configuration))
            {
                return;
            }

            MultiplayerLocalSettings.AdvertisedAddress = advertisedAddress;
            roomManager.HostRoom(configuration);
            SetStatus($"Hosting internet game. Clients must join {MultiplayerLocalSettings.AdvertisedAddress}:{configuration.Port}");
            RefreshView();
        }

        private void JoinInternetGame()
        {
            if (roomManager == null)
            {
                SetStatus("Room manager not found.");
                return;
            }

            SavePlayerName();

            if (addressInput == null || string.IsNullOrWhiteSpace(addressInput.text))
            {
                SetStatus("Enter the host address.");
                return;
            }

            string joinAddress = addressInput.text.Trim();
            if (IPAddress.TryParse(joinAddress, out IPAddress parsedAddress) && IPAddress.IsLoopback(parsedAddress))
            {
                SetStatus("127.0.0.1 only works when host and client run on the same PC.");
                return;
            }

            if (!TryGetPort(out ushort port))
            {
                SetStatus("Port must be a valid number.");
                return;
            }

            roomManager.JoinByAddress(joinAddress, port, NetworkRoomMode.Online);
            SetStatus($"Trying to join {joinAddress}:{port}");
            RefreshView();
        }

        private void LeaveGame()
        {
            if (roomManager == null)
            {
                SetStatus("Room manager not found.");
                return;
            }

            roomManager.LeaveCurrentSession();
            RefreshView();
        }

        private void EnterArena()
        {
            if (roomManager == null)
            {
                SetStatus("Room manager not found.");
                return;
            }

            roomManager.EnterArena();
            RefreshView();
        }

        private bool TryBuildConfiguration(NetworkRoomMode mode, RoomVisibility visibility, out HostedRoomConfiguration configuration)
        {
            configuration = default;

            SavePlayerName();

            if (!TryGetPort(out ushort port))
            {
                SetStatus("Port must be a valid number.");
                return false;
            }

            configuration = new HostedRoomConfiguration
            {
                RoomName = roomNameInput != null && !string.IsNullOrWhiteSpace(roomNameInput.text)
                    ? roomNameInput.text.Trim()
                    : "My Arena",
                Mode = mode,
                Visibility = visibility,
                MaxPlayers = 4,
                Port = port
            };

            return true;
        }

        private bool TryGetPort(out ushort port)
        {
            port = DefaultPort;
            return portInput != null && ushort.TryParse(portInput.text, out port);
        }

        private void SavePlayerName()
        {
            if (playerNameInput != null)
            {
                MultiplayerLocalSettings.PlayerName = RocketNetworkRoomManager.SanitizePlayerName(playerNameInput.text);
                playerNameInput.text = MultiplayerLocalSettings.PlayerName;
            }
        }

        private void RefreshView()
        {
            if (roomManager == null)
            {
                SetButtonActive(hostInternetButton, false);
                SetButtonActive(joinInternetButton, false);
                SetButtonActive(leaveButton, false);
                SetButtonActive(enterArenaButton, false);
                return;
            }

            bool isConnected = NetworkServer.active || NetworkClient.active;
            bool isHost = NetworkServer.active;
            bool canEnterArena = NetworkServer.active && NetworkClient.isConnected;

            SetButtonActive(hostInternetButton, !isConnected);
            SetButtonActive(joinInternetButton, !isConnected);
            SetButtonActive(leaveButton, isConnected);
            SetButtonActive(enterArenaButton, canEnterArena);

            if (roomKeyText == null)
            {
                return;
            }

            if (isHost && !string.IsNullOrWhiteSpace(roomManager.CurrentRoomKey))
            {
                string address = !string.IsNullOrWhiteSpace(MultiplayerLocalSettings.AdvertisedAddress)
                    ? MultiplayerLocalSettings.AdvertisedAddress
                    : LocalNetworkUtility.GetLanAddress();
                roomKeyText.text = $"Join Address: {address}:{portInput?.text}\nRoom Key: {roomManager.CurrentRoomKey}";
            }
            else
            {
                roomKeyText.text = isConnected ? "Connected." : "Room Key: -";
            }
        }

        private void HandleStatusChanged(string status)
        {
            SetStatus(status);
            RefreshView();
        }

        private void HandleErrorRaised(string error)
        {
            SetStatus(error);
            RefreshView();
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = $"Status: {message}";
            }
        }

        private void RegisterButton(Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(callback);
            button.onClick.AddListener(callback);
        }

        private void SetButtonActive(Button button, bool isActive)
        {
            if (button != null)
            {
                button.gameObject.SetActive(isActive);
            }
        }

        private InputField FindInput(string objectName) => FindChildComponent<InputField>(objectName);

        private Button FindButton(string objectName) => FindChildComponent<Button>(objectName);

        private Text FindText(string objectName) => FindChildComponent<Text>(objectName);

        private T FindChildComponent<T>(string objectName) where T : Component
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == objectName && child.TryGetComponent(out T component))
                {
                    return component;
                }
            }

            return null;
        }
    }
}
