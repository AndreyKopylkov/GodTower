using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GodTower.Editor
{
    /// <summary>
    /// UI art from lane I (Docs/Status/image.md) imported as sprites with their 9-slice borders,
    /// and a check for the TextMesh Pro essential resources (default font) the UI text needs.
    /// </summary>
    public static class UiImportSetup
    {
        public const string Folder = "Assets/Art/UI";
        public const string DefaultFont = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        /// <summary>9-slice borders as (left, bottom, right, top) px — image.md lists them as L/R/T/B.</summary>
        private static readonly Dictionary<string, Vector4> Borders = new()
        {
            { "Kit/panel.png", new Vector4(64, 64, 64, 64) },
            { "Kit/button_normal.png", new Vector4(48, 48, 48, 48) },
            { "Kit/button_pressed.png", new Vector4(48, 48, 48, 48) },
            { "Kit/button_disabled.png", new Vector4(48, 48, 48, 48) },
            { "Kit/button_blue.png", new Vector4(48, 48, 48, 48) },
            { "Kit/heightbar_frame.png", new Vector4(28, 28, 28, 28) },
            { "Kit/heightbar_fill.png", new Vector4(20, 20, 20, 20) },
            { "Banners/banner_hero_blue.png", new Vector4(64, 48, 64, 48) },
            { "Banners/banner_villain_red.png", new Vector4(64, 48, 64, 48) }
        };

        public static Sprite Sprite(string relativePath) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{relativePath}");

        /// <summary>The UI font (Lilita One), or TMP's LiberationSans when the font asset has not been generated.</summary>
        public static TMP_FontAsset Font => FontSetup.Font != null ? FontSetup.Font : DefaultFontAsset;

        private static TMP_FontAsset DefaultFontAsset => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFont);

        public static void Apply()
        {
            EnsureTmpEssentials();

            string[] pngs = Directory.GetFiles(Folder, "*.png", SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .Where(path => !path.Contains("/Logo/app_icon"))
                .ToArray();

            foreach (string path in pngs)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                string relative = path.Substring(Folder.Length + 1);
                Vector4 border = Borders.TryGetValue(relative, out Vector4 value) ? value : Vector4.zero;

                if (importer.textureType == TextureImporterType.Sprite && importer.spriteBorder == border && !importer.mipmapEnabled)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// TMP essentials are committed under <c>Assets/TextMesh Pro</c> (extracted from the ugui package):
        /// <c>AssetDatabase.ImportPackage</c> is deferred in batch mode and never runs before <c>-quit</c>.
        /// </summary>
        private static void EnsureTmpEssentials()
        {
            if (DefaultFontAsset == null)
                throw new System.InvalidOperationException($"TextMesh Pro essentials missing ({DefaultFont}).");
        }
    }
}
