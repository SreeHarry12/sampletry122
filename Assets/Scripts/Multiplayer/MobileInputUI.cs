using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Rocket.Multiplayer
{
    public class MobileInputUI : MonoBehaviour
    {
        public enum DisplayMode
        {
            Auto,
            ForceMobile,
            ForceDesktop
        }

        [Header("Visibility")]
        [SerializeField] private DisplayMode displayMode = DisplayMode.Auto;
        [SerializeField] private GameObject controlsRoot;

        [Header("Scaling")]
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
        [SerializeField] private float matchWidthOrHeight = 1f;

        public bool IsShowingMobileControls => controlsRoot != null && controlsRoot.activeSelf;

        private void Awake()
        {
            ConfigureCanvasScaler();
            ApplyVisibility();
        }

        private void OnValidate()
        {
            ConfigureCanvasScaler();
        }

        public void BindInput(SharedPlayerInput playerInput)
        {
            if (controlsRoot == null || playerInput == null)
            {
                return;
            }

            foreach (VirtualJoystick joystick in controlsRoot.GetComponentsInChildren<VirtualJoystick>(true))
            {
                joystick.SetTargetInput(playerInput);
            }

            foreach (TouchLookArea lookArea in controlsRoot.GetComponentsInChildren<TouchLookArea>(true))
            {
                lookArea.SetTargetInput(playerInput);
            }

            foreach (JumpButtonInput jumpButton in controlsRoot.GetComponentsInChildren<JumpButtonInput>(true))
            {
                jumpButton.SetTargetInput(playerInput);
            }
        }

        public void ApplyVisibility()
        {
            if (controlsRoot == null)
            {
                return;
            }

            controlsRoot.SetActive(ShouldShowMobileControls());
        }

        private bool ShouldShowMobileControls()
        {
            switch (displayMode)
            {
                case DisplayMode.ForceMobile:
                    return true;
                case DisplayMode.ForceDesktop:
                    return false;
                default:
                    break;
            }

            if (Application.isMobilePlatform)
            {
                return true;
            }

            return Touchscreen.current != null;
        }

        private void ConfigureCanvasScaler()
        {
            if (canvasScaler == null)
            {
                return;
            }

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = referenceResolution;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = matchWidthOrHeight;
        }
    }
}
