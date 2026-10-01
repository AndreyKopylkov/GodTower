using PrimeTween;
using UnityEngine;

namespace GodTower.Gameplay
{
    /// <summary>
    /// Places the climber on the column from <see cref="ClimberMotor"/> state and plays the matching animator state.
    /// Works without an animator (primitive placeholder).
    /// </summary>
    public sealed class ClimberView : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField, Min(0f)] private float _crossFade = 0.12f;

        private ClimberState? _shownState;
        private float _hangRadius;
        private float _hopDistance;
        private float _flinchUntil;

        /// <summary>Chest height of the unscaled 1.8 m hero model.</summary>
        private const float ChestHeight = 1.1f;

        /// <summary>Uniform display scale of the climber (the scene shows the hero bigger than 1 m = 1 unit).</summary>
        public float Scale => transform.lossyScale.y;

        /// <summary>Chest height above the climber's feet in world units.</summary>
        public float CenterHeight => ChestHeight * Scale;

        /// <summary>World point the camera and effects aim at (chest height).</summary>
        public Vector3 Center => transform.position + Vector3.up * CenterHeight;

        public void Configure(ClimberSettings settings)
        {
            _hangRadius = settings.HangRadius;
            _hopDistance = settings.ShiftHopDistance;
        }

        /// <summary>Position on the column for a lane angle and height (tower axis = world Y axis through the origin).</summary>
        public static Vector3 PositionOnColumn(float angleDegrees, float height, float radius)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians) * radius, height, -Mathf.Cos(radians) * radius);
        }

        /// <summary>Rotation facing the column axis (the camera sees the climber's back).</summary>
        public static Quaternion FacingColumn(float angleDegrees) => Quaternion.Euler(0f, -angleDegrees, 0f);

        public Vector3 LanePosition(float angleDegrees, float height) => PositionOnColumn(angleDegrees, height, _hangRadius);

        public void Apply(ClimberMotor motor)
        {
            if (motor.State != ClimberState.Win)
            {
                float radius = _hangRadius + _hopDistance * motor.ShiftHop;
                transform.SetPositionAndRotation(
                    PositionOnColumn(motor.LaneAngle, motor.Height, radius),
                    FacingColumn(motor.LaneAngle));
            }

            if (_shownState == motor.State || Time.time < _flinchUntil)
                return;

            _shownState = motor.State;
            CrossFade(ClimberAnimation.HashFor(motor.State, motor.ShiftDirection));
        }

        /// <summary>
        /// Plays the hit animation for <paramref name="duration"/> seconds without touching the motor (visual-only reaction,
        /// e.g. a webhook bump whose knockdown is fully capped). The motor's animation resumes afterwards.
        /// </summary>
        public void PlayFlinch(float duration)
        {
            _flinchUntil = Time.time + duration;
            _shownState = null;
            CrossFade(Animator.StringToHash(ClimberAnimation.Hit));
        }

        private void CrossFade(int stateHash)
        {
            if (_animator != null && _animator.runtimeAnimatorController != null)
                _animator.CrossFadeInFixedTime(stateHash, _crossFade);
        }

        /// <summary>Hops from the column onto the top deck (the win animation then plays there).</summary>
        public Tween HopOnto(Vector3 deckPoint, float duration)
        {
            Vector3 start = transform.position;
            return Tween.Custom(transform, 0f, 1f, duration, (target, t) =>
            {
                Vector3 position = Vector3.Lerp(start, deckPoint, t);
                position.y += Mathf.Sin(t * Mathf.PI) * 1.5f;
                target.position = position;
            }, Ease.InOutSine);
        }

        private void OnDestroy() => Tween.StopAll(transform);
    }
}
