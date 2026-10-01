using GodTower.Gameplay;
using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>
    /// Tuning of the webhook <c>/bump</c> event (Docs/Plan.md §2): the boxing-glove wave, its impact feedback,
    /// layering limits and the gameplay reaction. Knockdown distances live in <see cref="KnockdownSettings"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "God Tower/Bump Effect Config", fileName = "BumpEffectConfig")]
    public sealed class BumpEffectConfig : ScriptableObject
    {
        [Header("Gloves")]
        [Tooltip("Glove model (right glove punching along +Z, pivot at the wrist); left gloves are mirrored. A cube is used when empty.")]
        [SerializeField] private GameObject _glovePrefab;
        [SerializeField, Min(0.01f)] private float _gloveScale = 5f;
        [Tooltip("Distance from the glove pivot (wrist) to the front of the fist, in model units.")]
        [SerializeField, Min(0f)] private float _gloveReach = 0.72f;
        [SerializeField, Min(1)] private int _minGloves = 6;
        [SerializeField, Min(1)] private int _maxGloves = 8;

        [Header("Flight")]
        [SerializeField] private Vector2 _flightDuration = new(0.32f, 0.45f);
        [Tooltip("Delay between consecutive gloves of one wave.")]
        [SerializeField, Min(0f)] private float _stagger = 0.05f;
        [Tooltip("How far outside the screen edge gloves spawn (viewport units).")]
        [SerializeField, Min(0f)] private float _edgeMargin = 0.06f;
        [Tooltip("Spawn depth in front of the climber, towards the camera (meters): gloves start big and punch inwards.")]
        [SerializeField] private Vector2 _spawnDepthTowardsCamera = new(3f, 9f);
        [Tooltip("Sideways bend of the arc (meters).")]
        [SerializeField] private Vector2 _arcBend = new(2f, 5f);
        [Tooltip("Roll (degrees) unwound during the flight: the punch-in twist.")]
        [SerializeField] private Vector2 _punchRoll = new(140f, 260f);
        [Tooltip("Spread of the contact points around the climber's chest (meters).")]
        [SerializeField] private Vector3 _contactSpread = new(0.75f, 0.95f, 0.3f);
        [SerializeField, Min(0f)] private float _recoilDistance = 1.9f;
        [SerializeField, Min(0.01f)] private float _recoilDuration = 0.32f;

        [Header("Impact")]
        [Tooltip("Burst spawned at every glove contact (optional asset-store effect).")]
        [SerializeField] private GameObject _hitEffect;
        [SerializeField, Min(0.01f)] private float _hitEffectScale = 2f;
        [Tooltip("Comic text burst at the first contact of a wave (optional asset-store effect).")]
        [SerializeField] private GameObject _comicEffect;
        [SerializeField, Min(0.01f)] private float _comicEffectScale = 1f;
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField, Range(0f, 1f)] private float _flashAlpha = 0.6f;
        [SerializeField, Min(0.01f)] private float _flashDuration = 0.25f;
        [SerializeField, Min(0f)] private float _firstContactShake = 1.6f;
        [SerializeField, Min(0f)] private float _contactShake = 0.35f;
        [Tooltip("Punch sounds per wave (not one per glove, which would just be noise).")]
        [SerializeField, Min(1)] private int _punchSoundsPerWave = 3;

        [Header("Gameplay")]
        [Tooltip("Hit reaction and shortest fall: together they lock input for about half a second.")]
        [SerializeField, Min(0f)] private float _hitDuration = 0.25f;
        [SerializeField, Min(0f)] private float _minFallDuration = 0.25f;
        [Tooltip("Visual-only hit animation when the knockdown cap leaves nothing to take (no input lock).")]
        [SerializeField, Min(0f)] private float _flinchDuration = 0.35f;

        [Header("Layering")]
        [SerializeField, Min(1)] private int _maxConcurrentWaves = 3;
        [Tooltip("Requests waiting for a free wave slot; requests beyond this are dropped.")]
        [SerializeField, Min(0)] private int _maxQueuedWaves = 24;

        public GameObject GlovePrefab => _glovePrefab;
        public float GloveScale => _gloveScale;
        public float GloveReach => _gloveReach * _gloveScale;
        public int MinGloves => Mathf.Min(_minGloves, _maxGloves);
        public int MaxGloves => Mathf.Max(_minGloves, _maxGloves);
        public Vector2 FlightDuration => _flightDuration;
        public float Stagger => _stagger;
        public float EdgeMargin => _edgeMargin;
        public Vector2 SpawnDepthTowardsCamera => _spawnDepthTowardsCamera;
        public Vector2 ArcBend => _arcBend;
        public Vector2 PunchRoll => _punchRoll;
        public Vector3 ContactSpread => _contactSpread;
        public float RecoilDistance => _recoilDistance;
        public float RecoilDuration => _recoilDuration;
        public GameObject HitEffect => _hitEffect;
        public float HitEffectScale => _hitEffectScale;
        public GameObject ComicEffect => _comicEffect;
        public float ComicEffectScale => _comicEffectScale;
        public Color FlashColor => _flashColor;
        public float FlashAlpha => _flashAlpha;
        public float FlashDuration => _flashDuration;
        public float FirstContactShake => _firstContactShake;
        public float ContactShake => _contactShake;
        public int PunchSoundsPerWave => _punchSoundsPerWave;
        public HitReaction Reaction => new(_hitDuration, _minFallDuration);
        public float FlinchDuration => _flinchDuration;
        public int MaxConcurrentWaves => _maxConcurrentWaves;
        public int MaxQueuedWaves => _maxQueuedWaves;

        /// <summary>Longest time a wave can take (last glove start + flight + recoil), used as a watchdog.</summary>
        public float MaxWaveDuration => (MaxGloves - 1) * _stagger + _flightDuration.y + _recoilDuration;
    }
}
