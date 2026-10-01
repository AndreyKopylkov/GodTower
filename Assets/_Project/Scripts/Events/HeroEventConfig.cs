using UnityEngine;

namespace GodTower.Events
{
    /// <summary>One hero kind: how far and how long it carries the climber, and its presentation.</summary>
    [CreateAssetMenu(menuName = "God Tower/Hero Event", fileName = "Hero_")]
    public sealed class HeroEventConfig : ScriptableObject
    {
        [SerializeField] private HeroKind _kind;
        [SerializeField] private string _displayName = "Hero";
        [SerializeField] private Sprite _icon;

        [Tooltip("Lift as a fraction of the tower height.")]
        [SerializeField, Range(0f, 1f)] private float _heightFraction = 0.1f;
        [SerializeField, Min(0.1f)] private float _duration = 2f;

        [Tooltip("Prop attached to the climber during the boost; a primitive placeholder is used when empty.")]
        [SerializeField] private GameObject _prop;
        [SerializeField] private Vector3 _propOffset;
        [SerializeField] private Vector3 _propEuler;
        [SerializeField, Min(0.01f)] private float _propScale = 1f;

        [Tooltip("Looping effect attached to the prop for the whole boost (optional asset-store effect).")]
        [SerializeField] private GameObject _trailEffect;
        [SerializeField] private Vector3 _trailOffset;
        [SerializeField, Min(0.01f)] private float _trailEffectScale = 1f;

        [Tooltip("Burst at the start of the boost (optional asset-store effect).")]
        [SerializeField] private GameObject _startEffect;
        [SerializeField, Min(0.01f)] private float _startEffectScale = 1f;

        [Tooltip("Looping effect around the climber for the whole boost, e.g. the phoenix fire (optional asset-store effect).")]
        [SerializeField] private GameObject _auraEffect;
        [SerializeField] private Vector3 _auraOffset;
        [SerializeField, Min(0.01f)] private float _auraEffectScale = 1f;

        [Tooltip("Full-screen colour flash at the start of the boost (alpha 0 = none).")]
        [SerializeField] private Color _flashColor = Color.clear;
        [SerializeField, Min(0.01f)] private float _flashDuration = 0.6f;

        [SerializeField] private bool _zoomOutCamera;

        public HeroKind Kind => _kind;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public float HeightFraction => _heightFraction;
        public float Duration => _duration;
        public GameObject Prop => _prop;
        public Vector3 PropOffset => _propOffset;
        public Quaternion PropRotation => Quaternion.Euler(_propEuler);
        public float PropScale => _propScale;
        public GameObject TrailEffect => _trailEffect;
        public Vector3 TrailOffset => _trailOffset;
        public float TrailEffectScale => _trailEffectScale;
        public GameObject StartEffect => _startEffect;
        public float StartEffectScale => _startEffectScale;
        public GameObject AuraEffect => _auraEffect;
        public Vector3 AuraOffset => _auraOffset;
        public float AuraEffectScale => _auraEffectScale;
        public Color FlashColor => _flashColor;
        public float FlashDuration => _flashDuration;
        public bool ZoomOutCamera => _zoomOutCamera;
    }
}
