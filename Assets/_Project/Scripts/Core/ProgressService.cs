using System;
using UnityEngine;

namespace GodTower.Core
{
    /// <summary>Persistent storage of the number of completed levels.</summary>
    public interface IProgressStore
    {
        int LoadCompletedCount();

        void SaveCompletedCount(int count);
    }

    /// <summary><see cref="IProgressStore"/> on <see cref="PlayerPrefs"/>.</summary>
    public sealed class PlayerPrefsProgressStore : IProgressStore
    {
        public const string Key = "godtower.progress.completed";

        public int LoadCompletedCount() => PlayerPrefs.GetInt(Key, 0);

        public void SaveCompletedCount(int count)
        {
            PlayerPrefs.SetInt(Key, count);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Sequential level unlock (Docs/Plan.md §1): level 1 is always open, completing level N opens level N + 1.
    /// Indices are 0-based like the level catalog.
    /// </summary>
    public sealed class ProgressService
    {
        private readonly IProgressStore _store;

        public ProgressService(IProgressStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            CompletedCount = Mathf.Max(0, store.LoadCompletedCount());
        }

        /// <summary>Levels completed in order (level indices below this are done).</summary>
        public int CompletedCount { get; private set; }

        public bool IsCompleted(int index) => index >= 0 && index < CompletedCount;

        public bool IsUnlocked(int index) => index >= 0 && index <= CompletedCount;

        /// <summary>The level "Play" continues with: the first level not completed yet, clamped to the last level.</summary>
        public int NextLevelIndex(int levelCount) => Mathf.Clamp(CompletedCount, 0, Mathf.Max(0, levelCount - 1));

        /// <summary>Records a win: the level and every level before it count as completed (replays change nothing).</summary>
        public void MarkCompleted(int index)
        {
            if (index < 0 || index + 1 <= CompletedCount)
                return;

            CompletedCount = index + 1;
            _store.SaveCompletedCount(CompletedCount);
        }
    }
}
