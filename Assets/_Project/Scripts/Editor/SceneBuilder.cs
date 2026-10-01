using System.Linq;
using GodTower.Scopes;
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
            CreateCamera();
            EditorSceneManager.SaveScene(scene, ProjectPaths.MenuScene);
        }

        private static void BuildGameScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateScope<GameLifetimeScope>();
            CreateCamera();
            CreateSun();
            EditorSceneManager.SaveScene(scene, ProjectPaths.GameScene);
        }

        private static void CreateScope<TScope>() where TScope : LifetimeScope
        {
            new GameObject(typeof(TScope).Name).AddComponent<TScope>();
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.36f, 0.66f, 0.95f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 1f, -10f);
        }

        private static void CreateSun()
        {
            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sunObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }
    }
}
