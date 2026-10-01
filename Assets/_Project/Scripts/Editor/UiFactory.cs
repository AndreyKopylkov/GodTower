using GodTower.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GodTower.Editor
{
    /// <summary>Small helpers that build uGUI hierarchies from code (canvases, text, sliced images, kit buttons).</summary>
    public static class UiFactory
    {
        public static readonly Vector2 ReferenceResolution = new(1080f, 1920f);
        public static readonly Color Yellow = new(1f, 0.86f, 0.12f);
        public static readonly Color Navy = new(0.08f, 0.12f, 0.3f);

        public static Canvas CreateCanvas(string name, int sortingOrder, bool interactive)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            if (interactive)
                canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>Event system driven by the Input System (touch + mouse), needed for buttons.</summary>
        public static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            // No actions asset: the module assigns its default UI actions in OnEnable at runtime.
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>Stretched child that follows the device safe area.</summary>
        public static RectTransform CreateSafeArea(Transform parent)
        {
            RectTransform rect = CreateRect("SafeArea", parent);
            Stretch(rect);
            rect.gameObject.AddComponent<SafeAreaFitter>();
            return rect;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Anchors <paramref name="rect"/> at <paramref name="anchor"/> (pivot too) with a fixed size and offset.</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Sprite sprite, bool sliced = false, bool raycast = false)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced;
            image.raycastTarget = raycast;
            return image;
        }

        public static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = UiImportSetup.Font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        /// <summary>
        /// Kit button: a sliced pill (green = primary, blue = secondary) with an optional icon on the left and a label.
        /// Pressed state swaps to the pressed sprite (green) or tints (blue); the button squashes while held.
        /// </summary>
        public static Button CreateButton(string name, Transform parent, string text, bool primary, string icon,
            Vector2 size, float fontSize)
        {
            Image background = CreateImage(name, parent, UiImportSetup.Sprite(primary ? "Kit/button_normal.png" : "Kit/button_blue.png"),
                sliced: true, raycast: true);
            background.rectTransform.sizeDelta = size;
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ConfigureTransition(button, primary ? UiImportSetup.Sprite("Kit/button_pressed.png") : null,
                UiImportSetup.Sprite("Kit/button_disabled.png"));
            background.gameObject.AddComponent<ButtonPressScale>();

            float iconSize = size.y * 0.55f;
            float textLeft = 0f;
            if (!string.IsNullOrEmpty(icon))
            {
                Image iconImage = CreateImage("Icon", background.transform, UiImportSetup.Sprite($"Icons/{icon}.png"));
                Place(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(size.y * 0.32f, 4f), Vector2.one * iconSize);
                textLeft = size.y * 0.32f + iconSize * 0.6f;
            }

            TextMeshProUGUI label = CreateText("Label", background.transform, text, fontSize, Color.white);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(textLeft, 10f);
            label.rectTransform.offsetMax = new Vector2(0f, 0f);
            return button;
        }

        /// <summary>Round blue icon button (pause).</summary>
        public static Button CreateRoundButton(string name, Transform parent, string icon, float size)
        {
            Image background = CreateImage(name, parent, UiImportSetup.Sprite("Kit/round_button_normal.png"), raycast: true);
            background.rectTransform.sizeDelta = Vector2.one * size;
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ConfigureTransition(button, UiImportSetup.Sprite("Kit/round_button_pressed.png"), null);
            background.gameObject.AddComponent<ButtonPressScale>();

            Image iconImage = CreateImage("Icon", background.transform, UiImportSetup.Sprite($"Icons/{icon}.png"));
            Place(iconImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), Vector2.one * size * 0.5f);
            return button;
        }

        /// <summary>
        /// Full-screen popup root: a dim blocker (eats input meant for the game) and a centred kit panel window.
        /// Saved inactive; <see cref="PopupView"/> shows it.
        /// </summary>
        public static (RectTransform Root, RectTransform Window) CreatePopup(string name, Transform parent, Vector2 windowSize)
        {
            RectTransform root = CreateRect(name, parent);
            Stretch(root);
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.04f, 0.12f, 0.6f);
            dim.raycastTarget = true;
            root.gameObject.AddComponent<CanvasGroup>();

            Image window = CreateImage("Window", root, UiImportSetup.Sprite("Kit/panel.png"), sliced: true, raycast: true);
            Place(window.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, windowSize);
            return (root, window.rectTransform);
        }

        public static void BindPopup(PopupView popup, RectTransform window)
        {
            var serialized = new SerializedObject(popup);
            GameAssetsBuilder.Find(serialized, "_group").objectReferenceValue = popup.GetComponent<CanvasGroup>();
            GameAssetsBuilder.Find(serialized, "_window").objectReferenceValue = window;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            popup.gameObject.SetActive(false);
        }

        /// <summary>Assigns serialized object references by field name.</summary>
        public static void Bind(Object target, params (string Field, Object Value)[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach ((string field, Object value) in fields)
                GameAssetsBuilder.Find(serialized, field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTransition(Button button, Sprite pressed, Sprite disabled)
        {
            if (pressed != null || disabled != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState { pressedSprite = pressed, disabledSprite = disabled, highlightedSprite = null, selectedSprite = null };
            }
            else
            {
                button.transition = Selectable.Transition.ColorTint;
            }

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }
    }
}
