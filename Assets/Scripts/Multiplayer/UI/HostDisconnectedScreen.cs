using System;
using UnityEngine;

namespace Rocket.Multiplayer.UI
{
    /// <summary>Shown when the host disconnects unexpectedly (not a voluntary leave). No host
    /// migration - clients are simply returned to the main menu.</summary>
    public class HostDisconnectedScreen
    {
        public GameObject Root { get; }

        public HostDisconnectedScreen(Transform parent, Action onReturnToMenu)
        {
            RectTransform panel = UiBuilder.CreatePanel(parent, "HostDisconnectedScreen");
            Root = panel.gameObject;

            UiBuilder.CreateLabel(panel, "HOST DISCONNECTED", 28, TextAnchor.MiddleCenter, 44);
            UiBuilder.CreateButton(panel, "RETURN TO MENU", onReturnToMenu);
        }

        public void Show() => Root.SetActive(true);
        public void Hide() => Root.SetActive(false);
    }
}
