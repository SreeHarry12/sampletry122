using Rocket.Multiplayer.Core;
using Rocket.Multiplayer.Lobby;
using Rocket.Multiplayer.Matchmaking;
using Rocket.Multiplayer.Rooms;
using UnityEngine;

namespace Rocket.Multiplayer.UI
{
    /// <summary>
    /// Single screen-switcher for the whole multiplayer flow. Screens are pure views; this is the
    /// only place that listens to IRoomService/IMatchmakingService/CustomNetworkManager events and
    /// decides what to show - screens never call into Mirror or the services directly.
    /// </summary>
    public class MultiplayerUiController : MonoBehaviour
    {
        CustomNetworkManager networkManager;
        IRoomService roomService;
        IMatchmakingService matchmakingService;
        LobbyManager lobbyManager;
        MultiplayerConfig config;

        MainMenuScreen mainMenuScreen;
        JoinWithKeyScreen joinWithKeyScreen;
        SearchingScreen searchingScreen;
        LobbyScreen lobbyScreen;
        HostDisconnectedScreen hostDisconnectedScreen;

        RoomInfo pendingKeyRoom;
        Canvas canvas;

        public void Initialize(CustomNetworkManager networkManager, IRoomService roomService,
            IMatchmakingService matchmakingService, LobbyManager lobbyManager, MultiplayerConfig config)
        {
            this.networkManager = networkManager;
            this.roomService = roomService;
            this.matchmakingService = matchmakingService;
            this.lobbyManager = lobbyManager;
            this.config = config;

            canvas = UiBuilder.CreateCanvas(transform, "MultiplayerCanvas");

            mainMenuScreen = new MainMenuScreen(canvas.transform, OnCreatePrivateRoomClicked, OnFindOnlineGameClicked, OnJoinWithKeyClicked, OnBackClicked);
            joinWithKeyScreen = new JoinWithKeyScreen(canvas.transform, OnJoinClicked, ShowMainMenu);
            searchingScreen = new SearchingScreen(canvas.transform, OnCancelSearchClicked);
            lobbyScreen = new LobbyScreen(canvas.transform, networkManager, lobbyManager, OnEnterArenaClicked, OnLeaveClicked);
            hostDisconnectedScreen = new HostDisconnectedScreen(canvas.transform, OnReturnToMenuFromDisconnect);

            roomService.RoomJoined += OnRoomJoined;
            roomService.RoomError += OnRoomError;
            matchmakingService.MatchFound += _ => { };
            matchmakingService.MatchCreated += _ => searchingScreen.SetStatus("No existing game found. Creating a new game...");
            matchmakingService.SearchFailed += OnSearchFailed;
            lobbyManager.LobbyChanged += OnLobbyChanged;
            networkManager.HostDisconnected += OnHostDisconnected;
            networkManager.ClientJoinFailed += OnClientJoinFailed;
            networkManager.InRoomSceneChanged += OnInRoomSceneChanged;

            ShowMainMenu();
        }

        void ApplyPendingPlayerName() => RoomPlayer.PendingLocalPlayerName = mainMenuScreen.EnteredPlayerName;

        void OnCreatePrivateRoomClicked()
        {
            ApplyPendingPlayerName();
            roomService.CreateRoom(new RoomCreateRequest(config.DefaultRoomName, RoomVisibility.Private, config.MaxPlayers));
        }

        void OnFindOnlineGameClicked()
        {
            ApplyPendingPlayerName();
            ShowScreen(searchingScreen.Root);
            searchingScreen.Show();
            matchmakingService.FindMatch();
        }

        void OnJoinWithKeyClicked()
        {
            ShowScreen(joinWithKeyScreen.Root);
            joinWithKeyScreen.Show();
        }

        void OnBackClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnCancelSearchClicked()
        {
            matchmakingService.CancelSearch();
            ShowMainMenu();
        }

        void OnJoinClicked()
        {
            string key = joinWithKeyScreen.EnteredKey;
            if (string.IsNullOrEmpty(key))
            {
                joinWithKeyScreen.ShowError("Enter a room key first.");
                return;
            }

            ApplyPendingPlayerName();
            joinWithKeyScreen.SetBusy(true);
            joinWithKeyScreen.ShowError(string.Empty);

            pendingKeyRoom = null;
            roomService.RoomFound += OnKeyRoomFound;
            roomService.SearchCompleted += OnKeySearchCompleted;
            roomService.SearchTimedOut += OnKeySearchTimedOut;

            roomService.FindRoomByKey(key, config.ConnectionTimeoutSeconds);
        }

        void OnKeyRoomFound(RoomInfo room) => pendingKeyRoom = room;

        void OnKeySearchCompleted()
        {
            UnsubscribeKeySearch();
            joinWithKeyScreen.SetBusy(false);

            if (pendingKeyRoom == null)
            {
                joinWithKeyScreen.ShowError("Room not found.");
                return;
            }

            roomService.JoinRoom(pendingKeyRoom);
        }

        void OnKeySearchTimedOut()
        {
            UnsubscribeKeySearch();
            joinWithKeyScreen.SetBusy(false);
            joinWithKeyScreen.ShowError("Room not found.");
        }

        void UnsubscribeKeySearch()
        {
            roomService.RoomFound -= OnKeyRoomFound;
            roomService.SearchCompleted -= OnKeySearchCompleted;
            roomService.SearchTimedOut -= OnKeySearchTimedOut;
        }

        void OnEnterArenaClicked() => networkManager.HostStartMatch();

        void OnLeaveClicked()
        {
            roomService.LeaveRoom();
            ShowMainMenu();
        }

        void OnReturnToMenuFromDisconnect()
        {
            roomService.LeaveRoom();
            ShowMainMenu();
        }

        void OnRoomJoined(RoomInfo room)
        {
            ShowScreen(lobbyScreen.Root);
            lobbyScreen.Show();
        }

        void OnRoomError(string message)
        {
            if (joinWithKeyScreen.Root.activeSelf)
                joinWithKeyScreen.ShowError(message);
            else
                ShowMainMenu();
        }

        void OnSearchFailed(string message)
        {
            matchmakingService.CancelSearch();
            ShowMainMenu();
        }

        void OnLobbyChanged()
        {
            if (lobbyScreen.Root.activeSelf)
                lobbyScreen.Refresh();
        }

        void OnHostDisconnected()
        {
            canvas.gameObject.SetActive(true);
            ShowScreen(hostDisconnectedScreen.Root);
            hostDisconnectedScreen.Show();
        }

        // The menu/lobby canvas persists (DontDestroyOnLoad) so it can still show
        // HostDisconnectedScreen if the host drops while everyone is in the Arena scene, but it has
        // no reason to render on top of the arena otherwise.
        void OnInRoomSceneChanged(bool inRoomScene) => canvas.gameObject.SetActive(inRoomScene);

        void OnClientJoinFailed(JoinFailureReason reason, string message)
        {
            if (Mirror.NetworkServer.active)
                return; // hosts don't experience client-join failures against themselves

            ShowScreen(joinWithKeyScreen.Root);
            joinWithKeyScreen.Show();
            joinWithKeyScreen.ShowError(string.IsNullOrEmpty(message) ? reason.ToString() : message);
        }

        void ShowMainMenu() => ShowScreen(mainMenuScreen.Root);

        void ShowScreen(GameObject target)
        {
            mainMenuScreen.Hide();
            joinWithKeyScreen.Hide();
            searchingScreen.Hide();
            lobbyScreen.Hide();
            hostDisconnectedScreen.Hide();

            target.SetActive(true);
        }
    }
}
