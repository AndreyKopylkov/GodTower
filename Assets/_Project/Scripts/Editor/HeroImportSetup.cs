using System;
using System.Linq;
using GodTower.Gameplay;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GodTower.Editor
{
    /// <summary>
    /// Integrates the hero from lane A (Docs/Status/astra.md): Humanoid avatar created from <c>Hero.fbx</c>,
    /// the clips FBX retargeted through a copy of that avatar, an animator controller with one state per clip
    /// (named as <see cref="ClimberAnimation"/>, cross-faded from code) and the <c>Hero.prefab</c> used by the Game scene.
    /// </summary>
    public static class HeroImportSetup
    {
        private const string Folder = "Assets/Art/Characters/Hero";
        private const string ModelPath = Folder + "/Hero.fbx";
        private const string AnimationsPath = Folder + "/Hero_Animations.fbx";
        private const string AlbedoPath = Folder + "/Textures/Hero_Albedo.png";
        private const string MaterialPath = ProjectPaths.Materials + "/Characters/M_Hero.mat";
        private const string ControllerPath = ProjectPaths.Animation + "/Hero.controller";
        private const string ClipPrefix = "Hero_";

        private static readonly string[] LoopingClips = { "ClimbUp", "HangIdle", "Fall", "Carried" };

        public static bool IsAvailable =>
            AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(AnimationsPath) != null;

        public static void Apply()
        {
            if (!IsAvailable)
            {
                Debug.LogWarning("[HeroImportSetup] Hero FBX missing — the climber stays a capsule placeholder.");
                return;
            }

            Material material = CreateMaterial();
            Avatar avatar = ImportModel(material);
            AnimationClip[] clips = ImportAnimations(avatar);
            AnimatorController controller = BuildController(clips);
            BuildPrefab(controller, avatar);
        }

        private static Material CreateMaterial()
        {
            var texture = (TextureImporter)AssetImporter.GetAtPath(AlbedoPath);
            if (texture.maxTextureSize != 1024 || !texture.mipmapEnabled || !texture.sRGBTexture)
            {
                texture.textureType = TextureImporterType.Default;
                texture.sRGBTexture = true;
                texture.maxTextureSize = 1024;
                texture.mipmapEnabled = true;
                texture.SaveAndReimport();
            }

            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
            return MaterialFactory.Lit(MaterialPath, Color.white, albedo, smoothness: 0.3f);
        }

        private static Avatar ImportModel(Material material)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.bakeAxisConversion = true;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            TowerImportSetup.RemapAllMaterials(importer, material);
            importer.SaveAndReimport();

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException($"Hero avatar is not a valid Humanoid avatar ({ModelPath}).");

            Debug.Log($"[HeroImportSetup] Humanoid avatar OK: {avatar.humanDescription.human.Length} bones mapped.");
            return avatar;
        }

        /// <summary>
        /// Clips FBX retargeted through a copy of the hero avatar. It contains only the armature, which Unity would collapse
        /// into the file root ("Hips" parent mismatch) — <c>preserveHierarchy</c> keeps the <c>HeroRig</c> node like in <c>Hero.fbx</c>.
        /// </summary>
        private static AnimationClip[] ImportAnimations(Avatar avatar)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(AnimationsPath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            importer.preserveHierarchy = true;
            importer.bakeAxisConversion = true;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.name = clip.takeName;
                clip.loopTime = LoopingClips.Any(name => clip.takeName == ClipPrefix + name);

                // No root motion: hips motion (incl. the 180° turn of Hero_Win) is baked into the pose.
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            if (!AssetDatabase.LoadAllAssetsAtPath(AnimationsPath).OfType<AnimationClip>().Any(clip => clip.isHumanMotion))
                throw new InvalidOperationException($"Hero clips did not import as Humanoid motion ({AnimationsPath}).");

            return AssetDatabase.LoadAllAssetsAtPath(AnimationsPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
        }

        private static AnimatorController BuildController(AnimationClip[] clips)
        {
            AssetFolders.Ensure(ProjectPaths.Animation);
            // Reuse the asset (stable GUID for the prefab) and rebuild its states.
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)
                ?? AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states)
                stateMachine.RemoveState(child.state);

            foreach (string stateName in ClimberAnimation.States)
            {
                AnimationClip clip = clips.FirstOrDefault(candidate => candidate.name == ClipPrefix + stateName)
                    ?? throw new InvalidOperationException($"Hero clip '{ClipPrefix}{stateName}' not found in {AnimationsPath}.");

                AnimatorState state = stateMachine.AddState(stateName);
                state.motion = clip;
                if (stateName == ClimberAnimation.HangIdle)
                    stateMachine.defaultState = state;
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void BuildPrefab(RuntimeAnimatorController controller, Avatar avatar)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = "Hero";

            if (!instance.TryGetComponent(out Animator animator))
                animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            AssetFolders.Ensure(ProjectPaths.Prefabs);
            PrefabUtility.SaveAsPrefabAsset(instance, ProjectPaths.HeroPrefab);
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
