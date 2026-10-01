using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GodTower.Editor
{
    /// <summary>
    /// Import settings for the Blender tower (lane B, Docs/Status/blender.md): static meshes without rig/animation,
    /// URP materials of our own remapped onto the FBX material slots, normal maps flagged as such.
    /// </summary>
    public static class TowerImportSetup
    {
        public const string Folder = "Assets/Art/Environment/Tower";
        public const string TopModel = Folder + "/Tower_Top.fbx";

        public static readonly string[] SegmentModels =
        {
            Folder + "/Tower_Segment_Rings.fbx",
            Folder + "/Tower_Segment_Zigzag.fbx",
            Folder + "/Tower_Segment_Relief.fbx",
            Folder + "/Tower_Segment_Ribbed.fbx"
        };

        private const string Textures = Folder + "/Textures";
        private const string MaterialsFolder = ProjectPaths.Materials + "/Environment";

        /// <summary>True when every tower model delivered by lane B is present.</summary>
        public static bool IsAvailable =>
            SegmentModels.Append(TopModel).All(path => AssetDatabase.LoadAssetAtPath<GameObject>(path) != null);

        public static void Apply()
        {
            if (!IsAvailable)
            {
                Debug.LogWarning("[TowerImportSetup] Tower models missing — the game uses primitive placeholders.");
                return;
            }

            Material stone = CreateMaterial("M_TowerStone", "T_TowerStone");
            Material top = CreateMaterial("M_TowerTop", "T_TowerTop");

            foreach (string path in SegmentModels)
                ConfigureModel(path, stone);

            ConfigureModel(TopModel, top);
        }

        private static Material CreateMaterial(string name, string texturePrefix)
        {
            Texture2D albedo = ConfigureTexture($"{Textures}/{texturePrefix}_Albedo.png", isNormalMap: false);
            Texture2D normal = ConfigureTexture($"{Textures}/{texturePrefix}_Normal.png", isNormalMap: true);
            return MaterialFactory.Lit($"{MaterialsFolder}/{name}.mat", Color.white, albedo, normal, smoothness: 0.15f);
        }

        private static Texture2D ConfigureTexture(string path, bool isNormalMap)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            TextureImporterType type = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != type || importer.maxTextureSize != 1024 || !importer.mipmapEnabled)
            {
                importer.textureType = type;
                importer.sRGBTexture = !isNormalMap;
                importer.maxTextureSize = 1024;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Each tower model uses a single material: every FBX material slot is remapped to <paramref name="material"/>.</summary>
        public static void ConfigureModel(string path, Material material)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            RemapAllMaterials(importer, material);
            importer.SaveAndReimport();
        }

        public static void RemapAllMaterials(ModelImporter importer, Material material)
        {
            var slotNames = AssetDatabase.LoadAllAssetsAtPath(importer.assetPath)
                .OfType<Material>()
                .Select(slot => slot.name)
                .Concat(importer.GetExternalObjectMap().Keys
                    .Where(id => id.type == typeof(Material))
                    .Select(id => id.name))
                .Distinct()
                .ToList();

            foreach (string slotName in slotNames)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), slotName), material);
        }
    }
}
