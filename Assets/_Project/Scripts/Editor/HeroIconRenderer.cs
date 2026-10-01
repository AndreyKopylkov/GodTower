using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GodTower.Editor
{
    /// <summary>
    /// Renders a head-and-shoulders portrait of the hero prefab into a transparent sprite for the height-bar marker
    /// (the reference shows a tiny character next to the height). Rendered in a preview scene, needs a graphics device
    /// (run <c>SetupProject</c> without <c>-nographics</c>); skipped when the hero prefab is missing.
    /// </summary>
    public static class HeroIconRenderer
    {
        public const string OutputPath = ProjectPaths.Root + "/UI/Generated/hero_marker.png";
        private const int Size = 256;

        public static void Render()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.HeroPrefab);
            if (prefab == null || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return;

            Scene preview = EditorSceneManager.NewPreviewScene();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            try
            {
                var hero = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
                Animator animator = hero.GetComponentInChildren<Animator>();
                Transform head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
                Vector3 focus = head != null ? head.position + Vector3.up * 0.14f : hero.transform.position + Vector3.up * 1.45f;

                // The model's face points along -Z.
                Camera camera = CreateInScene<Camera>("PortraitCamera", preview);
                camera.scene = preview;
                camera.orthographic = true;
                camera.orthographicSize = 0.42f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 10f;
                camera.transform.SetPositionAndRotation(focus + new Vector3(0.25f, 0.1f, -3f),
                    Quaternion.LookRotation(new Vector3(-0.25f, -0.1f, 3f)));
                camera.targetTexture = target;

                Light light = CreateInScene<Light>("PortraitLight", preview);
                light.type = LightType.Directional;
                light.intensity = 1.4f;
                light.transform.rotation = Quaternion.Euler(25f, 20f, 0f);

                camera.Render();
                camera.targetTexture = null;
                RenderTexture.active = target;
                var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                texture.Apply();
                RenderTexture.active = null;

                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath)!);
                File.WriteAllBytes(OutputPath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
            finally
            {
                RenderTexture.active = null;
                Object.DestroyImmediate(target);
                EditorSceneManager.ClosePreviewScene(preview);
            }

            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(OutputPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static T CreateInScene<T>(string name, Scene scene) where T : Component
        {
            var gameObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            return gameObject.AddComponent<T>();
        }
    }
}
