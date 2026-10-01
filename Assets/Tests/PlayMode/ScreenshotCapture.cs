using System.IO;
using UnityEngine;

namespace GodTower.Tests.PlayMode
{
    /// <summary>
    /// Saves what the main camera sees, UI included, to <c>TestResults/Screenshots/</c> for visual review.
    /// Works under <c>-batchmode</c> (where <c>WaitForEndOfFrame</c> never fires and <c>ScreenCapture</c> misses overlay UI):
    /// overlay canvases are switched to Screen Space - Camera and the camera renders into a render texture.
    /// Needs a graphics device (do not run with <c>-nographics</c>); without one it does nothing.
    /// </summary>
    public static class ScreenshotCapture
    {
        public const string Folder = "TestResults/Screenshots";
        private const int Width = 540;
        private const int Height = 960;

        public static void Save(string name)
        {
            Camera camera = Camera.main;
            if (camera == null || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return;

            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    continue;

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 0.5f;
            }

            var target = RenderTexture.GetTemporary(Width, Height, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                camera.Render();

                RenderTexture.active = target;
                var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();

                Directory.CreateDirectory(Folder);
                File.WriteAllBytes(Path.Combine(Folder, name + ".png"), texture.EncodeToPNG());
                Object.Destroy(texture);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
