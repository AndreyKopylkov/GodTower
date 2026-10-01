using System;

namespace GodTower.Effects
{
    /// <summary>
    /// Layering rules for webhook bumps: at most <see cref="MaxActive"/> glove waves play at once, further requests
    /// wait in a bounded queue (requests beyond <see cref="MaxPending"/> are dropped so spam cannot pile up forever).
    /// Pure bookkeeping; the caller starts and ends the waves.
    /// </summary>
    public sealed class BumpQueue
    {
        public BumpQueue(int maxActive, int maxPending)
        {
            if (maxActive < 1)
                throw new ArgumentOutOfRangeException(nameof(maxActive), maxActive, "At least one wave must be able to play.");
            if (maxPending < 0)
                throw new ArgumentOutOfRangeException(nameof(maxPending), maxPending, "The queue size must not be negative.");

            MaxActive = maxActive;
            MaxPending = maxPending;
        }

        public int MaxActive { get; }

        public int MaxPending { get; }

        /// <summary>Waves currently playing.</summary>
        public int Active { get; private set; }

        /// <summary>Requests waiting for a free slot.</summary>
        public int Pending { get; private set; }

        /// <summary>Requests dropped because the queue was full (diagnostics).</summary>
        public int Dropped { get; private set; }

        public bool IsIdle => Active == 0 && Pending == 0;

        /// <summary>Registers a request. Returns <c>false</c> if it was dropped (queue full).</summary>
        public bool Enqueue()
        {
            // Free wave slots take requests on top of the queue (they start on the next tick).
            if (Pending >= MaxPending + (MaxActive - Active))
            {
                Dropped++;
                return false;
            }

            Pending++;
            return true;
        }

        /// <summary>Takes the next pending request if a wave slot is free; the caller must call <see cref="End"/> when it finishes.</summary>
        public bool TryBegin()
        {
            if (Pending == 0 || Active >= MaxActive)
                return false;

            Pending--;
            Active++;
            return true;
        }

        /// <summary>A wave started by <see cref="TryBegin"/> finished (or failed).</summary>
        public void End()
        {
            if (Active == 0)
                throw new InvalidOperationException("No wave is active.");

            Active--;
        }

        /// <summary>Forgets requests that have not started (the level ended).</summary>
        public void ClearPending() => Pending = 0;
    }
}
