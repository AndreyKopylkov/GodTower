using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;

namespace GodTower.Gameplay
{
    /// <summary>
    /// Front view of the column that follows the climber's height (Cinemachine follow with damping on a target
    /// that stays on the tower axis, so the column stays centred). Provides camera shakes and the hero-boost zoom-out.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _camera;
        [SerializeField] private Transform _target;
        [SerializeField] private CinemachineImpulseSource _impulse;

        [Tooltip("Target height above the climber's feet (above the chest: the climber sits slightly below the screen centre, like the reference).")]
        [SerializeField] private float _targetHeightOffset = 4.2f;

        [SerializeField, Range(10f, 90f)] private float _fieldOfView = 28f;
        [SerializeField, Range(10f, 90f)] private float _zoomedOutFieldOfView = 38f;
        [SerializeField, Min(0.01f)] private float _zoomDuration = 0.6f;

        private Tween _zoom;

        /// <summary>Moves the follow target to the climber's height (camera catches up with damping).</summary>
        public void Follow(float height) => _target.position = new Vector3(0f, height + _targetHeightOffset, 0f);

        /// <summary>Places the camera on the climber immediately (level start).</summary>
        public void SnapTo(float height)
        {
            Vector3 previous = _target.position;
            Follow(height);
            _camera.OnTargetObjectWarped(_target, _target.position - previous);
            _camera.PreviousStateIsValid = false;
            SetFieldOfView(_fieldOfView);
        }

        public void Shake(float force) => _impulse.GenerateImpulseWithForce(force);

        public void SetZoomedOut(bool zoomedOut)
        {
            _zoom.Stop();
            float target = zoomedOut ? _zoomedOutFieldOfView : _fieldOfView;
            _zoom = Tween.Custom(this, _camera.Lens.FieldOfView, target, _zoomDuration,
                (rig, fov) => rig.SetFieldOfView(fov), Ease.InOutSine);
        }

        private void SetFieldOfView(float fieldOfView)
        {
            LensSettings lens = _camera.Lens;
            lens.FieldOfView = fieldOfView;
            _camera.Lens = lens;
        }

        private void OnDestroy() => _zoom.Stop();
    }
}
