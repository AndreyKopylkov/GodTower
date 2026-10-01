using System;
using System.Collections.Generic;
using UnityEngine;

namespace GodTower.Gameplay
{
    /// <summary>Knockdown distances as fractions of the tower height (Docs/Plan.md §1–2).</summary>
    [Serializable]
    public sealed class KnockdownSettings
    {
        [SerializeField, Range(0f, 1f)] private float _villainFraction = 0.08f;
        [SerializeField, Range(0f, 1f)] private float _bumpFraction = 0.03f;
        [SerializeField, Range(0f, 1f)] private float _bumpCapFraction = 0.06f;
        [SerializeField, Min(0.1f)] private float _bumpCapWindow = 5f;

        public KnockdownSettings()
        {
        }

        public KnockdownSettings(float villainFraction, float bumpFraction, float bumpCapFraction, float bumpCapWindow)
        {
            _villainFraction = villainFraction;
            _bumpFraction = bumpFraction;
            _bumpCapFraction = bumpCapFraction;
            _bumpCapWindow = bumpCapWindow;
        }

        public float VillainFraction => _villainFraction;
        public float BumpFraction => _bumpFraction;
        public float BumpCapFraction => _bumpCapFraction;
        public float BumpCapWindow => _bumpCapWindow;
    }

    /// <summary>
    /// How far the climber is knocked down: a villain hit costs a fixed share of the tower,
    /// webhook bumps cost a smaller share but are capped over a rolling time window so spam cannot wipe a run.
    /// </summary>
    public sealed class KnockdownModel
    {
        private readonly float _towerHeight;
        private readonly KnockdownSettings _settings;
        private readonly Queue<(float Time, float Meters)> _recentBumps = new();

        public KnockdownModel(float towerHeight, KnockdownSettings settings)
        {
            if (towerHeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(towerHeight));

            _towerHeight = towerHeight;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public float VillainKnockdown => _towerHeight * _settings.VillainFraction;

        /// <summary>Knockdown for a bump at <paramref name="time"/> (seconds, monotonic), already reduced by the rolling cap.</summary>
        public float TakeBumpKnockdown(float time)
        {
            while (_recentBumps.Count > 0 && time - _recentBumps.Peek().Time >= _settings.BumpCapWindow)
                _recentBumps.Dequeue();

            float used = 0f;
            foreach ((float _, float taken) in _recentBumps)
                used += taken;

            float cap = _towerHeight * _settings.BumpCapFraction;
            float meters = Mathf.Clamp(_towerHeight * _settings.BumpFraction, 0f, Mathf.Max(0f, cap - used));
            if (meters > 0f)
                _recentBumps.Enqueue((time, meters));

            return meters;
        }

        /// <summary>Height after a knockdown; never below the bottom of the tower.</summary>
        public static float Apply(float height, float knockdown) => Mathf.Max(0f, height - knockdown);
    }
}
