using GodTower.Effects;
using GodTower.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.Editor
{
    /// <summary>
    /// Builds the Game scene's screen-space UI (1080×1920 reference, portrait). For now: the event banner stacks —
    /// blue hero banners on the left, red villain banners on the right, like the reference video.
    /// </summary>
    public static class GameHudBuilder
    {
        /// <summary>Components of the built HUD that the scene scope needs.</summary>
        public sealed class Result
        {
            public EventBannerPanel Banners;
            public ScreenFlash Flash;
        }

        public static readonly Vector2 ReferenceResolution = new(1080f, 1920f);
        private static readonly Vector2 BannerSize = new(520f, 150f);

        public static Result Build()
        {
            var canvasObject = new GameObject("HUD", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform banners = CreateRect("EventBanners", canvasObject.transform);
            Stretch(banners);
            var panel = banners.gameObject.AddComponent<EventBannerPanel>();

            RectTransform heroColumn = CreateColumn("HeroColumn", banners, new Vector2(0f, 0.8f));
            RectTransform villainColumn = CreateColumn("VillainColumn", banners, new Vector2(1f, 0.68f));
            EventBannerView heroTemplate = CreateBanner("HeroBannerTemplate", heroColumn, BannerSide.Hero);
            EventBannerView villainTemplate = CreateBanner("VillainBannerTemplate", villainColumn, BannerSide.Villain);

            var serialized = new SerializedObject(panel);
            GameAssetsBuilder.Find(serialized, "_heroColumn").objectReferenceValue = heroColumn;
            GameAssetsBuilder.Find(serialized, "_villainColumn").objectReferenceValue = villainColumn;
            GameAssetsBuilder.Find(serialized, "_heroTemplate").objectReferenceValue = heroTemplate;
            GameAssetsBuilder.Find(serialized, "_villainTemplate").objectReferenceValue = villainTemplate;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return new Result { Banners = panel, Flash = CreateFlash(canvasObject.transform) };
        }

        /// <summary>Full-screen white overlay for the bump flash (last child: drawn over the rest of the HUD).</summary>
        private static ScreenFlash CreateFlash(Transform canvas)
        {
            RectTransform rect = CreateRect("ScreenFlash", canvas);
            Stretch(rect);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = false;
            var flash = rect.gameObject.AddComponent<ScreenFlash>();
            var serialized = new SerializedObject(flash);
            GameAssetsBuilder.Find(serialized, "_image").objectReferenceValue = image;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return flash;
        }

        private static RectTransform CreateColumn(string name, RectTransform parent, Vector2 anchor)
        {
            RectTransform column = CreateRect(name, parent);
            column.anchorMin = column.anchorMax = anchor;
            column.pivot = new Vector2(anchor.x, 1f);
            column.sizeDelta = new Vector2(BannerSize.x, BannerSize.y);
            column.anchoredPosition = Vector2.zero;
            return column;
        }

        /// <summary>Banner anchored to its screen edge; the icon sits in the sprite's badge circle (outer end).</summary>
        private static EventBannerView CreateBanner(string name, RectTransform column, BannerSide side)
        {
            bool hero = side == BannerSide.Hero;
            float edge = hero ? 0f : 1f;

            RectTransform banner = CreateRect(name, column);
            banner.anchorMin = banner.anchorMax = new Vector2(edge, 1f);
            banner.pivot = new Vector2(edge, 1f);
            banner.sizeDelta = BannerSize;

            var background = banner.gameObject.AddComponent<Image>();
            background.sprite = UiImportSetup.Sprite(hero ? "Banners/banner_hero_blue.png" : "Banners/banner_villain_red.png");
            background.type = Image.Type.Sliced;
            background.raycastTarget = false;

            RectTransform iconRect = CreateRect("Icon", banner);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(edge, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(104f, 104f);
            iconRect.anchoredPosition = new Vector2(hero ? 76f : -76f, 0f);
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            RectTransform labelRect = CreateRect("Label", banner);
            Stretch(labelRect);
            labelRect.offsetMin = new Vector2(hero ? 150f : 40f, 12f);
            labelRect.offsetMax = new Vector2(hero ? -40f : -150f, -12f);
            var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = UiImportSetup.Font;
            label.text = hero ? "Hero ×1" : "Villain ×1";
            label.fontSize = 46f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            var view = banner.gameObject.AddComponent<EventBannerView>();
            var serialized = new SerializedObject(view);
            GameAssetsBuilder.Find(serialized, "_icon").objectReferenceValue = icon;
            GameAssetsBuilder.Find(serialized, "_label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
