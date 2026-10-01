using System.Linq;
using GodTower.Effects;
using GodTower.Environment;
using GodTower.Events;
using GodTower.Gameplay;
using GodTower.Levels;
using GodTower.Scopes;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace GodTower.Editor
{
    /// <summary>
    /// Generates the Menu and Game scenes from code so they can be rebuilt from the CLI without opening the editor.
    /// Scene content is the single source of truth here: extend the builders instead of editing scenes by hand.
    /// </summary>
    public static class SceneBuilder
    {
        /// <summary>
        /// Far follow camera (with <c>CameraRig</c>'s FOV 28°): ~29 m of tower in view, the column ~20% of the portrait width
        /// with open sky on both sides like the reference, the (scaled) hero ~11% of the screen height.
        /// </summary>
        private static readonly Vector3 CameraOffset = new(0f, 0f, -58f);

        /// <summary>
        /// The 1.8 m hero model is shown 1.75× bigger: the reference character is about as tall as the column is wide.
        /// Gameplay distances (displayed meters) are unaffected.
        /// </summary>
        public const float HeroScale = 1.75f;

        /// <summary>The hero model faces -Z; the climber root faces the column (+Z), so the visual is turned around.</summary>
        private static readonly Quaternion HeroVisualRotation = Quaternion.Euler(0f, 180f, 0f);

        private const float MenuHeroHeight = 21f;

        public static void BuildAll()
        {
            AssetFolders.Ensure(ProjectPaths.Scenes);
            BuildMenuScene();
            BuildGameScene();
            ApplyBuildSceneList();
        }

        public static void ApplyBuildSceneList()
        {
            EditorBuildSettings.scenes = ProjectPaths.BuildScenes
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();
        }

        /// <summary>Menu: the column (built at runtime by <c>MenuBackdrop</c>) with the hero hanging on it, framed close up behind the UI.</summary>
        private static void BuildMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var scope = CreateScope<MenuLifetimeScope>();
            Camera camera = CreateMainCamera();
            camera.fieldOfView = 40f;
            camera.transform.position = new Vector3(0f, MenuHeroHeight + 0.6f * HeroScale, -24f);
            SkyView sky = CreateSky();
            var towerRoot = new GameObject("Tower").transform;
            CreateMenuHero(GameSceneAssets.ClimberVisual);
            UiFactory.CreateEventSystem();
            GodTower.UI.MenuView menu = MenuUiBuilder.Build();

            UiFactory.Bind(scope, ("_levels", GameSceneAssets.Levels), ("_towerSet", GameSceneAssets.TowerSet),
                ("_towerRoot", towerRoot), ("_menuView", menu), ("_sky", sky));
            EditorSceneManager.SaveScene(scene, ProjectPaths.MenuScene);
        }

        private static void CreateMenuHero(GameObject visualPrefab)
        {
            if (visualPrefab == null)
                return;

            var root = new GameObject("MenuHero").transform;
            root.SetPositionAndRotation(ClimberView.PositionOnColumn(0f, MenuHeroHeight, GameSceneAssets.Gameplay.Climber.HangRadius),
                ClimberView.FacingColumn(0f));
            root.localScale = Vector3.one * HeroScale;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, root);
            visual.transform.localRotation = HeroVisualRotation;
        }

        private static void BuildGameScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var scope = CreateScope<GameLifetimeScope>();
            CreateMainCamera().gameObject.AddComponent<CinemachineBrain>();
            CameraRig cameraRig = CreateCameraRig();
            SkyView sky = CreateSky();
            var towerRoot = new GameObject("Tower").transform;
            ClimberView climber = CreateClimber(GameSceneAssets.ClimberVisual);
            EventStage eventStage = CreateEventStage();
            BumpStage bumpStage = CreateBumpStage();
            WinStage winStage = CreateWinStage();
            UiFactory.CreateEventSystem();
            GameHudBuilder.Result hud = GameHudBuilder.Build();

            var serialized = new SerializedObject(scope);
            GameAssetsBuilder.Find(serialized, "_levels").objectReferenceValue = GameSceneAssets.Levels;
            GameAssetsBuilder.Find(serialized, "_gameplay").objectReferenceValue = GameSceneAssets.Gameplay;
            GameAssetsBuilder.Find(serialized, "_towerSet").objectReferenceValue = GameSceneAssets.TowerSet;
            GameAssetsBuilder.Find(serialized, "_towerRoot").objectReferenceValue = towerRoot;
            GameAssetsBuilder.Find(serialized, "_climber").objectReferenceValue = climber;
            GameAssetsBuilder.Find(serialized, "_cameraRig").objectReferenceValue = cameraRig;
            GameAssetsBuilder.Find(serialized, "_events").objectReferenceValue = GameSceneAssets.Events;
            GameAssetsBuilder.Find(serialized, "_eventStage").objectReferenceValue = eventStage;
            GameAssetsBuilder.Find(serialized, "_banners").objectReferenceValue = hud.Banners;
            GameAssetsBuilder.Find(serialized, "_bumpEffect").objectReferenceValue = GameSceneAssets.BumpEffect;
            GameAssetsBuilder.Find(serialized, "_bumpStage").objectReferenceValue = bumpStage;
            GameAssetsBuilder.Find(serialized, "_screenFlash").objectReferenceValue = hud.Flash;
            GameAssetsBuilder.Find(serialized, "_hud").objectReferenceValue = hud.Hud;
            GameAssetsBuilder.Find(serialized, "_pausePanel").objectReferenceValue = hud.PausePanel;
            GameAssetsBuilder.Find(serialized, "_resultPanel").objectReferenceValue = hud.ResultPanel;
            GameAssetsBuilder.Find(serialized, "_winStage").objectReferenceValue = winStage;
            GameAssetsBuilder.Find(serialized, "_sky").objectReferenceValue = sky;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ProjectPaths.GameScene);
        }

        private static EventStage CreateEventStage()
        {
            var stage = new GameObject("EventStage").AddComponent<EventStage>();
            var serialized = new SerializedObject(stage);
            GameAssetsBuilder.Find(serialized, "_markerMaterial").objectReferenceValue =
                MaterialFactory.Unlit(ProjectPaths.Materials + "/Events/M_TelegraphMarker.mat", new Color(1f, 0.12f, 0.08f, 0.38f), transparent: true);
            GameAssetsBuilder.Find(serialized, "_placeholderMaterial").objectReferenceValue =
                MaterialFactory.Lit(ProjectPaths.Materials + "/Events/M_EventPlaceholder.mat", new Color(0.85f, 0.2f, 0.15f));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return stage;
        }

        private static BumpStage CreateBumpStage()
        {
            var stage = new GameObject("BumpStage").AddComponent<BumpStage>();
            var serialized = new SerializedObject(stage);
            GameAssetsBuilder.Find(serialized, "_placeholderMaterial").objectReferenceValue =
                MaterialFactory.Lit(ProjectPaths.Materials + "/Events/M_EventPlaceholder.mat", new Color(0.85f, 0.2f, 0.15f));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return stage;
        }

        private static WinStage CreateWinStage()
        {
            var stage = new GameObject("WinStage").AddComponent<WinStage>();
            UiFactory.Bind(stage, ("_trophy", AssetDatabase.LoadAssetAtPath<GameObject>(PropImportSetup.Trophy)),
                ("_burstEffect", AssetDatabase.LoadAssetAtPath<GameObject>(GameAssetsBuilder.CfxrPrefabs + "Explosions/CFXR4 Firework 1 Cyan-Purple (HDR).prefab")));
            return stage;
        }

        private static TScope CreateScope<TScope>() where TScope : LifetimeScope =>
            new GameObject(typeof(TScope).Name).AddComponent<TScope>();

        private static Camera CreateMainCamera()
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = 1f;
            camera.farClipPlane = 900f;
            RenderingSetup.ConfigureCamera(camera);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = CameraOffset;
            return camera;
        }

        private static CameraRig CreateCameraRig()
        {
            var rigObject = new GameObject("CameraRig");
            var target = new GameObject("CameraTarget").transform;
            target.SetParent(rigObject.transform, false);

            var impulse = rigObject.AddComponent<CinemachineImpulseSource>();
            impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            impulse.ImpulseDefinition.ImpulseDuration = 0.3f;
            impulse.DefaultVelocity = new Vector3(0.25f, -0.5f, 0f);

            var cameraObject = new GameObject("FollowCamera");
            cameraObject.transform.SetParent(rigObject.transform, false);
            cameraObject.transform.position = CameraOffset;
            var virtualCamera = cameraObject.AddComponent<CinemachineCamera>();
            virtualCamera.Target.TrackingTarget = target;

            var follow = cameraObject.AddComponent<CinemachineFollow>();
            follow.FollowOffset = CameraOffset;
            follow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
            follow.TrackerSettings.PositionDamping = new Vector3(0f, 0.35f, 0f);
            cameraObject.AddComponent<CinemachineImpulseListener>();

            var rig = rigObject.AddComponent<CameraRig>();
            var serialized = new SerializedObject(rig);
            GameAssetsBuilder.Find(serialized, "_camera").objectReferenceValue = virtualCamera;
            GameAssetsBuilder.Find(serialized, "_target").objectReferenceValue = target;
            GameAssetsBuilder.Find(serialized, "_impulse").objectReferenceValue = impulse;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rig;
        }

        /// <summary>
        /// Sun, gradient skybox and cloud layers. The scene is saved with the Day preset applied, so its lighting settings
        /// keep fog enabled (URP strips fog shader variants from builds when no scene uses fog); the scope applies the
        /// level's own preset at runtime.
        /// </summary>
        private static SkyView CreateSky()
        {
            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;

            var cloudsObject = new GameObject("Clouds");
            var clouds = cloudsObject.AddComponent<CloudField>();
            var serializedClouds = new SerializedObject(clouds);
            SerializedProperty sprites = GameAssetsBuilder.Find(serializedClouds, "_sprites");
            Sprite[] cloudSprites = SkySetup.CloudSprites;
            sprites.arraySize = cloudSprites.Length;
            for (int i = 0; i < cloudSprites.Length; i++)
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = cloudSprites[i];
            GameAssetsBuilder.Find(serializedClouds, "_material").objectReferenceValue = SkySetup.CloudMaterial;
            serializedClouds.ApplyModifiedPropertiesWithoutUndo();

            var sky = new GameObject("Sky").AddComponent<SkyView>();
            UiFactory.Bind(sky, ("_skyMaterial", SkySetup.SkyMaterial), ("_sun", sun), ("_clouds", clouds));

            RenderSettings.skybox = SkySetup.SkyMaterial;
            RenderSettings.sun = sun;
            sky.Apply(SkySetup.Preset(1));
            return sky;
        }

        /// <summary>Climber root (positioned by <see cref="ClimberView"/>) with the hero model or a capsule placeholder.</summary>
        private static ClimberView CreateClimber(GameObject visualPrefab)
        {
            var climberObject = new GameObject("Climber");
            climberObject.transform.localScale = Vector3.one * HeroScale;
            var view = climberObject.AddComponent<ClimberView>();
            Animator animator = null;

            if (visualPrefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, climberObject.transform);
                visual.transform.localRotation = HeroVisualRotation;
                animator = visual.GetComponentInChildren<Animator>();
            }
            else
            {
                GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = "Placeholder";
                Object.DestroyImmediate(capsule.GetComponent<Collider>());
                capsule.transform.SetParent(climberObject.transform, false);
                capsule.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                capsule.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
                capsule.GetComponent<MeshRenderer>().sharedMaterial =
                    MaterialFactory.Lit(ProjectPaths.Materials + "/M_ClimberPlaceholder.mat", new Color(1f, 0.55f, 0.1f));
            }

            var serialized = new SerializedObject(view);
            GameAssetsBuilder.Find(serialized, "_animator").objectReferenceValue = animator;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }
    }

    /// <summary>
    /// Generated assets the Game scene references. Loaded by path when used: opening a new scene unloads
    /// unreferenced assets, which would invalidate references held across <c>EditorSceneManager.NewScene</c>.
    /// </summary>
    public static class GameSceneAssets
    {
        public static LevelCatalog Levels => Load<LevelCatalog>(ProjectPaths.LevelCatalog);
        public static GameplayConfig Gameplay => Load<GameplayConfig>(ProjectPaths.GameplayConfig);
        public static TowerSet TowerSet => Load<TowerSet>(ProjectPaths.TowerSet);
        public static EventsConfig Events => Load<EventsConfig>(ProjectPaths.EventsConfig);
        public static BumpEffectConfig BumpEffect => Load<BumpEffectConfig>(ProjectPaths.BumpEffectConfig);

        /// <summary>Hero prefab, or null for the capsule placeholder.</summary>
        public static GameObject ClimberVisual => AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.HeroPrefab);

        private static T Load<T>(string path) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new System.InvalidOperationException($"Missing generated asset {path}.");
    }
}
