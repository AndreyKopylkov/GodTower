using System;
using System.Linq;
using GodTower.Gameplay;
using GodTower.Levels;
using VContainer.Unity;

namespace GodTower.Events
{
    /// <summary>
    /// Plays the level's <see cref="EventTimeline"/> on the level clock: announces villains (telegraph), resolves
    /// their impact against the climber's lane (hit = knockdown, otherwise a dodge) and starts hero boosts.
    /// Pure game logic; presentation subscribes to the events.
    /// </summary>
    public sealed class EventDirector : ITickable
    {
        private readonly LevelRunner _runner;
        private readonly EventsConfig _config;
        private int _nextTelegraph;
        private int _nextImpact;
        private int _nextBoost;

        public EventDirector(LevelRunner runner, EventsConfig config)
        {
            _runner = runner;
            _config = config;

            Timeline = BuildTimeline(runner.Level, config);
        }

        public EventTimeline Timeline { get; }

        /// <summary>The level's seeded timeline (deterministic: the same level always gets the same events).</summary>
        public static EventTimeline BuildTimeline(LevelConfig level, EventsConfig config) =>
            EventTimeline.Build(new TimelineParameters
            {
                TimeLimit = level.TimeLimit,
                Villains = level.Villains,
                VillainInterval = level.VillainInterval,
                TelegraphDuration = level.TelegraphDuration,
                BackToBackPairs = level.BackToBackPairs,
                HeroBoosts = level.HeroEvents
                    .Select(entry => new HeroBoost(entry.Kind, entry.Time, config.Get(entry.Kind).Duration))
                    .ToList(),
                Seed = level.Seed
            });

        /// <summary>A villain is announced: telegraph its lanes now, it lands at <see cref="VillainStrike.ImpactTime"/>.</summary>
        public event Action<VillainStrike> Telegraphed;

        /// <summary>A villain landed; the flag tells whether it hit the climber.</summary>
        public event Action<VillainStrike, bool> Landed;

        public event Action<HeroBoost> BoostStarted;

        public void Tick()
        {
            if (!_runner.IsRunning)
                return;

            float time = _runner.Elapsed;
            var strikes = Timeline.Strikes;

            while (_nextTelegraph < strikes.Count && time >= strikes[_nextTelegraph].TelegraphTime)
                Telegraphed?.Invoke(strikes[_nextTelegraph++]);

            while (_nextImpact < strikes.Count && time >= strikes[_nextImpact].ImpactTime)
                Land(strikes[_nextImpact++]);

            var boosts = Timeline.Boosts;
            while (_nextBoost < boosts.Count && time >= boosts[_nextBoost].Time)
                StartBoost(boosts[_nextBoost++]);
        }

        private void Land(VillainStrike strike)
        {
            ClimberMotor climber = _runner.Climber;
            bool hit = climber.IsVulnerable && strike.Hits(climber.Lanes.Index);
            if (hit)
                climber.Knockdown(_runner.Knockdown.VillainKnockdown);

            Landed?.Invoke(strike, hit);
        }

        private void StartBoost(HeroBoost boost)
        {
            ClimberMotor climber = _runner.Climber;
            if (climber.IsFinished)
                return;

            climber.Carry(climber.TopHeight * _config.Get(boost.Kind).HeightFraction, boost.Duration);
            BoostStarted?.Invoke(boost);
        }
    }
}
