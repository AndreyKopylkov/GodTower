using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace GodTower.Editor
{
    /// <summary>
    /// TextMesh Pro font asset of the UI font: Lilita One (Google Fonts, SIL OFL 1.1 — license next to the TTF),
    /// a bold casual display face close to the reference's chunky lettering. The default material carries a dark outline
    /// and a drop shadow so white/yellow text reads on the sky and on the panels.
    /// </summary>
    public static class FontSetup
    {
        public const string Folder = "Assets/Art/Fonts/LilitaOne";
        public const string SourceFont = Folder + "/LilitaOne-Regular.ttf";
        public const string FontAssetPath = Folder + "/LilitaOne SDF.asset";

        private const int SamplingPointSize = 72;
        private const int AtlasPadding = 10;
        private const int AtlasSize = 1024;

        /// <summary>ASCII plus the few symbols the UI prints.</summary>
        private static readonly string Characters = BuildCharacters();

        public static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

        public static void Apply()
        {
            TMP_FontAsset fontAsset = Font;
            if (fontAsset == null)
                fontAsset = CreateFontAsset();
            if (fontAsset == null)
                return;

            ConfigureMaterial(fontAsset.material);
            EditorUtility.SetDirty(fontAsset.material);
            EditorUtility.SetDirty(fontAsset);
        }

        private static TMP_FontAsset CreateFontAsset()
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(SourceFont);
            if (source == null)
            {
                Debug.LogWarning($"[FontSetup] {SourceFont} missing — UI falls back to LiberationSans.");
                return null;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(source, SamplingPointSize, AtlasPadding,
                GlyphRenderMode.SDFAA, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: false);
            fontAsset.name = "LilitaOne SDF";
            if (!fontAsset.TryAddCharacters(Characters, out string missing))
                Debug.LogWarning($"[FontSetup] Characters missing from Lilita One: '{missing}'.");

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            fontAsset.atlasTexture.name = "LilitaOne SDF Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            fontAsset.material.name = "LilitaOne SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        private static void ConfigureMaterial(Material material)
        {
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.06f, 0.09f, 0.24f, 1f));
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
            material.EnableKeyword(ShaderUtilities.Keyword_Outline);

            material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.03f, 0.05f, 0.15f, 0.55f));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.35f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.45f);
            material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.2f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.15f);
            material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        }

        private static string BuildCharacters()
        {
            var builder = new System.Text.StringBuilder();
            for (char c = ' '; c <= '~'; c++)
                builder.Append(c);
            return builder.Append("×—–…’•").ToString();
        }
    }
}
