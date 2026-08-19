using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
            displayMode = MultiplayerLocalSettings.MobileDisplayMode;
            ConfigureCanvasScaler();
            BuildDefaultControlsIfNeeded();
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

        public void ConfigureRuntimeCanvas(CanvasScaler scaler)
        {
            canvasScaler = scaler;
            ConfigureCanvasScaler();
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

        private void BuildDefaultControlsIfNeeded()
        {
            if (controlsRoot != null)
            {
                return;
            }

            controlsRoot = new GameObject("ControlsRoot");
            RectTransform rootTransform = controlsRoot.AddComponent<RectTransform>();
            rootTransform.SetParent(transform, false);
            rootTransform.anchorMin = Vector2.zero;
            rootTransform.anchorMax = Vector2.one;
            rootTransform.offsetMin = Vector2.zero;
            rootTransform.offsetMax = Vector2.zero;

            CreateTouchLookArea(rootTransform);
            CreateJumpButton(rootTransform);
            CreateJoystick(rootTransform);
        }

        private void CreateJoystick(RectTransform parent)
        {
            GameObject joystickRoot = CreatePanel("VirtualJoystick", parent, new Vector2(220f, 220f), new Vector2(24f, 24f), TextAnchor.LowerLeft, new Color(0f, 0f, 0f, 0.2f));
            GameObject handleObject = CreatePanel("Handle", joystickRoot.GetComponent<RectTransform>(), new Vector2(96f, 96f), Vector2.zero, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.5f));
            handleObject.GetComponent<Image>().raycastTarget = false;

            VirtualJoystick joystick = joystickRoot.AddComponent<VirtualJoystick>();
            joystickRoot.AddComponent<CanvasGroup>().blocksRaycasts = true;

            var handleField = typeof(VirtualJoystick).GetField("handle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var radiusField = typeof(VirtualJoystick).GetField("radius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            handleField?.SetValue(joystick, handleObject.GetComponent<RectTransform>());
            radiusField?.SetValue(joystick, 70f);
        }

        private void CreateJumpButton(RectTransform parent)
        {
            GameObject jumpButtonObject = CreatePanel("JumpButton", parent, new Vector2(140f, 140f), new Vector2(-24f, 24f), TextAnchor.LowerRight, new Color(0.1f, 0.55f, 0.9f, 0.6f));
            jumpButtonObject.AddComponent<JumpButtonInput>();
            GameObject labelObject = CreateLabel("JUMP", jumpButtonObject.GetComponent<RectTransform>());
            labelObject.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        }

        private void CreateTouchLookArea(RectTransform parent)
        {
            GameObject touchAreaObject = CreatePanel("TouchLookArea", parent, new Vector2(0f, 0f), Vector2.zero, TextAnchor.MiddleRight, new Color(0f, 0f, 0f, 0.01f));
            RectTransform rectTransform = touchAreaObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            touchAreaObject.AddComponent<TouchLookArea>();
        }

        private static GameObject CreatePanel(string name, RectTransform parent, Vector2 size, Vector2 margin, TextAnchor anchor, Color color)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);

            switch (anchor)
            {
                case TextAnchor.LowerLeft:
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.zero;
                    rectTransform.pivot = Vector2.zero;
                    rectTransform.anchoredPosition = margin;
                    break;
                case TextAnchor.LowerRight:
                    rectTransform.anchorMin = new Vector2(1f, 0f);
                    rectTransform.anchorMax = new Vector2(1f, 0f);
                    rectTransform.pivot = new Vector2(1f, 0f);
                    rectTransform.anchoredPosition = new Vector2(margin.x, margin.y);
                    break;
                default:
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.anchoredPosition = margin;
                    break;
            }

            rectTransform.sizeDelta = size;

            Image image = panelObject.GetComponent<Image>();
            image.color = color;

            return panelObject;
        }

        private static GameObject CreateLabel(string textValue, RectTransform parent)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Text text = labelObject.GetComponent<Text>();
            text.text = textValue;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            return labelObject;
        }
    }
}
