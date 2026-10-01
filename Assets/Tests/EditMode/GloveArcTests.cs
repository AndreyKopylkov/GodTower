using GodTower.Effects;
using NUnit.Framework;
using UnityEngine;

namespace GodTower.Tests.EditMode
{
    public sealed class GloveArcTests
    {
        private static readonly GloveArc Arc = new(new Vector3(-8f, 3f, -6f), new Vector3(0f, 0f, 2f), roll: 180f);

        [Test]
        public void StartsAtTheStartOffset_EndsOnTheTarget()
        {
            AssertVector(Arc.Offset(0f), Arc.StartOffset);
            AssertVector(Arc.Offset(1f), Vector3.zero);
        }

        [Test]
        public void BendIsPerpendicularToTheApproach_AndBulgesMidFlight()
        {
            Assert.That(Vector3.Dot(Arc.Bend, Arc.StartOffset), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(Arc.Bend.magnitude, Is.GreaterThan(0.5f));

            Vector3 straightMidpoint = Arc.StartOffset * 0.5f;
            AssertVector(Arc.Offset(0.5f) - straightMidpoint, Arc.Bend);
        }

        [Test]
        public void VelocityMatchesTheDerivativeOfTheOffset()
        {
            const float h = 1e-3f;
            foreach (float t in new[] { 0.1f, 0.5f, 0.9f })
                AssertVector(Arc.Velocity(t), (Arc.Offset(t + h) - Arc.Offset(t - h)) / (2f * h), tolerance: 0.02f);
        }

        [Test]
        public void PunchDirection_PointsFromTheStartTowardsTheTarget()
        {
            Assert.That(Vector3.Dot(Arc.PunchDirection, -Arc.StartOffset.normalized), Is.GreaterThan(0.5f));
            Assert.That(Arc.PunchDirection.magnitude, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void RollUnwindsToZeroAtContact_FistLeads()
        {
            Quaternion atContact = Arc.Rotation(1f);
            AssertVector(atContact * Vector3.forward, Arc.PunchDirection, tolerance: 1e-3f);

            Quaternion unrolled = Quaternion.LookRotation(Arc.Velocity(0f), Vector3.up);
            Assert.That(Quaternion.Angle(Arc.Rotation(0f), unrolled), Is.EqualTo(180f).Within(0.1f));
        }

        private static void AssertVector(Vector3 actual, Vector3 expected, float tolerance = 1e-4f) =>
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(tolerance), $"{actual} != {expected}");
    }
}
