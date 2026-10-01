using GodTower.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.Editor
{
    /// <summary>
    /// Builds the Menu scene's UI: logo, big green Play, blue Levels, and the level-select popup
    /// (5 tiles, locked tiles grey with a padlock, completed tiles with a star).
    /// </summary>
    public static class MenuUiBuilder
    {
        public const int LevelTiles = 5;
        private static readonly Vector2 TileSize = new(220f, 220f);

        public static MenuView Build()
        {
            Canvas canvas = UiFactory.CreateCanvas("MenuUI", sortingOrder: 0, interactive: true);
            RectTransform safeArea = UiFactory.CreateSafeArea(canvas.transform);

            Image logo = UiFactory.CreateImage("Logo", safeArea, UiImportSetup.Sprite("Logo/logo_god_tower.png"));
            UiFactory.Place(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(980f, 490f));

            var playSize = new Vector2(640f, 200f);
            Button play = UiFactory.CreateButton("PlayButton", safeArea, "PLAY", true, "icon_play", playSize, 120f);
            Object.DestroyImmediate(play.GetComponent<ButtonPressScale>()); // the Play button pulses instead
            RectTransform playRect = UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0f, 500f), playSize);
            playRect.pivot = new Vector2(0.5f, 0.5f); // pulse around the centre

            var levelsSize = new Vector2(520f, 160f);
            Button levels = UiFactory.CreateButton("LevelsButton", safeArea, "LEVELS", false, "icon_star", levelsSize, 84f);
            UiFactory.Place((RectTransform)levels.transform, new Vector2(0.5f, 0f), new Vector2(0f, 200f), levelsSize);

            (PopupView levelSelect, Button back, LevelButtonView[] tiles) = CreateLevelSelect(safeArea);

            var view = safeArea.gameObject.AddComponent<MenuView>();
            UiFactory.Bind(view, ("_logo", logo.rectTransform), ("_playButton", play), ("_levelsButton", levels),
                ("_levelSelect", levelSelect), ("_backButton", back));
            var serialized = new UnityEditor.SerializedObject(view);
            UnityEditor.SerializedProperty array = GameAssetsBuilder.Find(serialized, "_levelButtons");
            array.arraySize = tiles.Length;
            for (int i = 0; i < tiles.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = tiles[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static (PopupView, Button, LevelButtonView[]) CreateLevelSelect(RectTransform parent)
        {
            (RectTransform root, RectTransform window) = UiFactory.CreatePopup("LevelSelect", parent, new Vector2(900f, 1000f));
            TextMeshProUGUI title = UiFactory.CreateText("Title", window, "LEVELS", 120f, UiFactory.Yellow);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(800f, 150f));

            var tiles = new LevelButtonView[LevelTiles];
            for (int i = 0; i < LevelTiles; i++)
            {
                // 3 tiles on the first row, 2 centred on the second.
                int row = i / 3;
                int column = i % 3;
                int inRow = row == 0 ? 3 : LevelTiles - 3;
                float x = (column - (inRow - 1) * 0.5f) * (TileSize.x + 40f);
                float y = -350f - row * (TileSize.y + 40f);
                tiles[i] = CreateTile(window, i + 1, new Vector2(x, y));
            }

            var backSize = new Vector2(460f, 150f);
            Button back = UiFactory.CreateButton("BackButton", window, "BACK", false, "icon_home", backSize, 72f);
            UiFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), backSize);

            var popup = root.gameObject.AddComponent<PopupView>();
            UiFactory.BindPopup(popup, window);
            return (popup, back, tiles);
        }

        private static LevelButtonView CreateTile(RectTransform window, int number, Vector2 position)
        {
            Image background = UiFactory.CreateImage($"Level{number}", window, UiImportSetup.Sprite("Kit/button_normal.png"), sliced: true, raycast: true);
            RectTransform rect = background.rectTransform;
            UiFactory.Place(rect, new Vector2(0.5f, 1f), position, TileSize);
            rect.pivot = new Vector2(0.5f, 0.5f);
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            background.gameObject.AddComponent<ButtonPressScale>();

            TextMeshProUGUI label = UiFactory.CreateText("Number", rect, number.ToString(), 130f, Color.white);
            UiFactory.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(0f, 14f);

            Image padlock = UiFactory.CreateImage("Lock", rect, UiImportSetup.Sprite("Icons/icon_lock.png"));
            UiFactory.Place(padlock.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(130f, 130f));

            Image star = UiFactory.CreateImage("Star", rect, UiImportSetup.Sprite("Icons/icon_star.png"));
            UiFactory.Place(star.rectTransform, new Vector2(1f, 1f), new Vector2(26f, 26f), new Vector2(92f, 92f));

            var view = background.gameObject.AddComponent<LevelButtonView>();
            UiFactory.Bind(view, ("_button", button), ("_background", background), ("_number", label), ("_lock", padlock),
                ("_star", star), ("_unlockedSprite", UiImportSetup.Sprite("Kit/button_normal.png")),
                ("_lockedSprite", UiImportSetup.Sprite("Kit/button_disabled.png")));
            return view;
        }
    }
}
