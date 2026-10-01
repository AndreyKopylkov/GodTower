using GodTower.Gameplay;
using NUnit.Framework;

namespace GodTower.Tests.EditMode
{
    public sealed class LaneModelTests
    {
        private static LaneModel CreateCentered() => new(new[] { -35f, 0f, 35f }, 1);

        [Test]
        public void StartsInGivenLane()
        {
            LaneModel lanes = CreateCentered();

            Assert.That(lanes.Count, Is.EqualTo(3));
            Assert.That(lanes.Index, Is.EqualTo(1));
            Assert.That(lanes.CenterIndex, Is.EqualTo(1));
            Assert.That(lanes.Angle, Is.EqualTo(0f));
        }

        [Test]
        public void ShiftsLeftAndRight()
        {
            LaneModel lanes = CreateCentered();

            Assert.That(lanes.TryShift(-1), Is.True);
            Assert.That(lanes.Angle, Is.EqualTo(-35f));
            Assert.That(lanes.TryShift(1), Is.True);
            Assert.That(lanes.TryShift(1), Is.True);
            Assert.That(lanes.Index, Is.EqualTo(2));
            Assert.That(lanes.Angle, Is.EqualTo(35f));
        }

        [Test]
        public void ClampsAtEdges()
        {
            LaneModel lanes = CreateCentered();
            lanes.TryShift(1);

            Assert.That(lanes.TryShift(1), Is.False);
            Assert.That(lanes.Index, Is.EqualTo(2));

            lanes.TryShift(-1);
            lanes.TryShift(-1);
            Assert.That(lanes.TryShift(-1), Is.False);
            Assert.That(lanes.Index, Is.Zero);
        }

        [Test]
        public void ZeroDirection_DoesNothing()
        {
            LaneModel lanes = CreateCentered();

            Assert.That(lanes.TryShift(0), Is.False);
            Assert.That(lanes.Index, Is.EqualTo(1));
        }

        [Test]
        public void LargeDirection_MovesOneLane()
        {
            LaneModel lanes = CreateCentered();

            Assert.That(lanes.TryShift(5), Is.True);
            Assert.That(lanes.Index, Is.EqualTo(2));
        }
    }
}
