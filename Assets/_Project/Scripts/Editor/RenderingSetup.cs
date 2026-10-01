using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GodTower.Editor
{
    /// <summary>
    /// URP look and cost (U-67): HDR + SRP Batcher on both pipeline assets, a shadow distance that covers the far follow
    /// camera, and one post-processing profile (bloom for the HDR asset-store effects, light colour grading for the
    /// saturated cartoon look of the reference, soft vignette). Mobile keeps cheap bloom (no high-quality filtering).
    /// </summary>
    public static class RenderingSetup
    {
        public const string ProfilePath = ProjectPaths.Root + "/Settings/PostProcessProfile.asset";
        private const string LegacyProfilePath = "Assets/Settings/SampleSceneProfile.asset";
        private static readonly string[] PipelineAssets = { "Assets/Settings/Mobile_RPAsset.asset", "Assets/Settings/PC_RPAsset.asset" };

        /// <summary>Camera ~58 m from the column (see <c>SceneBuilder</c>): shadows must reach past it.</summary>
        private const float ShadowDistance = 110f;

        public static void Apply()
        {
            VolumeProfile profile = BuildProfile();

            foreach (string path in PipelineAssets)
            {
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (asset == null)
                    throw new System.InvalidOperationException($"Missing URP asset {path}.");

                asset.supportsHDR = true;
                asset.useSRPBatcher = true;
                asset.shadowDistance = ShadowDistance;

                var serialized = new SerializedObject(asset);
                GameAssetsBuilder.Find(serialized, "m_VolumeProfile").objectReferenceValue = profile;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }

            // The template profile is no longer referenced by anything.
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(LegacyProfilePath) != null)
                AssetDatabase.DeleteAsset(LegacyProfilePath);
        }

        /// <summary>Main camera options every scene uses: post-processing on, FXAA (MSAA stays off on mobile).</summary>
        public static void ConfigureCamera(Camera camera)
        {
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }

        private static VolumeProfile BuildProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                AssetFolders.Ensure(System.IO.Path.GetDirectoryName(ProfilePath)?.Replace('\\', '/'));
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            var bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.6f);
            bloom.scatter.Override(0.6f);
            bloom.highQualityFiltering.Override(false);

            // No tonemapping: the cartoon palette stays crisp; HDR effects simply clip to white.
            var tonemapping = GetOrAdd<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.None);

            var color = GetOrAdd<ColorAdjustments>(profile);
            color.saturation.Override(12f);
            color.contrast.Override(6f);

            var vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.35f);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T component))
            {
                EditorUtility.SetDirty(component);
                return component;
            }

            component = profile.Add<T>();
            component.name = typeof(T).Name;
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }
    }
}
