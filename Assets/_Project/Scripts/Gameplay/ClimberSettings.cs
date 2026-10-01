using System;
using UnityEngine;

namespace GodTower.Gameplay
{
    /// <summary>Movement tuning of the climber (Docs/Plan.md §1).</summary>
    [Serializable]
    public sealed class ClimberSettings
    {
        [Tooltip("Climb speed in displayed meters per second (1 m = 1 world unit).")]
        [SerializeField, Min(0.1f)] private float _climbSpeed = 6.6f;

        [Tooltip("Lane angles around the column axis in degrees, screen-left to screen-right (0 faces the camera).")]
        [SerializeField] private float[] _laneAngles = { -35f, 0f, 35f };

        [Tooltip("Distance from the column axis to the climber pivot (column radius 1.5 m, ledges up to 1.8 m; the hero is shown 1.75× bigger).")]
        [SerializeField, Min(0f)] private float _hangRadius = 2.3f;

        [SerializeField, Min(0.01f)] private float _shiftDuration = 0.2f;

        [Tooltip("Extra distance from the column at the middle of a lane hop.")]
        [SerializeField, Min(0f)] private float _shiftHopDistance = 0.35f;

        [Tooltip("Hit reaction before the fall starts.")]
        [SerializeField, Min(0f)] private float _hitDuration = 0.4f;

        [SerializeField, Min(0.1f)] private float _fallSpeed = 30f;
        [SerializeField, Min(0f)] private float _minFallDuration = 0.6f;

        public ClimberSettings()
        {
        }

        public ClimberSettings(float climbSpeed, float[] laneAngles, float shiftDuration, float hitDuration, float fallSpeed, float minFallDuration)
        {
            _climbSpeed = climbSpeed;
            _laneAngles = laneAngles;
            _shiftDuration = shiftDuration;
            _hitDuration = hitDuration;
            _fallSpeed = fallSpeed;
            _minFallDuration = minFallDuration;
        }

        public float ClimbSpeed => _climbSpeed;
        public float[] LaneAngles => _laneAngles;
        public float HangRadius => _hangRadius;
        public float ShiftDuration => _shiftDuration;
        public float ShiftHopDistance => _shiftHopDistance;
        public float HitDuration => _hitDuration;
        public float FallSpeed => _fallSpeed;
        public float MinFallDuration => _minFallDuration;
    }
}
