using System;
using System.Linq;
using UnityEngine;

namespace GodTower.Events
{
    /// <summary>All villain and hero event configs, looked up by kind.</summary>
    [CreateAssetMenu(menuName = "God Tower/Events Config", fileName = "EventsConfig")]
    public sealed class EventsConfig : ScriptableObject
    {
        [SerializeField] private VillainEventConfig[] _villains = Array.Empty<VillainEventConfig>();
        [SerializeField] private HeroEventConfig[] _heroes = Array.Empty<HeroEventConfig>();

        public VillainEventConfig Get(VillainKind kind) =>
            _villains.FirstOrDefault(config => config != null && config.Kind == kind)
            ?? throw new InvalidOperationException($"No villain config for {kind}.");

        public HeroEventConfig Get(HeroKind kind) =>
            _heroes.FirstOrDefault(config => config != null && config.Kind == kind)
            ?? throw new InvalidOperationException($"No hero config for {kind}.");
    }
}
