using UnityEditor;
using UnityEngine;

namespace GodTower.Editor
{
    /// <summary>
    /// Project-wide texture budget (Docs/Plan.md §3–4, U-67), applied after the per-asset import setups:
    /// at most 1024 px everywhere and an ASTC override on Android — 4×4 for UI, palettes and other textures without
    /// mipmaps (flat colours and crisp edges), 6×6 for mip-mapped 3D textures. Mipmaps themselves are chosen by the
    /// owning setup (on for 3D surfaces and world-space clouds, off for UI and the prop palette).
    /// Asset Store packs (outside <c>Assets/Art</c> and <c>Assets/_Project</c>) are left untouched.
    /// </summary>
    public static class TextureImportPolicy
    {
        public const int MaxSize = 1024;
        private const string Android = "Android";
        private static readonly string[] Roots = { "Assets/Art", ProjectPaths.Root };

        public static void Apply()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", Roots))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer && Configure(importer))
                    importer.SaveAndReimport();
            }
        }

        /// <summary>Returns true when a setting changed.</summary>
        private static bool Configure(TextureImporter importer)
        {
            bool changed = false;
            int maxSize = Mathf.Min(importer.maxTextureSize, MaxSize);
            if (importer.maxTextureSize != maxSize)
            {
                importer.maxTextureSize = maxSize;
                changed = true;
            }

            // Deliberately uncompressed textures (the app icon, which the build re-samples) keep their platform settings.
            if (importer.textureCompression == TextureImporterCompression.Uncompressed)
                return changed;

            TextureImporterFormat format = importer.mipmapEnabled ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4;
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings(Android);
            if (!android.overridden || android.format != format || android.maxTextureSize != maxSize)
            {
                android.overridden = true;
                android.format = format;
                android.maxTextureSize = maxSize;
                android.compressionQuality = (int)TextureCompressionQuality.Normal;
                importer.SetPlatformTextureSettings(android);
                changed = true;
            }

            return changed;
        }
    }
}
