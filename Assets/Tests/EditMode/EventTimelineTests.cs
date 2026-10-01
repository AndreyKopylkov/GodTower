using System.Linq;
using GodTower.Events;
using NUnit.Framework;

namespace GodTower.Tests.EditMode
{
    public sealed class EventTimelineTests
    {
        private static TimelineParameters Level(int seed = 7, bool pairs = false, params HeroBoost[] boosts) => new()
        {
            TimeLimit = 120f,
            Villains = new[] { VillainKind.Missile, VillainKind.Truck, VillainKind.Axes },
            VillainInterval = 4f,
            TelegraphDuration = 0.6f,
            BackToBackPairs = pairs,
            HeroBoosts = boosts,
            Seed = seed
        };

        [Test]
        public void Strikes_AreSortedAndTelegraphedBeforeImpact()
        {
            EventTimeline timeline = EventTimeline.Build(Level(pairs: true));

            Assert.That(timeline.Strikes, Is.Not.Empty);
            Assert.That(timeline.Strikes.Select(s => s.ImpactTime), Is.Ordered);
            Assert.That(timeline.Strikes.Select(s => s.Id), Is.EqualTo(Enumerable.Range(0, timeline.Strikes.Count)));
            foreach (VillainStrike strike in timeline.Strikes)
                Assert.That(strike.ImpactTime - strike.TelegraphTime, Is.EqualTo(0.6f).Within(1e-4f), strike.ToString());
        }

        [Test]
        public void SameSeed_SameSchedule_DifferentSeed_DifferentSchedule()
        {
            string Describe(EventTimeline t) => string.Join(";", t.Strikes.Select(s => s.ToString()));

            Assert.That(Describe(EventTimeline.Build(Level(seed: 3))), Is.EqualTo(Describe(EventTimeline.Build(Level(seed: 3)))));
            Assert.That(Describe(EventTimeline.Build(Level(seed: 3))), Is.Not.EqualTo(Describe(EventTimeline.Build(Level(seed: 4)))));
        }

        [Test]
        public void VillainsLandEveryInterval_StartingAfterOneInterval_AndNotAtTheVeryEnd()
        {
            TimelineParameters parameters = Level();
            parameters.TimeLimit = 75f;
            parameters.VillainInterval = 8f;

            EventTimeline timeline = EventTimeline.Build(parameters);

            Assert.That(timeline.Strikes.Select(s => s.ImpactTime), Is.EqualTo(new[] { 8f, 16f, 24f, 32f, 40f, 48f, 56f, 64f, 72f }));
        }

        [Test]
        public void OnlyAllowedKinds_AreScheduled()
        {
            TimelineParameters parameters = Level();
            parameters.Villains = new[] { VillainKind.Missile };

            Assert.That(EventTimeline.Build(parameters).Strikes.Select(s => s.Kind), Is.All.EqualTo(VillainKind.Missile));
        }

        [Test]
        public void NoVillains_NoStrikes()
        {
            TimelineParameters parameters = Level();
            parameters.Villains = new VillainKind[0];

            Assert.That(EventTimeline.Build(parameters).Strikes, Is.Empty);
        }

        [Test]
        public void Missile_And_Truck_HitOneLane_Axes_LeaveOneSafeLane()
        {
            foreach (VillainStrike strike in EventTimeline.Build(Level()).Strikes)
            {
                int hitLanes = Enumerable.Range(0, 3).Count(strike.Hits);
                Assert.That(hitLanes, Is.EqualTo(strike.Kind == VillainKind.Axes ? 2 : 1), strike.ToString());
            }

            Assert.That(EventTimeline.LaneMaskFor(VillainKind.Axes, 1, 3), Is.EqualTo(0b101));
            Assert.That(EventTimeline.LaneMaskFor(VillainKind.Missile, 2, 3), Is.EqualTo(0b100));
        }

        [Test]
        public void NoVillain_IsTelegraphedOrLands_DuringAHeroBoost()
        {
            var boost = new HeroBoost(HeroKind.Phoenix, 30f, 3f);
            EventTimeline timeline = EventTimeline.Build(Level(boosts: new[] { boost }));

            Assert.That(timeline.Boosts, Is.EqualTo(new[] { boost }));
            foreach (VillainStrike strike in timeline.Strikes)
                Assert.That(strike.ImpactTime < boost.Time - 1f || strike.TelegraphTime > boost.EndTime + 1f, Is.True, strike.ToString());
        }

        [Test]
        public void BackToBackPairs_AddSecondVillainsShortlyAfter()
        {
            int single = EventTimeline.Build(Level(pairs: false)).Strikes.Count;
            EventTimeline paired = EventTimeline.Build(Level(pairs: true));

            Assert.That(paired.Strikes.Count, Is.GreaterThan(single));
            bool hasPair = paired.Strikes.Zip(paired.Strikes.Skip(1), (a, b) => b.ImpactTime - a.ImpactTime)
                .Any(gap => gap < 1.5f);
            Assert.That(hasPair, Is.True);
        }

        [Test]
        public void Boosts_AreSortedByTime()
        {
            EventTimeline timeline = EventTimeline.Build(Level(boosts: new[]
            {
                new HeroBoost(HeroKind.Phoenix, 75f, 3f), new HeroBoost(HeroKind.Jetpack, 35f, 2f)
            }));

            Assert.That(timeline.Boosts.Select(b => b.Kind), Is.EqualTo(new[] { HeroKind.Jetpack, HeroKind.Phoenix }));
        }
    }
}
