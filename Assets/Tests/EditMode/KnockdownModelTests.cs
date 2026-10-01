using GodTower.Gameplay;
using NUnit.Framework;

namespace GodTower.Tests.EditMode
{
    public sealed class KnockdownModelTests
    {
        private const float Tower = 300f;

        private static KnockdownModel Create() => new(Tower, new KnockdownSettings(0.08f, 0.03f, 0.06f, 5f));

        [Test]
        public void VillainKnockdown_Is8PercentOfTower()
        {
            Assert.That(Create().VillainKnockdown, Is.EqualTo(24f).Within(1e-4f));
        }

        [Test]
        public void Bump_Is3PercentOfTower()
        {
            Assert.That(Create().TakeBumpKnockdown(0f), Is.EqualTo(9f).Within(1e-4f));
        }

        [Test]
        public void Bumps_AreCappedAt6PercentPerRollingWindow()
        {
            KnockdownModel model = Create();

            Assert.That(model.TakeBumpKnockdown(0f), Is.EqualTo(9f).Within(1e-4f));
            Assert.That(model.TakeBumpKnockdown(1f), Is.EqualTo(9f).Within(1e-4f));
            Assert.That(model.TakeBumpKnockdown(2f), Is.Zero, "Cap of 18 m reached.");
            Assert.That(model.TakeBumpKnockdown(4.9f), Is.Zero);

            // The first bump leaves the window at t = 5.
            Assert.That(model.TakeBumpKnockdown(5f), Is.EqualTo(9f).Within(1e-4f));
            Assert.That(model.TakeBumpKnockdown(5.5f), Is.Zero);
        }

        [Test]
        public void Bump_IsPartial_WhenCapAlmostReached()
        {
            var model = new KnockdownModel(Tower, new KnockdownSettings(0.08f, 0.04f, 0.06f, 5f));

            Assert.That(model.TakeBumpKnockdown(0f), Is.EqualTo(12f).Within(1e-4f));
            Assert.That(model.TakeBumpKnockdown(1f), Is.EqualTo(6f).Within(1e-4f));
        }

        [Test]
        public void Apply_FloorsAtBottom()
        {
            Assert.That(KnockdownModel.Apply(100f, 24f), Is.EqualTo(76f));
            Assert.That(KnockdownModel.Apply(10f, 24f), Is.Zero);
        }
    }
}
