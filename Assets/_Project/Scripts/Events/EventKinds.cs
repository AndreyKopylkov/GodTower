using System;
using UnityEngine;

namespace GodTower.Events
{
    /// <summary>Villain events knock the climber down when they land in its lane.</summary>
    public enum VillainKind
    {
        /// <summary>Flies in horizontally from the screen side into one lane.</summary>
        Missile,

        /// <summary>Falls from above into one lane.</summary>
        Truck,

        /// <summary>Spinning axes sweep two lanes; one lane is safe.</summary>
        Axes
    }

    /// <summary>Hero events carry the climber up automatically.</summary>
    public enum HeroKind
    {
        Jetpack,
        Phoenix
    }

    /// <summary>A hero event at a fixed time of the level timeline.</summary>
    [Serializable]
    public struct HeroEventEntry
    {
        [SerializeField] private HeroKind _kind;
        [SerializeField, Min(0f)] private float _time;

        public HeroEventEntry(HeroKind kind, float time)
        {
            _kind = kind;
            _time = time;
        }

        public HeroKind Kind => _kind;

        /// <summary>Seconds since the level started.</summary>
        public float Time => _time;
    }
}
