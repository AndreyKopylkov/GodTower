using UnityEngine;

namespace GodTower.Events
{
    /// <summary>Presentation of one villain kind (banner, prop, effects). Gameplay timing lives in the level config.</summary>
    [CreateAssetMenu(menuName = "God Tower/Villain Event", fileName = "Villain_")]
    public sealed class VillainEventConfig : ScriptableObject
    {
        [SerializeField] private VillainKind _kind;
        [SerializeField] private string _displayName = "Villain";
        [SerializeField] private Sprite _icon;

        [Tooltip("Prop model; a primitive placeholder is used when empty.")]
        [SerializeField] private GameObject _prop;
        [SerializeField, Min(0.01f)] private float _propScale = 1f;

        [Tooltip("Burst played where the villain lands (optional asset-store effect).")]
        [SerializeField] private GameObject _impactEffect;
        [SerializeField, Min(0.01f)] private float _impactEffectScale = 1f;

        [Tooltip("Seconds the prop is visible before the impact.")]
        [SerializeField, Min(0.1f)] private float _approachDuration = 0.8f;

        [SerializeField, Min(0f)] private float _shakeOnImpact = 0.4f;
        [SerializeField, Min(0f)] private float _shakeOnHit = 1f;

        public VillainKind Kind => _kind;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public GameObject Prop => _prop;
        public float PropScale => _propScale;
        public GameObject ImpactEffect => _impactEffect;
        public float ImpactEffectScale => _impactEffectScale;
        public float ApproachDuration => _approachDuration;
        public float ShakeOnImpact => _shakeOnImpact;
        public float ShakeOnHit => _shakeOnHit;
    }
}
