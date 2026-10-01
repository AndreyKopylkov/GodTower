using System.Collections.Generic;
using GodTower.Gameplay;
using NUnit.Framework;

namespace GodTower.Tests.EditMode
{
    public sealed class ClimberMotorTests
    {
        private const float Dt = 0.02f;

        private static ClimberSettings Settings() =>
            new(climbSpeed: 6f, laneAngles: new[] { -35f, 0f, 35f }, shiftDuration: 0.2f,
                hitDuration: 0.4f, fallSpeed: 30f, minFallDuration: 0.6f);

        private static ClimberMotor Create(float top = 300f, float start = 0f) => new(Settings(), top, start);

        private static void Run(ClimberMotor motor, float seconds, bool holding)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += Dt)
                motor.Tick(Dt, holding);
        }

        [Test]
        public void StartsIdleInCenterLane()
        {
            ClimberMotor motor = Create();

            Assert.That(motor.State, Is.EqualTo(ClimberState.Idle));
            Assert.That(motor.Lanes.Index, Is.EqualTo(1));
            Assert.That(motor.LaneAngle, Is.Zero);
            Assert.That(motor.Height, Is.Zero);
        }

        [Test]
        public void Holding_ClimbsAt6MetersPerSecond_ReleasingHangs()
        {
            ClimberMotor motor = Create();

            Run(motor, 1f, holding: true);
            Assert.That(motor.State, Is.EqualTo(ClimberState.Climb));
            Assert.That(motor.Height, Is.EqualTo(6f).Within(0.01f));

            Run(motor, 1f, holding: false);
            Assert.That(motor.State, Is.EqualTo(ClimberState.Idle));
            Assert.That(motor.Height, Is.EqualTo(6f).Within(0.01f));
        }

        [Test]
        public void Shift_MovesToNeighbourLane_OverShiftDuration_WhileClimbing()
        {
            ClimberMotor motor = Create();
            motor.Tick(Dt, true);

            Assert.That(motor.RequestShift(1), Is.True);
            Assert.That(motor.State, Is.EqualTo(ClimberState.Shift));
            Assert.That(motor.ShiftDirection, Is.EqualTo(1));

            motor.Tick(0.1f, true);
            Assert.That(motor.LaneAngle, Is.GreaterThan(0f).And.LessThan(35f));
            Assert.That(motor.ShiftHop, Is.GreaterThan(0.5f));

            motor.Tick(0.11f, true);
            Assert.That(motor.LaneAngle, Is.EqualTo(35f));
            Assert.That(motor.ShiftHop, Is.Zero);
            Assert.That(motor.State, Is.EqualTo(ClimberState.Climb));
            Assert.That(motor.Height, Is.EqualTo(6f * (Dt + 0.21f)).Within(0.01f), "Climbing continues during a hop.");
        }

        [Test]
        public void Shift_IsRejected_AtEdge_DuringHop_AndWhileHit()
        {
            ClimberMotor motor = Create();
            Assert.That(motor.RequestShift(-1), Is.True);
            Assert.That(motor.RequestShift(-1), Is.False, "A hop is in progress.");

            Run(motor, 0.3f, holding: false);
            Assert.That(motor.RequestShift(-1), Is.False, "Already in the left lane.");

            motor.Knockdown(10f);
            Assert.That(motor.RequestShift(1), Is.False);
            Assert.That(motor.AcceptsInput, Is.False);
        }

        [Test]
        public void Knockdown_PlaysHitThenFallsTheDistance_ThenHangs()
        {
            ClimberMotor motor = Create(start: 100f);
            var states = new List<ClimberState>();
            motor.StateChanged += states.Add;

            motor.Knockdown(24f);
            Assert.That(motor.State, Is.EqualTo(ClimberState.Hit));
            Assert.That(motor.IsVulnerable, Is.False);

            Run(motor, 0.42f, holding: true);
            Assert.That(motor.State, Is.EqualTo(ClimberState.Fall));
            Assert.That(motor.Height, Is.LessThanOrEqualTo(100f).And.GreaterThan(95f), "Holding during the hit reaction does not climb.");

            Run(motor, 1f, holding: false);
            Assert.That(motor.Height, Is.EqualTo(76f).Within(0.01f));
            Assert.That(states, Is.EqualTo(new[] { ClimberState.Hit, ClimberState.Fall, ClimberState.Idle }));
        }

        [Test]
        public void Knockdown_IsFlooredAtBottom()
        {
            ClimberMotor motor = Create(start: 5f);

            motor.Knockdown(24f);
            Run(motor, 2f, holding: false);

            Assert.That(motor.Height, Is.Zero);
            Assert.That(motor.State, Is.EqualTo(ClimberState.Idle));
        }

        [Test]
        public void Knockdown_DuringShift_SnapsToTargetLane()
        {
            ClimberMotor motor = Create();
            motor.RequestShift(1);
            motor.Tick(0.05f, false);

            motor.Knockdown(1f);

            Assert.That(motor.LaneAngle, Is.EqualTo(35f));
            Assert.That(motor.Lanes.Index, Is.EqualTo(2));
        }

        [Test]
        public void Carry_LiftsOverDuration_ThenHangs()
        {
            ClimberMotor motor = Create(start: 50f);

            motor.Carry(30f, 2f);
            Assert.That(motor.State, Is.EqualTo(ClimberState.Carried));
            Assert.That(motor.AcceptsInput, Is.False);

            Run(motor, 1f, holding: false);
            Assert.That(motor.Height, Is.GreaterThan(55f).And.LessThan(75f));

            Run(motor, 1.02f, holding: false);
            Assert.That(motor.Height, Is.EqualTo(80f).Within(0.01f));
            Assert.That(motor.State, Is.EqualTo(ClimberState.Idle));
        }

        [Test]
        public void Carry_CancelsFall()
        {
            ClimberMotor motor = Create(start: 100f);
            motor.Knockdown(24f);
            Run(motor, 0.5f, holding: false);

            float height = motor.Height;
            motor.Carry(30f, 2f);
            Run(motor, 2.02f, holding: false);

            Assert.That(motor.Height, Is.EqualTo(height + 30f).Within(0.01f));
        }

        [Test]
        public void ReachingTop_RaisesReachedTopOnce_AndStopsClimbing()
        {
            ClimberMotor motor = Create(top: 10f);
            int reached = 0;
            motor.ReachedTop += () => reached++;

            Run(motor, 3f, holding: true);

            Assert.That(motor.Height, Is.EqualTo(10f));
            Assert.That(reached, Is.EqualTo(1));
        }

        [Test]
        public void Carry_IsCappedAtTop_AndRaisesReachedTop()
        {
            ClimberMotor motor = Create(top: 100f, start: 90f);
            bool reached = false;
            motor.ReachedTop += () => reached = true;

            motor.Carry(30f, 1f);
            Run(motor, 1.02f, holding: false);

            Assert.That(motor.Height, Is.EqualTo(100f));
            Assert.That(reached, Is.True);
        }

        [Test]
        public void Knockdown_WithCustomReaction_UsesItsHitAndMinimumFallDurations()
        {
            ClimberMotor motor = Create(start: 100f);

            motor.Knockdown(9f, new HitReaction(hitDuration: 0.25f, minFallDuration: 0.25f));
            Run(motor, 0.24f, holding: false);
            Assert.That(motor.State, Is.EqualTo(ClimberState.Hit));

            Run(motor, 0.04f, holding: false);
            Assert.That(motor.State, Is.EqualTo(ClimberState.Fall));

            // 9 m at 30 m/s = 0.3 s (longer than the 0.25 s minimum).
            Run(motor, 0.32f, holding: false);
            Assert.That(motor.State, Is.EqualTo(ClimberState.Idle));
            Assert.That(motor.Height, Is.EqualTo(91f).Within(0.01f));
        }

        [Test]
        public void Knockdown_DuringHit_AddsUp()
        {
            ClimberMotor motor = Create(start: 100f);

            motor.Knockdown(10f);
            Run(motor, 0.2f, holding: false);
            motor.Knockdown(5f);
            Run(motor, 0.38f, holding: false);
            Assert.That(motor.State, Is.EqualTo(ClimberState.Hit), "A new hit restarts the reaction.");

            Run(motor, 2f, holding: false);
            Assert.That(motor.Height, Is.EqualTo(85f).Within(0.01f));
        }

        [Test]
        public void Knockdown_DuringFall_KeepsTheRemainingDistance()
        {
            ClimberMotor motor = Create(start: 100f);

            motor.Knockdown(24f);
            Run(motor, 0.6f, holding: false);
            Assert.That(motor.State, Is.EqualTo(ClimberState.Fall));
            Assert.That(motor.Height, Is.LessThan(100f).And.GreaterThan(76f));

            motor.Knockdown(6f);
            Assert.That(motor.State, Is.EqualTo(ClimberState.Hit));
            Run(motor, 3f, holding: false);
            Assert.That(motor.Height, Is.EqualTo(70f).Within(0.01f));
            Assert.That(motor.State, Is.EqualTo(ClimberState.Idle));
        }

        [Test]
        public void WinAndLose_AreTerminal()
        {
            ClimberMotor motor = Create();
            motor.Lose();

            motor.Tick(1f, true);
            motor.Knockdown(10f);
            motor.Carry(10f, 1f);
            motor.Win();

            Assert.That(motor.State, Is.EqualTo(ClimberState.Lose));
            Assert.That(motor.Height, Is.Zero);
            Assert.That(motor.IsFinished, Is.True);
        }
    }
}
