using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GodTower.Editor
{
    /// <summary>
    /// Import settings for lane B's props (Docs/Status/blender.md): one shared URP material over the palette texture
    /// (smoothness from albedo alpha, no mipmaps), static props without rig, the phoenix as a Generic rig with its
    /// looping flap clip wrapped in a prefab with an animator.
    /// </summary>
    public static class PropImportSetup
    {
        public const string Folder = "Assets/Art/Props";
        public const string Missile = Folder + "/Prop_Missile.fbx";
        public const string Truck = Folder + "/Prop_Truck.fbx";
        public const string Axe = Folder + "/Prop_Axe.fbx";
        public const string Jetpack = Folder + "/Prop_Jetpack.fbx";
        public const string BoxingGlove = Folder + "/Prop_BoxingGlove.fbx";
        public const string Trophy = Folder + "/Prop_Trophy.fbx";
        private const string PhoenixModel = Folder + "/Prop_Phoenix.fbx";
        private const string Palette = Folder + "/Textures/T_PropPalette.png";

        public const string PhoenixPrefab = ProjectPaths.Prefabs + "/Props/Phoenix.prefab";
        private const string PhoenixController = ProjectPaths.Animation + "/Phoenix.controller";
        private const string MaterialPath = ProjectPaths.Materials + "/Props/M_Props.mat";

        private static readonly string[] StaticProps = { Missile, Truck, Axe, Jetpack, BoxingGlove, Trophy };

        public static void Apply()
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(Palette) == null)
            {
                Debug.LogWarning("[PropImportSetup] Prop palette missing — events use primitive placeholders.");
                return;
            }

            Material material = CreateMaterial();
            foreach (string path in StaticProps.Where(path => AssetImporter.GetAtPath(path) != null))
                TowerImportSetup.ConfigureModel(path, material);

            if (AssetImporter.GetAtPath(PhoenixModel) != null)
                BuildPhoenix(material);
        }

        private static Material CreateMaterial()
        {
            var texture = (TextureImporter)AssetImporter.GetAtPath(Palette);
            if (texture.mipmapEnabled || texture.filterMode != FilterMode.Bilinear)
            {
                texture.mipmapEnabled = false;
                texture.filterMode = FilterMode.Bilinear;
                texture.sRGBTexture = true;
                texture.SaveAndReimport();
            }

            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(Palette);
            Material material = MaterialFactory.Lit(MaterialPath, Color.white, albedo, smoothness: 1f);

            // Per-swatch smoothness is stored in the palette's alpha channel.
            material.SetFloat("_SmoothnessTextureChannel", 1f);
            material.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildPhoenix(Material material)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(PhoenixModel);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
                clip.loopTime = true;
            importer.clipAnimations = clips;
            TowerImportSetup.RemapAllMaterials(importer, material);
            importer.SaveAndReimport();

            AnimationClip flap = AssetDatabase.LoadAllAssetsAtPath(PhoenixModel)
                .OfType<AnimationClip>()
                .First(clip => !clip.name.StartsWith("__preview__"));

            AssetFolders.Ensure(ProjectPaths.Animation);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PhoenixController)
                ?? AnimatorController.CreateAnimatorControllerAtPath(PhoenixController);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states)
                stateMachine.RemoveState(child.state);
            stateMachine.AddState("Flap").motion = flap;
            EditorUtility.SetDirty(controller);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PhoenixModel));
            if (!instance.TryGetComponent(out Animator animator))
                animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            AssetFolders.Ensure(ProjectPaths.Prefabs + "/Props");
            PrefabUtility.SaveAsPrefabAsset(instance, PhoenixPrefab);
            Object.DestroyImmediate(instance);
        }
    }
}
