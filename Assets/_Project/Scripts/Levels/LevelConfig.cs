using System.Collections.Generic;
using GodTower.Events;
using UnityEngine;

namespace GodTower.Levels
{
    /// <summary>One level of the game (Docs/Plan.md §1 level table). Authored by <c>LevelAssetsBuilder</c>.</summary>
    [CreateAssetMenu(menuName = "God Tower/Level Config", fileName = "Level_00")]
    public sealed class LevelConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int _number = 1;

        [Tooltip("Climbable height in displayed meters (1 m = 1 world unit).")]
        [SerializeField, Min(10f)] private float _towerHeight = 300f;

        [SerializeField, Min(5f)] private float _timeLimit = 75f;

        [Header("Villains")]
        [SerializeField] private VillainKind[] _villains = { VillainKind.Missile };
        [SerializeField, Min(1f)] private float _villainInterval = 8f;
        [SerializeField, Min(0.1f)] private float _telegraphDuration = 1f;

        [Tooltip("Some villain slots get a second villain right after the first.")]
        [SerializeField] private bool _backToBackPairs;

        [Header("Heroes")]
        [SerializeField] private HeroEventEntry[] _heroEvents = { new(HeroKind.Jetpack, 25f) };

        [Tooltip("Seed of the villain schedule and the tower segment order (deterministic levels).")]
        [SerializeField] private int _seed = 1;

        public int Number => _number;
        public float TowerHeight => _towerHeight;
        public float TimeLimit => _timeLimit;
        public IReadOnlyList<VillainKind> Villains => _villains;
        public float VillainInterval => _villainInterval;
        public float TelegraphDuration => _telegraphDuration;
        public bool BackToBackPairs => _backToBackPairs;
        public IReadOnlyList<HeroEventEntry> HeroEvents => _heroEvents;
        public int Seed => _seed;
    }
}
