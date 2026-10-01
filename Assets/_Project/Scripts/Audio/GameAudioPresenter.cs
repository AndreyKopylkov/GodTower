using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GodTower.Events;
using GodTower.Gameplay;
using GodTower.Levels;
using VContainer.Unity;

namespace GodTower.Audio
{
    /// <summary>
    /// Level sound effects driven by gameplay events: telegraph beeps and villain sounds, hero boosts (looping jetpack),
    /// climbing steps, lane hops and falls. Bump punches, UI, win/lose and countdown sounds are played by their presenters.
    /// </summary>
    public sealed class GameAudioPresenter : IStartable, ITickable, IDisposable
    {
        private const float StepInterval = 0.3f;

        private readonly LevelRunner _runner;
        private readonly EventDirector _events;
        private readonly IAudioService _audio;
        private readonly CancellationTokenSource _lifetime = new();
        private ClimberMotor _climber;
        private IDisposable _jetpackLoop;
        private float _stepTimer;

        public GameAudioPresenter(LevelRunner runner, EventDirector events, IAudioService audio)
        {
            _runner = runner;
            _events = events;
            _audio = audio;
        }

        public void Start()
        {
            _events.Telegraphed += OnTelegraphed;
            _events.Landed += OnLanded;
            _events.BoostStarted += OnBoostStarted;
        }

        public void Tick()
        {
            if (_climber == null && _runner.Climber != null)
            {
                _climber = _runner.Climber;
                _climber.StateChanged += OnClimberStateChanged;
            }

            if (_climber == null || !_runner.IsRunning || _climber.State != ClimberState.Climb)
            {
                _stepTimer = 0f;
                return;
            }

            _stepTimer -= UnityEngine.Time.deltaTime;
            if (_stepTimer > 0f)
                return;

            _stepTimer = StepInterval;
            _audio.Play(SoundId.ClimbStep);
        }

        public void Dispose()
        {
            _events.Telegraphed -= OnTelegraphed;
            _events.Landed -= OnLanded;
            _events.BoostStarted -= OnBoostStarted;
            if (_climber != null)
                _climber.StateChanged -= OnClimberStateChanged;
            _lifetime.Cancel();
            _lifetime.Dispose();
            _jetpackLoop?.Dispose();
        }

        private void OnTelegraphed(VillainStrike strike)
        {
            _audio.Play(SoundId.WarningBeep);
            if (strike.Kind == VillainKind.Missile)
                _audio.Play(SoundId.MissileFlyby);
        }

        private void OnLanded(VillainStrike strike, bool hit)
        {
            switch (strike.Kind)
            {
                case VillainKind.Truck:
                    _audio.Play(SoundId.TruckCrash);
                    break;
                case VillainKind.Axes:
                    _audio.Play(SoundId.AxeWhoosh);
                    break;
                default:
                    if (hit)
                        _audio.Play(SoundId.Explosion);
                    break;
            }
        }

        private void OnBoostStarted(HeroBoost boost)
        {
            if (boost.Kind == HeroKind.Phoenix)
            {
                _audio.Play(SoundId.Phoenix);
                return;
            }

            PlayJetpackAsync(boost.Duration, _lifetime.Token).Forget();
        }

        private async UniTaskVoid PlayJetpackAsync(float duration, CancellationToken token)
        {
            _jetpackLoop?.Dispose();
            IDisposable loop = _audio.StartLoop(SoundId.Jetpack);
            _jetpackLoop = loop;
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token); // scaled: holds during pause
            }
            finally
            {
                loop.Dispose();
                if (_jetpackLoop == loop)
                    _jetpackLoop = null;
            }
        }

        private void OnClimberStateChanged(ClimberState state)
        {
            switch (state)
            {
                case ClimberState.Shift:
                    _audio.Play(SoundId.LaneShift);
                    break;
                case ClimberState.Fall:
                    _audio.Play(SoundId.Fall);
                    break;
            }
        }
    }
}
