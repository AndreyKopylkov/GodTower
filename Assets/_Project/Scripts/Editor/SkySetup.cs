using System.IO;
using System.Linq;
using GodTower.Environment;
using UnityEditor;
using UnityEngine;

namespace GodTower.Editor
{
    /// <summary>
    /// Sky assets: lane I's clouds imported as sprites, the gradient skybox and cloud materials, and the five level
    /// <see cref="SkyPreset"/>s (Docs/Plan.md §1: Day → Bright afternoon → Golden hour → Sunset → Dusk).
    /// The preset table lives here, like the level table in <see cref="GameAssetsBuilder"/>.
    /// </summary>
    public static class SkySetup
    {
        public const string CloudFolder = "Assets/Art/Environment/Sky";
        public const string SkyShader = "Assets/_Project/Shaders/SkyGradient.shader";
        public const string SkyMaterialPath = ProjectPaths.Materials + "/Environment/M_Sky.mat";
        public const string CloudMaterialPath = ProjectPaths.Materials + "/Environment/M_Cloud.mat";
        private const string CloudShader = "Universal Render Pipeline/2D/Sprite-Unlit-Default";
        private const string PresetFolder = ProjectPaths.Configs + "/Sky";

        private static readonly PresetDefinition[] Presets =
        {
            new("Day",
                zenith: new Color(0.09f, 0.36f, 0.86f), horizon: new Color(0.60f, 0.84f, 1f), ground: new Color(0.30f, 0.60f, 0.94f),
                horizonHeight: 0.28f, spread: 0.45f, fog: new Color(0.62f, 0.84f, 0.98f), fogStart: 90f, fogEnd: 380f,
                sun: new Color(1f, 0.96f, 0.88f), sunIntensity: 1.35f, sunEuler: new Vector3(38f, -35f, 0f),
                ambient: new Color(0.56f, 0.63f, 0.74f), cloud: Color.white, cloudOpacity: 0.95f),
            new("Bright afternoon",
                zenith: new Color(0.04f, 0.42f, 0.95f), horizon: new Color(0.72f, 0.91f, 1f), ground: new Color(0.42f, 0.74f, 0.98f),
                horizonHeight: 0.24f, spread: 0.5f, fog: new Color(0.72f, 0.90f, 1f), fogStart: 100f, fogEnd: 420f,
                sun: new Color(1f, 0.98f, 0.93f), sunIntensity: 1.5f, sunEuler: new Vector3(52f, -25f, 0f),
                ambient: new Color(0.62f, 0.68f, 0.76f), cloud: Color.white, cloudOpacity: 1f),
            new("Golden hour",
                zenith: new Color(0.16f, 0.36f, 0.78f), horizon: new Color(1f, 0.80f, 0.52f), ground: new Color(0.60f, 0.60f, 0.72f),
                horizonHeight: 0.3f, spread: 0.5f, fog: new Color(0.95f, 0.80f, 0.62f), fogStart: 90f, fogEnd: 360f,
                sun: new Color(1f, 0.80f, 0.52f), sunIntensity: 1.35f, sunEuler: new Vector3(22f, -50f, 0f),
                ambient: new Color(0.62f, 0.56f, 0.52f), cloud: new Color(1f, 0.90f, 0.74f), cloudOpacity: 0.95f),
            new("Sunset",
                zenith: new Color(0.20f, 0.22f, 0.55f), horizon: new Color(1f, 0.52f, 0.32f), ground: new Color(0.60f, 0.33f, 0.42f),
                horizonHeight: 0.32f, spread: 0.55f, fog: new Color(0.85f, 0.50f, 0.45f), fogStart: 80f, fogEnd: 330f,
                sun: new Color(1f, 0.58f, 0.36f), sunIntensity: 1.25f, sunEuler: new Vector3(12f, -60f, 0f),
                ambient: new Color(0.56f, 0.45f, 0.50f), cloud: new Color(1f, 0.72f, 0.62f), cloudOpacity: 0.95f),
            new("Dusk",
                zenith: new Color(0.05f, 0.07f, 0.24f), horizon: new Color(0.52f, 0.36f, 0.62f), ground: new Color(0.14f, 0.13f, 0.32f),
                horizonHeight: 0.3f, spread: 0.55f, fog: new Color(0.30f, 0.26f, 0.45f), fogStart: 70f, fogEnd: 300f,
                sun: new Color(0.72f, 0.68f, 1f), sunIntensity: 0.9f, sunEuler: new Vector3(25f, -40f, 0f),
                ambient: new Color(0.40f, 0.42f, 0.60f), cloud: new Color(0.70f, 0.64f, 0.86f), cloudOpacity: 0.85f)
        };

        public static int PresetCount => Presets.Length;

        /// <summary>Preset of level <paramref name="number"/> (1-based; levels past the table reuse the last preset).</summary>
        public static SkyPreset Preset(int number) =>
            AssetDatabase.LoadAssetAtPath<SkyPreset>(PresetPath(Mathf.Clamp(number, 1, Presets.Length)));

        public static Sprite[] CloudSprites => Directory.GetFiles(CloudFolder, "cloud_*.png")
            .OrderBy(path => path)
            .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace('\\', '/')))
            .Where(sprite => sprite != null)
            .ToArray();

        public static Material SkyMaterial => AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);

        public static Material CloudMaterial => AssetDatabase.LoadAssetAtPath<Material>(CloudMaterialPath);

        public static void Apply()
        {
            ImportClouds();
            BuildMaterials();
            AssetFolders.Ensure(PresetFolder);
            for (int i = 0; i < Presets.Length; i++)
                BuildPreset(i + 1, Presets[i]);
        }

        /// <summary>Clouds are world-space sprites at varying distances: mipmaps on, 512 px is plenty.</summary>
        private static void ImportClouds()
        {
            if (!AssetDatabase.IsValidFolder(CloudFolder))
            {
                Debug.LogWarning("[SkySetup] No cloud art — the sky has no clouds.");
                return;
            }

            foreach (string file in Directory.GetFiles(CloudFolder, "cloud_*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
                if (importer.textureType == TextureImporterType.Sprite && importer.mipmapEnabled && importer.maxTextureSize == 512)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 512;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }

        private static void BuildMaterials()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(SkyShader)
                         ?? throw new System.InvalidOperationException($"Missing sky shader {SkyShader}.");
            Material sky = LoadOrCreateMaterial(SkyMaterialPath, shader);
            PresetDefinition day = Presets[0];
            sky.SetColor("_ZenithColor", day.Zenith);
            sky.SetColor("_HorizonColor", day.Horizon);
            sky.SetColor("_GroundColor", day.Ground);
            EditorUtility.SetDirty(sky);

            Shader cloudShader = Shader.Find(CloudShader)
                                 ?? throw new System.InvalidOperationException($"Missing shader {CloudShader}.");
            EditorUtility.SetDirty(LoadOrCreateMaterial(CloudMaterialPath, cloudShader));
        }

        private static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                material.shader = shader;
                return material;
            }

            AssetFolders.Ensure(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void BuildPreset(int number, PresetDefinition definition)
        {
            SkyPreset preset = GameAssetsBuilder.LoadOrCreate<SkyPreset>(PresetPath(number));
            var serialized = new SerializedObject(preset);
            GameAssetsBuilder.Find(serialized, "_displayName").stringValue = definition.Name;
            GameAssetsBuilder.Find(serialized, "_zenithColor").colorValue = definition.Zenith;
            GameAssetsBuilder.Find(serialized, "_horizonColor").colorValue = definition.Horizon;
            GameAssetsBuilder.Find(serialized, "_groundColor").colorValue = definition.Ground;
            GameAssetsBuilder.Find(serialized, "_horizonHeight").floatValue = definition.HorizonHeight;
            GameAssetsBuilder.Find(serialized, "_horizonSpread").floatValue = definition.Spread;
            GameAssetsBuilder.Find(serialized, "_fogColor").colorValue = definition.Fog;
            GameAssetsBuilder.Find(serialized, "_fogStart").floatValue = definition.FogStart;
            GameAssetsBuilder.Find(serialized, "_fogEnd").floatValue = definition.FogEnd;
            GameAssetsBuilder.Find(serialized, "_sunColor").colorValue = definition.Sun;
            GameAssetsBuilder.Find(serialized, "_sunIntensity").floatValue = definition.SunIntensity;
            GameAssetsBuilder.Find(serialized, "_sunEuler").vector3Value = definition.SunEuler;
            GameAssetsBuilder.Find(serialized, "_ambientColor").colorValue = definition.Ambient;
            GameAssetsBuilder.Find(serialized, "_cloudColor").colorValue = definition.Cloud;
            GameAssetsBuilder.Find(serialized, "_cloudOpacity").floatValue = definition.CloudOpacity;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string PresetPath(int number) =>
            $"{PresetFolder}/Sky_{number:00}_{Presets[number - 1].Name.Replace(" ", string.Empty)}.asset";

        private readonly struct PresetDefinition
        {
            public readonly string Name;
            public readonly Color Zenith, Horizon, Ground, Fog, Sun, Ambient, Cloud;
            public readonly float HorizonHeight, Spread, FogStart, FogEnd, SunIntensity, CloudOpacity;
            public readonly Vector3 SunEuler;

            public PresetDefinition(string name, Color zenith, Color horizon, Color ground, float horizonHeight, float spread,
                Color fog, float fogStart, float fogEnd, Color sun, float sunIntensity, Vector3 sunEuler, Color ambient,
                Color cloud, float cloudOpacity)
            {
                Name = name;
                Zenith = zenith;
                Horizon = horizon;
                Ground = ground;
                HorizonHeight = horizonHeight;
                Spread = spread;
                Fog = fog;
                FogStart = fogStart;
                FogEnd = fogEnd;
                Sun = sun;
                SunIntensity = sunIntensity;
                SunEuler = sunEuler;
                Ambient = ambient;
                Cloud = cloud;
                CloudOpacity = cloudOpacity;
            }
        }
    }
}
