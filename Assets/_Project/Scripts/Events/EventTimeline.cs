using System;
using System.Collections.Generic;
using System.Linq;

namespace GodTower.Events
{
    /// <summary>A villain landing at <see cref="ImpactTime"/> on a set of lanes, announced from <see cref="TelegraphTime"/>.</summary>
    public readonly struct VillainStrike
    {
        public VillainStrike(int id, VillainKind kind, float impactTime, float telegraphTime, int laneMask)
        {
            Id = id;
            Kind = kind;
            ImpactTime = impactTime;
            TelegraphTime = telegraphTime;
            LaneMask = laneMask;
        }

        /// <summary>Index in the level schedule (stable per seed).</summary>
        public int Id { get; }

        public VillainKind Kind { get; }

        /// <summary>Seconds since the level started.</summary>
        public float ImpactTime { get; }

        public float TelegraphTime { get; }

        /// <summary>Bit i set = lane i is hit.</summary>
        public int LaneMask { get; }

        public bool Hits(int lane) => (LaneMask & (1 << lane)) != 0;

        public override string ToString() => $"{Kind} @{ImpactTime:0.0}s lanes={Convert.ToString(LaneMask, 2)}";
    }

    /// <summary>A hero event starting at <see cref="Time"/>.</summary>
    public readonly struct HeroBoost
    {
        public HeroBoost(HeroKind kind, float time, float duration)
        {
            Kind = kind;
            Time = time;
            Duration = duration;
        }

        public HeroKind Kind { get; }

        public float Time { get; }

        public float Duration { get; }

        public float EndTime => Time + Duration;
    }

    /// <summary>Inputs of <see cref="EventTimeline.Build"/> (plain data so the schedule is testable without assets).</summary>
    public sealed class TimelineParameters
    {
        public float TimeLimit;
        public IReadOnlyList<VillainKind> Villains = Array.Empty<VillainKind>();
        public float VillainInterval = 8f;
        public float TelegraphDuration = 1f;
        public bool BackToBackPairs;
        public IReadOnlyList<HeroBoost> HeroBoosts = Array.Empty<HeroBoost>();
        public int LaneCount = 3;
        public int Seed;

        /// <summary>Delay of the second villain of a back-to-back pair.</summary>
        public float PairGap = 1.2f;

        /// <summary>Share of villain slots that become pairs when <see cref="BackToBackPairs"/> is on.</summary>
        public float PairChance = 0.5f;

        /// <summary>No villain lands this close around a hero event (the climber is carried and immune).</summary>
        public float HeroClearance = 1f;

        /// <summary>No villain lands in the last seconds of the time limit.</summary>
        public float EndClearance = 2f;
    }

    /// <summary>The deterministic event schedule of a level.</summary>
    public sealed class EventTimeline
    {
        private EventTimeline(IReadOnlyList<VillainStrike> strikes, IReadOnlyList<HeroBoost> boosts)
        {
            Strikes = strikes;
            Boosts = boosts;
        }

        /// <summary>Sorted by impact time.</summary>
        public IReadOnlyList<VillainStrike> Strikes { get; }

        /// <summary>Sorted by start time.</summary>
        public IReadOnlyList<HeroBoost> Boosts { get; }

        /// <summary>
        /// Villains land every <c>VillainInterval</c> seconds (first one after one interval), each of a seeded random
        /// allowed kind and lane; axes sweep all lanes but one. Slots overlapping a hero event are skipped.
        /// </summary>
        public static EventTimeline Build(TimelineParameters parameters)
        {
            if (parameters.LaneCount < 2)
                throw new ArgumentOutOfRangeException(nameof(parameters), "At least two lanes are required.");
            if (parameters.VillainInterval <= 0f)
                throw new ArgumentOutOfRangeException(nameof(parameters), "Villain interval must be positive.");

            var random = new Random(parameters.Seed);
            List<HeroBoost> boosts = parameters.HeroBoosts.OrderBy(boost => boost.Time).ToList();
            var strikes = new List<VillainStrike>();

            if (parameters.Villains.Count > 0)
            {
                float lastImpact = parameters.TimeLimit - parameters.EndClearance;
                for (float slot = parameters.VillainInterval; slot <= lastImpact; slot += parameters.VillainInterval)
                {
                    // Draw every slot's randomness even when skipped, so one hero time change does not reshuffle the level.
                    VillainKind kind = parameters.Villains[random.Next(parameters.Villains.Count)];
                    int lane = random.Next(parameters.LaneCount);
                    bool pair = parameters.BackToBackPairs && random.NextDouble() < parameters.PairChance;
                    VillainKind pairKind = parameters.Villains[random.Next(parameters.Villains.Count)];
                    int pairLane = random.Next(parameters.LaneCount);

                    TryAdd(strikes, parameters, boosts, kind, slot, lane);
                    if (pair && slot + parameters.PairGap <= lastImpact)
                        TryAdd(strikes, parameters, boosts, pairKind, slot + parameters.PairGap, pairLane);
                }
            }

            List<VillainStrike> ordered = strikes
                .OrderBy(strike => strike.ImpactTime)
                .Select((strike, index) => new VillainStrike(index, strike.Kind, strike.ImpactTime, strike.TelegraphTime, strike.LaneMask))
                .ToList();

            return new EventTimeline(ordered, boosts);
        }

        /// <summary>Lanes hit by a villain of <paramref name="kind"/> aimed at <paramref name="lane"/> (for axes: the safe lane).</summary>
        public static int LaneMaskFor(VillainKind kind, int lane, int laneCount)
        {
            int all = (1 << laneCount) - 1;
            return kind == VillainKind.Axes ? all & ~(1 << lane) : 1 << lane;
        }

        private static void TryAdd(List<VillainStrike> strikes, TimelineParameters parameters, List<HeroBoost> boosts,
            VillainKind kind, float impact, int lane)
        {
            float telegraph = Math.Max(0f, impact - parameters.TelegraphDuration);
            bool overlapsHero = boosts.Any(boost =>
                impact >= boost.Time - parameters.HeroClearance && telegraph <= boost.EndTime + parameters.HeroClearance);
            if (overlapsHero)
                return;

            strikes.Add(new VillainStrike(strikes.Count, kind, impact, telegraph, LaneMaskFor(kind, lane, parameters.LaneCount)));
        }
    }
}
