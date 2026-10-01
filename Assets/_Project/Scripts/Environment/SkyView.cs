using UnityEngine;
using UnityEngine.Rendering;

namespace GodTower.Environment
{
    /// <summary>
    /// Applies a <see cref="SkyPreset"/> to the scene: gradient skybox (a runtime copy of the sky material, so the asset is
    /// never modified), linear fog, sun, flat ambient light and cloud tint.
    /// </summary>
    public sealed class SkyView : MonoBehaviour
    {
        private static readonly int ZenithColor = Shader.PropertyToID("_ZenithColor");
        private static readonly int HorizonColor = Shader.PropertyToID("_HorizonColor");
        private static readonly int GroundColor = Shader.PropertyToID("_GroundColor");
        private static readonly int HorizonHeight = Shader.PropertyToID("_HorizonHeight");
        private static readonly int HorizonSpread = Shader.PropertyToID("_HorizonSpread");

        [Tooltip("GodTower/Sky Gradient material used as the template of the skybox.")]
        [SerializeField] private Material _skyMaterial;
        [SerializeField] private Light _sun;
        [SerializeField] private CloudField _clouds;

        private Material _skyInstance;

        public SkyPreset Current { get; private set; }

        public void Apply(SkyPreset preset)
        {
            if (preset == null)
                return;

            Current = preset;
            ApplySky(preset);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = preset.FogColor;
            RenderSettings.fogStartDistance = preset.FogStart;
            RenderSettings.fogEndDistance = preset.FogEnd;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = preset.AmbientColor;

            if (_sun != null)
            {
                _sun.color = preset.SunColor;
                _sun.intensity = preset.SunIntensity;
                _sun.transform.rotation = preset.SunRotation;
            }

            if (_clouds != null)
                _clouds.SetColors(preset.CloudColor, preset.HorizonColor, preset.CloudOpacity);
        }

        private void ApplySky(SkyPreset preset)
        {
            if (_skyMaterial == null)
                return;

            // At edit time (scene generation) the template itself is set up and saved with the scene.
            Material sky = _skyMaterial;
            if (Application.isPlaying)
            {
                if (_skyInstance == null)
                    _skyInstance = new Material(_skyMaterial) { name = _skyMaterial.name + " (Runtime)" };
                sky = _skyInstance;
            }

            sky.SetColor(ZenithColor, preset.ZenithColor);
            sky.SetColor(HorizonColor, preset.HorizonColor);
            sky.SetColor(GroundColor, preset.GroundColor);
            sky.SetFloat(HorizonHeight, preset.HorizonHeight);
            sky.SetFloat(HorizonSpread, preset.HorizonSpread);
            RenderSettings.skybox = sky;
        }

        private void OnDestroy()
        {
            if (_skyInstance == null)
                return;

            if (RenderSettings.skybox == _skyInstance)
                RenderSettings.skybox = _skyMaterial;
            Destroy(_skyInstance);
        }
    }
}
