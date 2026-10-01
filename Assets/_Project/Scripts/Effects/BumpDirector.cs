using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GodTower.Gameplay;
using GodTower.Levels;
using GodTower.Webhook;
using UnityEngine;
using VContainer.Unity;

namespace GodTower.Effects
{
    /// <summary>
    /// Turns accepted webhook bumps into glove waves (Docs/Plan.md §2): requests are layered through a <see cref="BumpQueue"/>
    /// (max concurrent waves, bounded queue), new waves only start while the level runs (never while paused or after the end),
    /// and the first glove contact of every wave knocks the climber down by the capped bump distance.
    /// A wave that fails or hangs is ended by a watchdog, so the queue can never soft-lock.
    /// </summary>
    public sealed class BumpDirector : IStartable, ITickable, IDisposable
    {
        private readonly IBumpSignal _signal;
        private readonly LevelRunner _runner;
        private readonly ClimberView _climberView;
        private readonly BumpStage _stage;
        private readonly BumpEffectConfig _config;
        private readonly CancellationTokenSource _lifetime = new();

        public BumpDirector(IBumpSignal signal, LevelRunner runner, ClimberView climberView, BumpStage stage, BumpEffectConfig config)
        {
            _signal = signal;
            _runner = runner;
            _climberView = climberView;
            _stage = stage;
            _config = config;
            Queue = new BumpQueue(config.MaxConcurrentWaves, config.MaxQueuedWaves);
        }

        public BumpQueue Queue { get; }

        /// <summary>Waves started so far.</summary>
        public int WavesStarted { get; private set; }

        /// <summary>A wave hit the climber; the argument is the knockdown in meters (0 when capped or immune).</summary>
        public event Action<float> Hit;

        public void Start()
        {
            _signal.BumpRequested += OnBumpRequested;
            _runner.Ended += OnLevelEnded;
        }

        public void Tick()
        {
            if (!_runner.IsRunning)
                return;

            while (Queue.TryBegin())
                RunWaveAsync(_lifetime.Token).Forget();
        }

        public void Dispose()
        {
            _signal.BumpRequested -= OnBumpRequested;
            _runner.Ended -= OnLevelEnded;
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        private void OnBumpRequested()
        {
            if (_runner.Outcome == null)
                Queue.Enqueue();
        }

        private void OnLevelEnded(LevelOutcome _) => Queue.ClearPending();

        private async UniTaskVoid RunWaveAsync(CancellationToken token)
        {
            WavesStarted++;
            using var watchdogCancel = CancellationTokenSource.CreateLinkedTokenSource(token);
            try
            {
                // Watchdog on scaled time (a paused wave is not "hung").
                var watchdog = TimeSpan.FromSeconds(_config.MaxWaveDuration * 2f + 1f);
                await UniTask.WhenAny(
                    _stage.PlayWaveAsync(OnFirstContact, token),
                    UniTask.Delay(watchdog, DelayType.DeltaTime, PlayerLoopTiming.Update, watchdogCancel.Token));
            }
            catch (OperationCanceledException)
            {
                // Scene unloaded mid-wave.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                watchdogCancel.Cancel();
                Queue.End();
            }
        }

        private void OnFirstContact()
        {
            ClimberMotor climber = _runner.Climber;
            if (climber == null || climber.IsFinished)
                return;

            // A hero boost makes the climber immune (as for villains); the cap may also leave nothing to take.
            float meters = climber.State == ClimberState.Carried ? 0f : _runner.Knockdown.TakeBumpKnockdown(_runner.Elapsed);
            if (meters > 0f)
                climber.Knockdown(meters, _config.Reaction);
            else
                _climberView.PlayFlinch(_config.FlinchDuration);

            Hit?.Invoke(meters);
        }
    }
}
