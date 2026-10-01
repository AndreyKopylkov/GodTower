using UnityEngine;

namespace GodTower.Environment
{
    /// <summary>
    /// Look of the sky for one level (Docs/Plan.md §1, Day → Dusk): screen-space sky gradient, distance fog,
    /// sun and ambient light, cloud tint. Applied by <see cref="SkyView"/>; authored by <c>SkySetup</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "God Tower/Sky Preset", fileName = "Sky_")]
    public sealed class SkyPreset : ScriptableObject
    {
        [SerializeField] private string _displayName = "Day";

        [Header("Gradient (screen space, top to bottom)")]
        [SerializeField] private Color _zenithColor = new(0.10f, 0.38f, 0.88f);
        [SerializeField] private Color _horizonColor = new(0.62f, 0.85f, 1f);
        [SerializeField] private Color _groundColor = new(0.32f, 0.62f, 0.95f);
        [Tooltip("Screen height of the bright horizon band (0 = bottom, 1 = top).")]
        [SerializeField, Range(0f, 1f)] private float _horizonHeight = 0.3f;
        [Tooltip("Vertical size of the horizon band (screen fraction).")]
        [SerializeField, Range(0.05f, 1f)] private float _horizonSpread = 0.35f;

        [Header("Fog (linear, world meters from the camera)")]
        [SerializeField] private Color _fogColor = new(0.62f, 0.85f, 1f);
        [SerializeField, Min(0f)] private float _fogStart = 80f;
        [SerializeField, Min(1f)] private float _fogEnd = 320f;

        [Header("Light")]
        [SerializeField] private Color _sunColor = new(1f, 0.96f, 0.88f);
        [SerializeField, Min(0f)] private float _sunIntensity = 1.3f;
        [SerializeField] private Vector3 _sunEuler = new(35f, -35f, 0f);
        [SerializeField] private Color _ambientColor = new(0.55f, 0.62f, 0.72f);

        [Header("Clouds")]
        [Tooltip("Tint of the nearest clouds; farther layers blend towards the horizon colour.")]
        [SerializeField] private Color _cloudColor = Color.white;
        [SerializeField, Range(0f, 1f)] private float _cloudOpacity = 0.95f;

        public string DisplayName => _displayName;
        public Color ZenithColor => _zenithColor;
        public Color HorizonColor => _horizonColor;
        public Color GroundColor => _groundColor;
        public float HorizonHeight => _horizonHeight;
        public float HorizonSpread => _horizonSpread;
        public Color FogColor => _fogColor;
        public float FogStart => _fogStart;
        public float FogEnd => Mathf.Max(_fogStart + 1f, _fogEnd);
        public Color SunColor => _sunColor;
        public float SunIntensity => _sunIntensity;
        public Quaternion SunRotation => Quaternion.Euler(_sunEuler);
        public Color AmbientColor => _ambientColor;
        public Color CloudColor => _cloudColor;
        public float CloudOpacity => _cloudOpacity;
    }
}
