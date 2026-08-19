using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rocket.Multiplayer.UI
{
    /// <summary>
    /// Small procedural-UI helpers. The whole multiplayer UI is built at runtime in code rather than
    /// as hand-authored Canvas/prefab YAML, since there is no way to verify uGUI serialization without
    /// the Unity Editor - the same approach already proven for this project's earlier (since removed)
    /// multiplayer scene bootstrap.
    /// </summary>
    public static class UiBuilder
    {
        public static readonly Color PanelBackground = new Color(0.08f, 0.09f, 0.11f, 0.92f);
        public static readonly Color ButtonColor = new Color(0.20f, 0.45f, 0.85f, 1f);
        public static readonly Color ButtonDisabledColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        public static readonly Color ErrorColor = new Color(0.85f, 0.30f, 0.25f, 1f);

        public static Canvas CreateCanvas(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);

            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();

            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        public static RectTransform CreatePanel(Transform parent, string name, bool withLayout = true)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(560, 640);
            rect.anchoredPosition = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.color = PanelBackground;

            if (withLayout)
            {
                VerticalLayoutGroup layout = go.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(32, 32, 32, 32);
                layout.spacing = 16;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }

            return rect;
        }

        public static Text CreateLabel(Transform parent, string text, int fontSize = 24, TextAnchor alignment = TextAnchor.MiddleCenter, float height = 36)
        {
            GameObject go = new GameObject("Label_" + text, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            Text label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;

            LayoutElement layoutElement = go.GetComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;

            return label;
        }

        public static Button CreateButton(Transform parent, string label, Action onClick, float height = 56)
        {
            GameObject go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.color = ButtonColor;

            LayoutElement layoutElement = go.GetComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;

            Button button = go.GetComponent<Button>();
            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            Text text = CreateLabel(go.transform, label, 22);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.GetComponent<LayoutElement>().ignoreLayout = true;

            return button;
        }

        public static InputField CreateInputField(Transform parent, string placeholderText, float height = 56)
        {
            GameObject go = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.color = Color.white;

            LayoutElement layoutElement = go.GetComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;

            InputField field = go.GetComponent<InputField>();

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            Text text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.color = Color.black;
            text.supportRichText = false;
            SetStretch(textGo.GetComponent<RectTransform>(), 8);

            GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(go.transform, false);
            Text placeholder = placeholderGo.GetComponent<Text>();
            placeholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholder.fontSize = 22;
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.color = new Color(0, 0, 0, 0.5f);
            placeholder.text = placeholderText;
            SetStretch(placeholderGo.GetComponent<RectTransform>(), 8);

            field.textComponent = text;
            field.placeholder = placeholder;

            return field;
        }

        static void SetStretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }
    }
}
