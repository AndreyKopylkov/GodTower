using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GodTower.Audio;
using GodTower.Gameplay;
using PrimeTween;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace GodTower.Effects
{
    /// <summary>
    /// Presentation of the webhook bump: a wave of pooled 3D boxing gloves flies in from every screen edge along bent arcs,
    /// twists into the punch and hits the climber (who keeps being tracked while falling). Every contact bursts stars,
    /// the first one of a wave also flashes the screen, shakes the camera hard, pops a comic "POW" and reports the hit
    /// back so gameplay can knock the climber down. Gloves move on scaled time: they freeze while the game is paused.
    /// </summary>
    public sealed class BumpStage : MonoBehaviour
    {
        private const float EffectLifetime = 2.5f;

        [Tooltip("Fallback material for the cube placeholder when the glove model is missing.")]
        [SerializeField] private Material _placeholderMaterial;

        private readonly Stack<Transform> _pool = new();
        private readonly List<Transform> _all = new();
        private BumpEffectConfig _config;
        private ClimberView _climber;
        private CameraRig _cameraRig;
        private ScreenFlash _flash;
        private IAudioService _audio;
        private Camera _camera;
        private int _nextEdge;

        [Inject]
        public void Construct(BumpEffectConfig config, ClimberView climber, CameraRig cameraRig, ScreenFlash flash, IAudioService audio)
        {
            _config = config;
            _climber = climber;
            _cameraRig = cameraRig;
            _flash = flash;
            _audio = audio;
        }

        /// <summary>Gloves currently flying (diagnostics and tests).</summary>
        public int GlovesInFlight => _all.Count - _pool.Count;

        private void Start()
        {
            _camera = Camera.main;
            int prewarm = _config.MaxGloves * _config.MaxConcurrentWaves;
            for (int i = 0; i < prewarm; i++)
                _pool.Push(CreateGlove());
        }

        /// <summary>
        /// Plays one wave. <paramref name="onFirstContact"/> runs when the first glove lands. Completes when every glove
        /// is back in the pool.
        /// </summary>
        public async UniTask PlayWaveAsync(Action onFirstContact, CancellationToken token)
        {
            int count = Random.Range(_config.MinGloves, _config.MaxGloves + 1);
            var wave = new WaveState(onFirstContact, _config.PunchSoundsPerWave);

            // Spread the gloves over all four edges, starting from a rotating edge so consecutive waves differ.
            int firstEdge = _nextEdge++;
            var gloves = new UniTask[count];
            for (int i = 0; i < count; i++)
                gloves[i] = FlyGloveAsync(i * _config.Stagger, (firstEdge + i) % 4, mirrored: i % 2 == 1, wave, token);

            await UniTask.WhenAll(gloves);
        }

        private async UniTask FlyGloveAsync(float delay, int edge, bool mirrored, WaveState wave, CancellationToken token)
        {
            if (delay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delay), DelayType.DeltaTime, PlayerLoopTiming.Update, token);

            Transform glove = Rent();
            try
            {
                Vector3 contactOffset = RandomContactOffset();
                GloveArc arc = PlanArc(edge, _climber.Center + contactOffset);
                float reach = _config.GloveReach;
                float scale = _config.GloveScale;

                glove.localScale = new Vector3(mirrored ? -scale : scale, scale, scale);
                Place(glove, arc, contactOffset, reach, 0f);
                glove.gameObject.SetActive(true);

                float duration = Random.Range(_config.FlightDuration.x, _config.FlightDuration.y);
                await Tween.Custom(glove, 0f, 1f, duration, (target, t) => Place(target, arc, contactOffset, reach, t), Ease.InSine);
                token.ThrowIfCancellationRequested();

                Contact(wave, _climber.Center + contactOffset, arc.PunchDirection);

                // Bounce back off the climber (still tracking it while it falls) and shrink away.
                Vector3 back = -arc.PunchDirection * _config.RecoilDistance + Vector3.up * 0.3f;
                float recoil = _config.RecoilDuration;
                _ = Tween.Custom(glove, 0f, 1f, recoil, (target, t) =>
                    target.position = _climber.Center + contactOffset - arc.PunchDirection * reach + back * t, Ease.OutQuad);
                await Tween.Scale(glove, Vector3.zero, recoil * 0.6f, Ease.InBack, startDelay: recoil * 0.4f);
                token.ThrowIfCancellationRequested();
            }
            finally
            {
                Return(glove);
            }
        }

        private void Place(Transform glove, GloveArc arc, Vector3 contactOffset, float reach, float t)
        {
            Vector3 contact = _climber.Center + contactOffset;
            glove.SetPositionAndRotation(contact - arc.PunchDirection * reach + arc.Offset(t), arc.Rotation(t));
        }

        private void Contact(WaveState wave, Vector3 point, Vector3 punchDirection)
        {
            SpawnEffect(_config.HitEffect, point - punchDirection * 0.25f, _config.HitEffectScale, _climber.transform);
            if (wave.TakeSound())
                _audio.Play(SoundId.Punch);

            if (!wave.TakeFirstContact())
            {
                _cameraRig.Shake(_config.ContactShake);
                return;
            }

            _flash.Flash(_config.FlashColor, _config.FlashAlpha, _config.FlashDuration);
            _cameraRig.Shake(_config.FirstContactShake);
            Vector3 towardsCamera = _camera != null ? -_camera.transform.forward : Vector3.back;
            float scale = _climber.Scale;
            SpawnEffect(_config.ComicEffect, _climber.Center + (towardsCamera * 1.5f + Vector3.up * 1.8f) * scale, _config.ComicEffectScale, _climber.transform);
            wave.OnFirstContact?.Invoke();
        }

        /// <summary>Arc from a point just outside screen edge <paramref name="edge"/> (0 left, 1 top, 2 right, 3 bottom), in front of the climber.</summary>
        private GloveArc PlanArc(int edge, Vector3 contact)
        {
            Vector3 start;
            if (_camera != null)
            {
                Transform view = _camera.transform;
                float distance = Vector3.Dot(contact - view.position, view.forward);
                Vector2 towards = _config.SpawnDepthTowardsCamera;
                float depth = Mathf.Max(_camera.nearClipPlane + 2f, distance - Random.Range(towards.x, towards.y));
                Vector2 viewport = EdgePoint(edge, Random.Range(0.12f, 0.88f), _config.EdgeMargin);
                start = _camera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, depth));
            }
            else
            {
                start = contact + Quaternion.Euler(0f, 0f, edge * 90f) * Vector3.left * 8f + Vector3.back * 6f;
            }

            Vector3 offset = start - contact;
            Vector3 forward = _camera != null ? _camera.transform.forward : Vector3.forward;
            Vector3 bendAxis = Vector3.Cross(offset, forward).normalized * (Random.value < 0.5f ? -1f : 1f);
            float roll = Random.Range(_config.PunchRoll.x, _config.PunchRoll.y) * (Random.value < 0.5f ? -1f : 1f);
            return new GloveArc(offset, bendAxis * Random.Range(_config.ArcBend.x, _config.ArcBend.y), roll);
        }

        private static Vector2 EdgePoint(int edge, float along, float margin) => edge switch
        {
            0 => new Vector2(-margin, along),
            1 => new Vector2(along, 1f + margin),
            2 => new Vector2(1f + margin, along),
            _ => new Vector2(along, -margin)
        };

        private Vector3 RandomContactOffset()
        {
            Vector3 spread = _config.ContactSpread;
            Vector3 towardsCamera = _camera != null ? -_camera.transform.forward : Vector3.back;
            return new Vector3(Random.Range(-spread.x, spread.x), Random.Range(-spread.y, spread.y), 0f)
                   + towardsCamera * (0.35f * _climber.Scale + Random.Range(0f, spread.z));
        }

        /// <summary>Spawns a self-destroying burst attached to <paramref name="parent"/> (bursts follow the falling climber).</summary>
        private void SpawnEffect(GameObject effect, Vector3 position, float scale, Transform parent)
        {
            if (effect == null)
                return;

            GameObject instance = Instantiate(effect, position, Quaternion.identity, parent);
            instance.transform.localScale *= scale;
            Destroy(instance, EffectLifetime);
        }

        private Transform Rent() => _pool.Count > 0 ? _pool.Pop() : CreateGlove();

        private void Return(Transform glove)
        {
            if (glove == null)
                return;

            Tween.StopAll(glove);
            glove.gameObject.SetActive(false);
            _pool.Push(glove);
        }

        private Transform CreateGlove()
        {
            Transform glove;
            if (_config.GlovePrefab != null)
            {
                glove = Instantiate(_config.GlovePrefab, transform).transform;
            }
            else
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(cube.GetComponent<Collider>());
                cube.GetComponent<MeshRenderer>().sharedMaterial = _placeholderMaterial;
                glove = cube.transform;
                glove.SetParent(transform, false);
            }

            glove.name = $"Glove_{_all.Count:00}";
            glove.gameObject.SetActive(false);
            _all.Add(glove);
            return glove;
        }

        private void OnDestroy()
        {
            foreach (Transform glove in _all)
                if (glove != null)
                    Tween.StopAll(glove);
        }

        /// <summary>Shared state of the gloves of one wave.</summary>
        private sealed class WaveState
        {
            public readonly Action OnFirstContact;
            private int _soundsLeft;
            private bool _contacted;

            public WaveState(Action onFirstContact, int sounds)
            {
                OnFirstContact = onFirstContact;
                _soundsLeft = sounds;
            }

            public bool TakeFirstContact()
            {
                if (_contacted)
                    return false;

                _contacted = true;
                return true;
            }

            public bool TakeSound() => _soundsLeft-- > 0;
        }
    }
}
