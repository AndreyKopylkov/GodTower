using GodTower.Gameplay;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace GodTower.Effects
{
    /// <summary>Win celebration on the top deck: the 3D trophy pops up beside the climber and spins, with a burst.</summary>
    public sealed class WinStage : MonoBehaviour
    {
        [Tooltip("Trophy model (pivot at the bottom, front +Z); skipped when empty.")]
        [SerializeField] private GameObject _trophy;
        [SerializeField, Min(0.01f)] private float _trophyScale = 1.8f;
        [Tooltip("Offset from the climber on the deck (x = screen right), in climber units (multiplied by its display scale).")]
        [SerializeField] private Vector3 _trophyOffset = new(1.4f, 0f, -0.6f);
        [SerializeField] private GameObject _burstEffect;
        [SerializeField, Min(0.01f)] private float _burstScale = 1.5f;

        private ClimberView _climber;
        private Transform _spawned;

        [Inject]
        public void Construct(ClimberView climber) => _climber = climber;

        public void Celebrate()
        {
            if (_trophy == null || _spawned != null)
                return;

            float scale = _climber.Scale;
            Vector3 position = _climber.transform.position + _trophyOffset * scale;
            _spawned = Instantiate(_trophy, position, Quaternion.Euler(0f, 180f, 0f), transform).transform; // front faces the camera
            _spawned.localScale = Vector3.zero;
            Tween.Scale(_spawned, _trophyScale * scale, 0.6f, Ease.OutBack);
            Tween.LocalPositionY(_spawned, position.y + 0.4f * scale, 1.2f, Ease.InOutSine, -1, CycleMode.Yoyo);
            Tween.Rotation(_spawned, Quaternion.Euler(0f, 180f + 25f, 0f), 1.6f, Ease.InOutSine, -1, CycleMode.Yoyo);

            if (_burstEffect != null)
            {
                GameObject burst = Instantiate(_burstEffect, position + Vector3.up * scale, Quaternion.identity, transform);
                burst.transform.localScale *= _burstScale * scale;
                Destroy(burst, 4f);
            }
        }

        private void OnDestroy()
        {
            if (_spawned != null)
                Tween.StopAll(_spawned);
        }
    }
}
