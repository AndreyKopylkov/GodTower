using System;
using System.Collections.Generic;
using UnityEngine;

namespace GodTower.Levels
{
    /// <summary>All levels in play order.</summary>
    [CreateAssetMenu(menuName = "God Tower/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] private LevelConfig[] _levels = Array.Empty<LevelConfig>();

        public IReadOnlyList<LevelConfig> Levels => _levels;

        public int Count => _levels.Length;

        public LevelConfig Get(int index)
        {
            if (_levels.Length == 0)
                throw new InvalidOperationException("The level catalog is empty.");

            return _levels[Mathf.Clamp(index, 0, _levels.Length - 1)];
        }
    }
}
