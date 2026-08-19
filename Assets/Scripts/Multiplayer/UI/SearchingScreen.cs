using System;
using UnityEngine;
using UnityEngine.UI;

namespace Rocket.Multiplayer.UI
{
    /// <summary>"FINDING GAME..." screen shown while IMatchmakingService looks for a public room.</summary>
    public class SearchingScreen
    {
        public GameObject Root { get; }
        readonly Text statusLabel;

        public SearchingScreen(Transform parent, Action onCancel)
        {
            RectTransform panel = UiBuilder.CreatePanel(parent, "SearchingScreen");
            Root = panel.gameObject;

            UiBuilder.CreateLabel(panel, "FINDING GAME...", 28, TextAnchor.MiddleCenter, 44);
            statusLabel = UiBuilder.CreateLabel(panel, "Searching for available public rooms...", 18);
            UiBuilder.CreateButton(panel, "CANCEL", onCancel);
        }

        public void SetStatus(string message) => statusLabel.text = message;

        public void Show()
        {
            Root.SetActive(true);
            SetStatus("Searching for available public rooms...");
        }

        public void Hide() => Root.SetActive(false);
    }
}
