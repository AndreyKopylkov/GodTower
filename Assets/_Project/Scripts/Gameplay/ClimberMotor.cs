using System;
using UnityEngine;

namespace GodTower.Gameplay
{
    public enum ClimberState
    {
        /// <summary>Hanging in place (screen released).</summary>
        Idle,
        Climb,
        /// <summary>Hopping to a neighbouring lane; keeps climbing while the screen is held.</summary>
        Shift,
        /// <summary>Hit reaction; followed by <see cref="Fall"/>.</summary>
        Hit,
        Fall,
        /// <summary>Lifted by a hero event (jetpack, phoenix).</summary>
        Carried,
        Win,
        Lose
    }

    /// <summary>Timing of a knockdown: how long the hit reaction lasts and the shortest fall that follows it.</summary>
    public readonly struct HitReaction
    {
        public readonly float HitDuration;
        public readonly float MinFallDuration;

        public HitReaction(float hitDuration, float minFallDuration)
        {
            HitDuration = Mathf.Max(0f, hitDuration);
            MinFallDuration = Mathf.Max(0f, minFallDuration);
        }
    }

    /// <summary>
    /// Climber state machine and kinematics in tower space (height in meters, lane angle in degrees).
    /// Pure logic driven by <see cref="Tick"/>; the view only reads it.
    /// </summary>
    public sealed class ClimberMotor
    {
        private readonly ClimberSettings _settings;

        private float _stateTime;
        private float _shiftFromAngle;
        private float _pendingKnockdown;
        private HitReaction _reaction;
        private float _moveFrom;
        private float _moveTo;
        private float _moveDuration;
        private bool _reachedTopRaised;

        public ClimberMotor(ClimberSettings settings, float topHeight, float startHeight = 0f)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (topHeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(topHeight));

            TopHeight = topHeight;
            Height = Mathf.Clamp(startHeight, 0f, topHeight);
            Lanes = new LaneModel(settings.LaneAngles, settings.LaneAngles.Length / 2);
            LaneAngle = Lanes.Angle;
        }

        public ClimberState State { get; private set; } = ClimberState.Idle;

        public float Height { get; private set; }

        public float TopHeight { get; }

        public LaneModel Lanes { get; }

        /// <summary>Current (interpolated during a shift) angle around the column, degrees.</summary>
        public float LaneAngle { get; private set; }

        /// <summary>0..1..0 arc of the current lane hop (0 when not shifting).</summary>
        public float ShiftHop { get; private set; }

        /// <summary>Direction of the last shift: -1 left, +1 right.</summary>
        public int ShiftDirection { get; private set; }

        /// <summary>Hold and swipe are applied (not reacting to a hit, not carried, not finished).</summary>
        public bool AcceptsInput => State is ClimberState.Idle or ClimberState.Climb or ClimberState.Shift;

        /// <summary>A villain landing in the climber's lane knocks it down.</summary>
        public bool IsVulnerable => AcceptsInput;

        public bool IsFinished => State is ClimberState.Win or ClimberState.Lose;

        public bool IsAtTop => Height >= TopHeight;

        public event Action<ClimberState> StateChanged;

        /// <summary>Raised once, the first time the climber reaches the top of the tower.</summary>
        public event Action ReachedTop;

        public void Tick(float deltaTime, bool holding)
        {
            if (deltaTime <= 0f || IsFinished)
                return;

            _stateTime += deltaTime;
            switch (State)
            {
                case ClimberState.Idle:
                case ClimberState.Climb:
                    TickClimb(deltaTime, holding);
                    SetState(holding && !IsAtTop ? ClimberState.Climb : ClimberState.Idle);
                    break;
                case ClimberState.Shift:
                    TickClimb(deltaTime, holding);
                    TickShift(holding);
                    break;
                case ClimberState.Hit:
                    if (_stateTime >= _reaction.HitDuration)
                        StartFall();
                    break;
                case ClimberState.Fall:
                case ClimberState.Carried:
                    TickMove();
                    break;
            }

            CheckReachedTop();
        }

        /// <summary>Starts a hop to the neighbouring lane (-1 left, +1 right).</summary>
        /// <returns><c>false</c> if input is locked, a hop is in progress or the climber is at that edge.</returns>
        public bool RequestShift(int direction)
        {
            if (State is not (ClimberState.Idle or ClimberState.Climb))
                return false;

            float fromAngle = LaneAngle;
            if (!Lanes.TryShift(direction))
                return false;

            _shiftFromAngle = fromAngle;
            ShiftDirection = Math.Sign(direction);
            SetState(ClimberState.Shift);
            return true;
        }

        /// <summary>Hit reaction followed by a fall of <paramref name="meters"/> (floored at the bottom).</summary>
        public void Knockdown(float meters) =>
            Knockdown(meters, new HitReaction(_settings.HitDuration, _settings.MinFallDuration));

        /// <summary>
        /// Knockdown with custom timing. A knockdown during a hit or a fall restarts the hit reaction and adds up:
        /// the distance still to fall is kept, so stacked hits never cancel each other.
        /// </summary>
        public void Knockdown(float meters, HitReaction reaction)
        {
            if (IsFinished)
                return;

            float remaining = State switch
            {
                ClimberState.Hit => _pendingKnockdown,
                ClimberState.Fall => Mathf.Max(0f, Height - _moveTo),
                _ => 0f
            };

            CompleteShift();
            _pendingKnockdown = remaining + Mathf.Max(0f, meters);
            _reaction = reaction;
            SetState(ClimberState.Hit);
            _stateTime = 0f;
        }

        /// <summary>Lifts the climber by <paramref name="meters"/> over <paramref name="duration"/> seconds (capped at the top).</summary>
        public void Carry(float meters, float duration)
        {
            if (IsFinished)
                return;

            CompleteShift();
            BeginMove(Mathf.Min(TopHeight, Height + Mathf.Max(0f, meters)), Mathf.Max(0.01f, duration));
            SetState(ClimberState.Carried);
        }

        public void Win() => Finish(ClimberState.Win);

        public void Lose() => Finish(ClimberState.Lose);

        private void TickClimb(float deltaTime, bool holding)
        {
            if (holding)
                Height = Mathf.Min(TopHeight, Height + _settings.ClimbSpeed * deltaTime);
        }

        private void TickShift(bool holding)
        {
            float t = Mathf.Clamp01(_stateTime / _settings.ShiftDuration);
            LaneAngle = Mathf.Lerp(_shiftFromAngle, Lanes.Angle, Mathf.SmoothStep(0f, 1f, t));
            ShiftHop = Mathf.Sin(t * Mathf.PI);

            if (t >= 1f)
            {
                CompleteShift();
                SetState(holding && !IsAtTop ? ClimberState.Climb : ClimberState.Idle);
            }
        }

        private void CompleteShift()
        {
            LaneAngle = Lanes.Angle;
            ShiftHop = 0f;
        }

        private void StartFall()
        {
            float target = KnockdownModel.Apply(Height, _pendingKnockdown);
            float distance = Height - target;
            _pendingKnockdown = 0f;
            BeginMove(target, Mathf.Max(_reaction.MinFallDuration, distance / _settings.FallSpeed));
            SetState(ClimberState.Fall);
        }

        private void BeginMove(float to, float duration)
        {
            _moveFrom = Height;
            _moveTo = to;
            _moveDuration = duration;
        }

        private void TickMove()
        {
            float t = Mathf.Clamp01(_stateTime / _moveDuration);
            float eased = State == ClimberState.Fall ? 1f - (1f - t) * (1f - t) : Mathf.SmoothStep(0f, 1f, t);
            Height = Mathf.Lerp(_moveFrom, _moveTo, eased);

            if (t >= 1f)
                SetState(ClimberState.Idle);
        }

        private void CheckReachedTop()
        {
            if (_reachedTopRaised || !IsAtTop || IsFinished)
                return;

            _reachedTopRaised = true;
            ReachedTop?.Invoke();
        }

        private void Finish(ClimberState state)
        {
            if (IsFinished)
                return;

            CompleteShift();
            SetState(state);
        }

        private void SetState(ClimberState state)
        {
            if (State == state)
                return;

            State = state;
            _stateTime = 0f;
            StateChanged?.Invoke(state);
        }
    }
}
