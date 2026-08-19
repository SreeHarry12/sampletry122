using System;
using System.Text;
using Rocket.Multiplayer.Core;
using Rocket.Multiplayer.Lobby;
using UnityEngine;
using UnityEngine.UI;

namespace Rocket.Multiplayer.UI
{
    /// <summary>
    /// Covers both the "PRIVATE ROOM"/"PUBLIC ROOM" waiting screens and the "LOBBY" screen from the
    /// spec - they differ only in whether a room key is shown and whether Enter Arena is visible, so
    /// this single view renders all three rather than duplicating near-identical panels.
    /// </summary>
    public class LobbyScreen
    {
        public GameObject Root { get; }

        readonly CustomNetworkManager networkManager;
        readonly LobbyManager lobbyManager;

        readonly Text titleLabel;
        readonly GameObject keyRow;
        readonly Text keyLabel;
        readonly Text copyFeedbackLabel;
        readonly Text playersLabel;
        readonly Text countLabel;
        readonly Text statusLabel;
        readonly Button enterArenaButton;

        public LobbyScreen(Transform parent, CustomNetworkManager networkManager, LobbyManager lobbyManager, Action onEnterArena, Action onLeave)
        {
            this.networkManager = networkManager;
            this.lobbyManager = lobbyManager;

            RectTransform panel = UiBuilder.CreatePanel(parent, "LobbyScreen");
            Root = panel.gameObject;

            titleLabel = UiBuilder.CreateLabel(panel, "LOBBY", 28, TextAnchor.MiddleCenter, 44);

            keyRow = new GameObject("KeyRow", typeof(RectTransform));
            keyRow.transform.SetParent(panel, false);
            LayoutElement keyRowLayout = keyRow.AddComponent<LayoutElement>();
            keyRowLayout.minHeight = 36;
            keyLabel = UiBuilder.CreateLabel(keyRow.transform, string.Empty, 26);

            copyFeedbackLabel = UiBuilder.CreateLabel(panel, string.Empty, 16);
            copyFeedbackLabel.color = Color.green;
            copyFeedbackLabel.gameObject.SetActive(false);
            UiBuilder.CreateButton(panel, "COPY KEY", CopyKey, 40);

            playersLabel = UiBuilder.CreateLabel(panel, string.Empty, 20, TextAnchor.UpperLeft, 140);
            countLabel = UiBuilder.CreateLabel(panel, string.Empty, 20);
            statusLabel = UiBuilder.CreateLabel(panel, "Waiting for players...", 18);

            enterArenaButton = UiBuilder.CreateButton(panel, "ENTER ARENA", onEnterArena);
            UiBuilder.CreateButton(panel, "LEAVE ROOM", onLeave);
        }

        void CopyKey()
        {
            GUIUtility.systemCopyBuffer = networkManager.CurrentRoomKey ?? string.Empty;
            copyFeedbackLabel.text = "ROOM KEY COPIED!";
            copyFeedbackLabel.gameObject.SetActive(true);
        }

        public void Refresh()
        {
            titleLabel.text = string.IsNullOrEmpty(networkManager.CurrentRoomName) ? "LOBBY" : networkManager.CurrentRoomName.ToUpperInvariant();

            bool showKey = networkManager.CurrentVisibility == RoomVisibility.Private && !string.IsNullOrEmpty(networkManager.CurrentRoomKey);
            keyRow.SetActive(showKey);
            if (showKey)
                keyLabel.text = $"ROOM KEY: {networkManager.CurrentRoomKey}";

            var names = lobbyManager.GetPlayerNames();
            StringBuilder sb = new StringBuilder();
            foreach (string name in names)
                sb.AppendLine(name);
            playersLabel.text = sb.ToString();

            countLabel.text = $"{lobbyManager.CurrentPlayerCount} / {lobbyManager.MaxPlayers}";
            statusLabel.text = lobbyManager.CurrentPlayerCount >= lobbyManager.MaxPlayers ? "Room full." : "Waiting for players...";

            enterArenaButton.gameObject.SetActive(lobbyManager.IsHost);
        }

        public void Show()
        {
            Root.SetActive(true);
            copyFeedbackLabel.gameObject.SetActive(false);
            Refresh();
        }

        public void Hide() => Root.SetActive(false);
    }
}
