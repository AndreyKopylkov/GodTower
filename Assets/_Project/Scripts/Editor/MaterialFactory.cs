using UnityEditor;
using UnityEngine;

namespace GodTower.Editor
{
    /// <summary>Creates or updates URP materials as assets.</summary>
    public static class MaterialFactory
    {
        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string UnlitShader = "Universal Render Pipeline/Unlit";

        public static Material Lit(string path, Color color, Texture2D baseMap = null, Texture2D normalMap = null, float smoothness = 0.2f)
        {
            Material material = LoadOrCreate(path, LitShader);
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", baseMap);
            material.SetTexture("_BumpMap", normalMap);
            material.SetFloat("_Smoothness", smoothness);
            SetKeyword(material, "_NORMALMAP", normalMap != null);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Unlit, optionally alpha-blended (telegraph markers, flat placeholders).</summary>
        public static Material Unlit(string path, Color color, bool transparent)
        {
            Material material = LoadOrCreate(path, UnlitShader);
            material.SetColor("_BaseColor", color);

            material.SetFloat("_Surface", transparent ? 1f : 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);
            material.SetFloat("_SrcBlend", transparent ? (float)UnityEngine.Rendering.BlendMode.SrcAlpha : (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlend", transparent ? (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : (float)UnityEngine.Rendering.BlendMode.Zero);
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            material.renderQueue = transparent ? (int)UnityEngine.Rendering.RenderQueue.Transparent : -1;
            material.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
            SetKeyword(material, "_SURFACE_TYPE_TRANSPARENT", transparent);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreate(string path, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                material.shader = shader;
                return material;
            }

            AssetFolders.Ensure(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }
    }
}
