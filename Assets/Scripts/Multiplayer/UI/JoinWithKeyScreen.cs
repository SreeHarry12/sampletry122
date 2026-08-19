using System;
using UnityEngine;
using UnityEngine.UI;

namespace Rocket.Multiplayer.UI
{
    /// <summary>"JOIN PRIVATE ROOM" screen. Surfaces every distinct join-failure reason as text
    /// and always stays on this screen when a join fails - never a stuck loading state.</summary>
    public class JoinWithKeyScreen
    {
        public GameObject Root { get; }
        readonly InputField keyField;
        readonly Text errorLabel;
        readonly Button joinButton;

        public string EnteredKey => keyField != null ? keyField.text.Trim().ToUpperInvariant() : string.Empty;

        public JoinWithKeyScreen(Transform parent, Action onJoin, Action onBack)
        {
            RectTransform panel = UiBuilder.CreatePanel(parent, "JoinWithKeyScreen");
            Root = panel.gameObject;

            UiBuilder.CreateLabel(panel, "JOIN PRIVATE ROOM", 28, TextAnchor.MiddleCenter, 44);
            UiBuilder.CreateLabel(panel, "Enter Room Key", 18);
            keyField = UiBuilder.CreateInputField(panel, "A7K2P");
            keyField.characterLimit = 8;

            errorLabel = UiBuilder.CreateLabel(panel, string.Empty, 18);
            errorLabel.color = UiBuilder.ErrorColor;
            errorLabel.gameObject.SetActive(false);

            joinButton = UiBuilder.CreateButton(panel, "JOIN", onJoin);
            UiBuilder.CreateButton(panel, "BACK", onBack);
        }

        public void ShowError(string message)
        {
            errorLabel.text = message;
            errorLabel.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        public void SetBusy(bool busy)
        {
            joinButton.interactable = !busy;
        }

        public void Show()
        {
            Root.SetActive(true);
            ShowError(string.Empty);
            SetBusy(false);
        }

        public void Hide() => Root.SetActive(false);
    }
}
