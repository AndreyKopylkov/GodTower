using System.Linq;
using GodTower.Effects;
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
        private static readonly Color SkyColor = new(0.24f, 0.55f, 0.93f);

        /// <summary>Camera distance and offset that frame a 1.8 m climber at ~12% of the screen height (FOV 34°).</summary>
        private static readonly Vector3 CameraOffset = new(0f, 0f, -24f);

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

        private static void BuildMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateScope<MenuLifetimeScope>();
            CreateMainCamera();
            EditorSceneManager.SaveScene(scene, ProjectPaths.MenuScene);
        }

        private static void BuildGameScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var scope = CreateScope<GameLifetimeScope>();
            CreateMainCamera().gameObject.AddComponent<CinemachineBrain>();
            CameraRig cameraRig = CreateCameraRig();
            CreateSun();
            var towerRoot = new GameObject("Tower").transform;
            ClimberView climber = CreateClimber(GameSceneAssets.ClimberVisual);
            EventStage eventStage = CreateEventStage();
            BumpStage bumpStage = CreateBumpStage();
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

        private static TScope CreateScope<TScope>() where TScope : LifetimeScope =>
            new GameObject(typeof(TScope).Name).AddComponent<TScope>();

        private static Camera CreateMainCamera()
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SkyColor;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 400f;
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

        private static void CreateSun()
        {
            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.3f;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.62f, 0.72f);
        }

        /// <summary>Climber root (positioned by <see cref="ClimberView"/>) with the hero model or a capsule placeholder.</summary>
        private static ClimberView CreateClimber(GameObject visualPrefab)
        {
            var climberObject = new GameObject("Climber");
            var view = climberObject.AddComponent<ClimberView>();
            Animator animator = null;

            if (visualPrefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, climberObject.transform);
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
