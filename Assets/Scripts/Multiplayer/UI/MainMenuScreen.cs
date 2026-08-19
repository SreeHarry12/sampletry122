using System;
using UnityEngine;
using UnityEngine.UI;

namespace Rocket.Multiplayer.UI
{
    /// <summary>Root "MULTIPLAYER" menu. Pure view - all button actions are handled by whoever
    /// constructs this (MultiplayerUiController), never by talking to Mirror/services directly.</summary>
    public class MainMenuScreen
    {
        public GameObject Root { get; }
        InputField nameField;

        public string EnteredPlayerName => nameField != null ? nameField.text : string.Empty;

        public MainMenuScreen(Transform parent, Action onCreatePrivateRoom, Action onFindOnlineGame, Action onJoinWithKey, Action onBack)
        {
            RectTransform panel = UiBuilder.CreatePanel(parent, "MainMenuScreen");
            Root = panel.gameObject;

            UiBuilder.CreateLabel(panel, "MULTIPLAYER", 32, TextAnchor.MiddleCenter, 48);
            nameField = UiBuilder.CreateInputField(panel, "Your name (optional)");
            UiBuilder.CreateButton(panel, "CREATE PRIVATE ROOM", onCreatePrivateRoom);
            UiBuilder.CreateButton(panel, "FIND ONLINE GAME", onFindOnlineGame);
            UiBuilder.CreateButton(panel, "JOIN WITH KEY", onJoinWithKey);
            UiBuilder.CreateButton(panel, "BACK", onBack);
        }

        public void Show() => Root.SetActive(true);
        public void Hide() => Root.SetActive(false);
    }
}
