using GodTower.Effects;
using GodTower.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.Editor
{
    /// <summary>
    /// Builds the Game scene's screen-space UI (1080×1920 reference, portrait) like the reference video: the left vertical
    /// height bar (tower height on top, fill + hero marker with the current height), level title and timer at the top,
    /// the pause button, the event banner stacks (blue heroes left, red villains right), the bump flash overlay,
    /// and the pause / result popups on their own canvas.
    /// </summary>
    public static class GameHudBuilder
    {
        private static readonly Vector2 BannerSize = new(500f, 144f);
        private static readonly Vector2 ButtonSize = new(560f, 150f);

        /// <summary>Components of the built UI that the scene scope needs.</summary>
        public sealed class Result
        {
            public EventBannerPanel Banners;
            public ScreenFlash Flash;
            public HudView Hud;
            public PausePanelView PausePanel;
            public ResultPanelView ResultPanel;
        }

        public static Result Build()
        {
            Canvas hudCanvas = UiFactory.CreateCanvas("HUD", sortingOrder: 0, interactive: true);
            RectTransform safeArea = UiFactory.CreateSafeArea(hudCanvas.transform);

            EventBannerPanel banners = CreateBanners(safeArea);
            HudView hud = CreateHud(safeArea);
            ScreenFlash flash = CreateFlash(hudCanvas.transform);

            Canvas panelCanvas = UiFactory.CreateCanvas("Panels", sortingOrder: 10, interactive: true);
            RectTransform panelArea = UiFactory.CreateSafeArea(panelCanvas.transform);

            return new Result
            {
                Banners = banners,
                Flash = flash,
                Hud = hud,
                PausePanel = CreatePausePanel(panelArea),
                ResultPanel = CreateResultPanel(panelArea)
            };
        }

        private static HudView CreateHud(RectTransform parent)
        {
            RectTransform root = UiFactory.CreateRect("Hud", parent);
            UiFactory.Stretch(root);

            HeightBarView heightBar = CreateHeightBar(root);

            TextMeshProUGUI level = UiFactory.CreateText("Level", root, "LEVEL 1", 46f, Color.white);
            UiFactory.Place(level.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(600f, 60f));
            TextMeshProUGUI timerLabel = UiFactory.CreateText("Timer", root, "1:15", 104f, UiFactory.Yellow);
            UiFactory.Place(timerLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(500f, 120f));
            var timer = timerLabel.gameObject.AddComponent<TimerView>();
            Bind(timer, ("_label", timerLabel));

            Button pause = UiFactory.CreateRoundButton("PauseButton", root, "icon_pause", 132f);
            UiFactory.Place((RectTransform)pause.transform, new Vector2(1f, 1f), new Vector2(-36f, -36f), Vector2.one * 132f);

            var hud = root.gameObject.AddComponent<HudView>();
            Bind(hud, ("_heightBar", heightBar), ("_timer", timer), ("_levelLabel", level), ("_pauseButton", pause));
            return hud;
        }

        /// <summary>Dark capsule along the left edge with a yellow fill; the marker (hero badge + height) rides the fill.</summary>
        private static HeightBarView CreateHeightBar(RectTransform parent)
        {
            RectTransform root = UiFactory.CreateRect("HeightBar", parent);
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = new Vector2(46f, 150f);
            root.offsetMax = new Vector2(100f, -250f);

            Image frame = UiFactory.CreateImage("Frame", root, UiImportSetup.Sprite("Kit/heightbar_frame.png"), sliced: true);
            frame.pixelsPerUnitMultiplier = 1.4f;
            UiFactory.Stretch(frame.rectTransform);

            RectTransform track = UiFactory.CreateRect("Track", root);
            UiFactory.Stretch(track, inset: 9f);

            Image fill = UiFactory.CreateImage("Fill", track, UiImportSetup.Sprite("Kit/heightbar_fill.png"), sliced: true);
            fill.pixelsPerUnitMultiplier = 1.6f;
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 0f);
            fillRect.pivot = new Vector2(0.5f, 0f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero; // stretched up to anchorMax.y = height fraction (HeightBarView)

            TextMeshProUGUI max = UiFactory.CreateText("Max", root, "300", 50f, new Color(1f, 0.32f, 0.25f), TextAlignmentOptions.Left);
            UiFactory.Place(max.rectTransform, new Vector2(0f, 1f), new Vector2(-14f, 128f), new Vector2(260f, 64f));

            RectTransform marker = UiFactory.CreateRect("Marker", track);
            UiFactory.Place(marker, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(96f, 96f));
            marker.pivot = new Vector2(0.5f, 0.5f);
            Image badge = UiFactory.CreateImage("Badge", marker, UiImportSetup.Sprite("Kit/round_button_normal.png"));
            UiFactory.Stretch(badge.rectTransform);
            Sprite heroIcon = AssetDatabase.LoadAssetAtPath<Sprite>(HeroIconRenderer.OutputPath);
            Image mask = UiFactory.CreateImage("Mask", marker, UiImportSetup.Sprite("Kit/round_button_normal.png"));
            UiFactory.Stretch(mask.rectTransform, inset: 7f);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            Image portrait = UiFactory.CreateImage("Hero", mask.transform, heroIcon != null ? heroIcon : UiImportSetup.Sprite("Icons/icon_star.png"));
            UiFactory.Stretch(portrait.rectTransform, inset: heroIcon != null ? 0f : 10f);
            TextMeshProUGUI height = UiFactory.CreateText("Height", marker, "0", 60f, UiFactory.Yellow, TextAlignmentOptions.Left);
            UiFactory.Place(height.rectTransform, new Vector2(1f, 0.5f), new Vector2(10f, 0f), new Vector2(240f, 80f));
            height.rectTransform.pivot = new Vector2(0f, 0.5f);

            var view = root.gameObject.AddComponent<HeightBarView>();
            Bind(view, ("_track", track), ("_fill", fillRect), ("_marker", marker), ("_maxLabel", max), ("_heightLabel", height));
            return view;
        }

        private static EventBannerPanel CreateBanners(RectTransform parent)
        {
            RectTransform banners = UiFactory.CreateRect("EventBanners", parent);
            UiFactory.Stretch(banners);
            var panel = banners.gameObject.AddComponent<EventBannerPanel>();

            // Hero banners start right of the height bar and its marker labels.
            RectTransform heroColumn = CreateColumn("HeroColumn", banners, new Vector2(0f, 0.78f), 150f);
            RectTransform villainColumn = CreateColumn("VillainColumn", banners, new Vector2(1f, 0.7f), 0f);
            EventBannerView heroTemplate = CreateBanner("HeroBannerTemplate", heroColumn, BannerSide.Hero);
            EventBannerView villainTemplate = CreateBanner("VillainBannerTemplate", villainColumn, BannerSide.Villain);

            Bind(panel, ("_heroColumn", heroColumn), ("_villainColumn", villainColumn),
                ("_heroTemplate", heroTemplate), ("_villainTemplate", villainTemplate));
            return panel;
        }

        private static RectTransform CreateColumn(string name, RectTransform parent, Vector2 anchor, float inset)
        {
            RectTransform column = UiFactory.CreateRect(name, parent);
            column.anchorMin = column.anchorMax = anchor;
            column.pivot = new Vector2(anchor.x, 1f);
            column.sizeDelta = BannerSize;
            column.anchoredPosition = new Vector2(anchor.x < 0.5f ? inset : -inset, 0f);
            return column;
        }

        /// <summary>Banner anchored to its screen edge; the icon sits in the sprite's badge circle (outer end).</summary>
        private static EventBannerView CreateBanner(string name, RectTransform column, BannerSide side)
        {
            bool hero = side == BannerSide.Hero;
            float edge = hero ? 0f : 1f;

            Image background = UiFactory.CreateImage(name, column,
                UiImportSetup.Sprite(hero ? "Banners/banner_hero_blue.png" : "Banners/banner_villain_red.png"), sliced: true);
            RectTransform banner = background.rectTransform;
            UiFactory.Place(banner, new Vector2(edge, 1f), Vector2.zero, BannerSize);

            Image icon = UiFactory.CreateImage("Icon", banner, null);
            UiFactory.Place(icon.rectTransform, new Vector2(edge, 0.5f), new Vector2(hero ? 22f : -22f, 0f), new Vector2(100f, 100f));

            TextMeshProUGUI label = UiFactory.CreateText("Label", banner, hero ? "Hero ×1" : "Villain ×1", 50f, Color.white);
            UiFactory.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(hero ? 140f : 36f, 10f);
            label.rectTransform.offsetMax = new Vector2(hero ? -36f : -140f, -6f);

            var view = banner.gameObject.AddComponent<EventBannerView>();
            Bind(view, ("_icon", icon), ("_label", label));
            return view;
        }

        /// <summary>Full-screen white overlay for the bump flash (outside the safe area, above the HUD).</summary>
        private static ScreenFlash CreateFlash(Transform canvas)
        {
            RectTransform rect = UiFactory.CreateRect("ScreenFlash", canvas);
            UiFactory.Stretch(rect);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = false;
            var flash = rect.gameObject.AddComponent<ScreenFlash>();
            Bind(flash, ("_image", image));
            return flash;
        }

        private static PausePanelView CreatePausePanel(RectTransform parent)
        {
            (RectTransform root, RectTransform window) = UiFactory.CreatePopup("PausePanel", parent, new Vector2(760f, 900f));
            TextMeshProUGUI title = UiFactory.CreateText("Title", window, "PAUSED", 120f, UiFactory.Yellow);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(700f, 150f));

            Button resume = UiFactory.CreateButton("ResumeButton", window, "RESUME", true, "icon_play", ButtonSize, 72f);
            UiFactory.Place((RectTransform)resume.transform, new Vector2(0.5f, 1f), new Vector2(0f, -270f), ButtonSize);
            Button restart = UiFactory.CreateButton("RestartButton", window, "RESTART", false, "icon_retry", ButtonSize, 72f);
            UiFactory.Place((RectTransform)restart.transform, new Vector2(0.5f, 1f), new Vector2(0f, -460f), ButtonSize);
            Button menu = UiFactory.CreateButton("MenuButton", window, "MENU", false, "icon_home", ButtonSize, 72f);
            UiFactory.Place((RectTransform)menu.transform, new Vector2(0.5f, 1f), new Vector2(0f, -650f), ButtonSize);

            var view = root.gameObject.AddComponent<PausePanelView>();
            Bind(view, ("_resumeButton", resume), ("_restartButton", restart), ("_menuButton", menu));
            UiFactory.BindPopup(view, window);
            return view;
        }

        private static ResultPanelView CreateResultPanel(RectTransform parent)
        {
            (RectTransform root, RectTransform window) = UiFactory.CreatePopup("ResultPanel", parent, new Vector2(800f, 1120f));
            TextMeshProUGUI title = UiFactory.CreateText("Title", window, "YOU WIN!", 124f, UiFactory.Yellow);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(760f, 150f));

            Image icon = UiFactory.CreateImage("Icon", window, UiImportSetup.Sprite("Icons/icon_trophy.png"));
            UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(300f, 300f));
            icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(0f, -360f);

            TextMeshProUGUI subtitle = UiFactory.CreateText("Subtitle", window, "Level 1 complete", 60f, Color.white);
            UiFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -530f), new Vector2(760f, 80f));

            Button next = UiFactory.CreateButton("NextButton", window, "NEXT", true, "icon_next", ButtonSize, 72f);
            UiFactory.Place((RectTransform)next.transform, new Vector2(0.5f, 1f), new Vector2(0f, -680f), ButtonSize);
            Button retry = UiFactory.CreateButton("RetryButton", window, "RETRY", true, "icon_retry", ButtonSize, 72f);
            UiFactory.Place((RectTransform)retry.transform, new Vector2(0.5f, 1f), new Vector2(0f, -680f), ButtonSize);
            Button menu = UiFactory.CreateButton("MenuButton", window, "MENU", false, "icon_home", ButtonSize, 72f);
            UiFactory.Place((RectTransform)menu.transform, new Vector2(0.5f, 1f), new Vector2(0f, -870f), ButtonSize);

            var view = root.gameObject.AddComponent<ResultPanelView>();
            Bind(view, ("_title", title), ("_subtitle", subtitle), ("_icon", icon),
                ("_winIcon", UiImportSetup.Sprite("Icons/icon_trophy.png")), ("_loseIcon", UiImportSetup.Sprite("Icons/icon_retry.png")),
                ("_nextButton", next), ("_retryButton", retry), ("_menuButton", menu));
            UiFactory.BindPopup(view, window);
            return view;
        }

        private static void Bind(Object target, params (string Field, Object Value)[] fields) => UiFactory.Bind(target, fields);
    }
}
