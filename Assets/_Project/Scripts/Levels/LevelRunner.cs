using System;
using GodTower.Core;
using GodTower.Gameplay;
using UnityEngine;
using VContainer.Unity;

namespace GodTower.Levels
{
    public enum LevelOutcome
    {
        Won,
        Lost
    }

    /// <summary>
    /// Runs one level: builds the tower, feeds input to the climber, counts the time limit down,
    /// decides win (top reached) / lose (time out), handles pause and drives the session <see cref="PlayState"/>
    /// (Playing / Paused / Result; Menu again when the Game scene is unloaded).
    /// </summary>
    public sealed class LevelRunner : IStartable, ITickable, IDisposable
    {
        private readonly GameplayConfig _gameplay;
        private readonly PlayStateService _playState;
        private readonly IClimbInput _input;
        private readonly ClimberView _climberView;
        private readonly TowerBuilder _towerBuilder;
        private readonly CameraRig _cameraRig;

        private Vector3 _deckPoint;

        public LevelRunner(
            LevelConfig level,
            GameplayConfig gameplay,
            PlayStateService playState,
            IClimbInput input,
            ClimberView climberView,
            TowerBuilder towerBuilder,
            CameraRig cameraRig)
        {
            Level = level;
            _gameplay = gameplay;
            _playState = playState;
            _input = input;
            _climberView = climberView;
            _towerBuilder = towerBuilder;
            _cameraRig = cameraRig;
        }

        public LevelConfig Level { get; }

        /// <summary>Created in <see cref="Start"/>.</summary>
        public ClimberMotor Climber { get; private set; }

        public KnockdownModel Knockdown { get; private set; }

        /// <summary>Level time since start in seconds (stops while paused and after the end).</summary>
        public float Elapsed { get; private set; }

        public float TimeLeft { get; private set; }

        public bool IsPaused { get; private set; }

        public LevelOutcome? Outcome { get; private set; }

        /// <summary>The level is in progress and not paused: gameplay systems should advance.</summary>
        public bool IsRunning => Climber != null && Outcome == null && !IsPaused;

        public event Action<LevelOutcome> Ended;

        public void Start()
        {
            TowerLayout layout = _towerBuilder.Build(Level.TowerHeight, Level.Seed);
            _deckPoint = _towerBuilder.DeckPoint(layout);

            Climber = new ClimberMotor(_gameplay.Climber, layout.TopY);
            Climber.ReachedTop += OnReachedTop;
            Knockdown = new KnockdownModel(layout.ClimbHeight, _gameplay.Knockdown);
            TimeLeft = Level.TimeLimit;

            _climberView.Configure(_gameplay.Climber);
            _climberView.Apply(Climber);
            _cameraRig.SnapTo(Climber.Height);

            _playState.Set(PlayState.Playing);
        }

        public void Tick()
        {
            if (Climber == null || IsPaused)
                return;

            if (Outcome != null)
            {
                // Keep framing the climber during the win hop / lose animation.
                _climberView.Apply(Climber);
                _cameraRig.Follow(_climberView.transform.position.y);
                return;
            }

            float deltaTime = Time.deltaTime;
            ClimbInputFrame input = _input.Read(Time.time);
            if (input.Swipe != 0)
                Climber.RequestShift(input.Swipe);

            Climber.Tick(deltaTime, input.IsHolding);
            Elapsed += deltaTime;
            TimeLeft = Mathf.Max(0f, TimeLeft - deltaTime);

            if (Outcome == null && TimeLeft <= 0f)
                Finish(LevelOutcome.Lost);

            _climberView.Apply(Climber);
            _cameraRig.Follow(Climber.Height);
        }

        public void Pause()
        {
            if (!IsRunning)
                return;

            IsPaused = true;
            Time.timeScale = 0f;
            _playState.Set(PlayState.Paused);
        }

        public void Resume()
        {
            if (!IsPaused)
                return;

            IsPaused = false;
            Time.timeScale = 1f;
            _playState.Set(Outcome == null ? PlayState.Playing : PlayState.Result);
        }

        public void Dispose()
        {
            if (Climber != null)
                Climber.ReachedTop -= OnReachedTop;

            if (IsPaused)
                Time.timeScale = 1f;

            // Leaving the Game scene: never answer 200 to /bump during the transition.
            _playState.Set(PlayState.Menu);
        }

        private void OnReachedTop() => Finish(LevelOutcome.Won);

        private void Finish(LevelOutcome outcome)
        {
            if (Outcome != null)
                return;

            Outcome = outcome;
            if (outcome == LevelOutcome.Won)
            {
                Climber.Win();
                _climberView.HopOnto(_deckPoint, _gameplay.WinHopDuration);
            }
            else
            {
                Climber.Lose();
            }

            _playState.Set(PlayState.Result);
            Ended?.Invoke(outcome);
        }
    }
}
