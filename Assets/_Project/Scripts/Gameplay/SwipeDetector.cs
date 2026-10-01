using System;
using System.Collections.Generic;

namespace GodTower.Gameplay
{
    /// <summary>
    /// Classifies horizontal swipes from pointer samples of one press (pure logic, no Unity input).
    /// A swipe is a horizontal travel of at least <c>minDistanceFraction</c> of the screen width within
    /// <c>maxDuration</c> seconds. Several swipes can happen during one long press (swipe while holding).
    /// </summary>
    public sealed class SwipeDetector
    {
        public const float DefaultMinDistanceFraction = 0.08f;
        public const float DefaultMaxDuration = 0.35f;

        private readonly float _minDistanceFraction;
        private readonly float _maxDuration;
        private readonly Queue<Sample> _window = new();

        public SwipeDetector(float minDistanceFraction = DefaultMinDistanceFraction, float maxDuration = DefaultMaxDuration)
        {
            if (minDistanceFraction <= 0f)
                throw new ArgumentOutOfRangeException(nameof(minDistanceFraction));
            if (maxDuration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxDuration));

            _minDistanceFraction = minDistanceFraction;
            _maxDuration = maxDuration;
        }

        /// <summary>Starts a new gesture (pointer went down).</summary>
        public void Begin(float time, float x)
        {
            _window.Clear();
            _window.Enqueue(new Sample(time, x));
        }

        /// <summary>Adds a sample of the ongoing press.</summary>
        /// <returns>-1 for a swipe to the left, +1 to the right, 0 if no swipe completed with this sample.</returns>
        public int Update(float time, float x, float screenWidth)
        {
            _window.Enqueue(new Sample(time, x));
            while (time - _window.Peek().Time > _maxDuration)
                _window.Dequeue();

            float delta = x - _window.Peek().X;
            if (Math.Abs(delta) < _minDistanceFraction * screenWidth)
                return 0;

            // The swipe consumed this travel: the next swipe of the same press starts from here.
            Begin(time, x);
            return Math.Sign(delta);
        }

        /// <summary>Ends the gesture (pointer went up).</summary>
        public void End() => _window.Clear();

        private readonly struct Sample
        {
            public readonly float Time;
            public readonly float X;

            public Sample(float time, float x)
            {
                Time = time;
                X = x;
            }
        }
    }
}
