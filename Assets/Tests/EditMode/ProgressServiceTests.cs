using GodTower.Core;
using NUnit.Framework;

namespace GodTower.Tests.EditMode
{
    public sealed class ProgressServiceTests
    {
        private sealed class MemoryStore : IProgressStore
        {
            public int Saved;
            public int Saves;

            public int LoadCompletedCount() => Saved;

            public void SaveCompletedCount(int count)
            {
                Saved = count;
                Saves++;
            }
        }

        [Test]
        public void FreshProgress_OnlyLevel1IsUnlocked()
        {
            var progress = new ProgressService(new MemoryStore());

            Assert.That(progress.IsUnlocked(0), Is.True);
            Assert.That(progress.IsUnlocked(1), Is.False);
            Assert.That(progress.IsCompleted(0), Is.False);
            Assert.That(progress.NextLevelIndex(5), Is.Zero);
        }

        [Test]
        public void CompletingALevel_UnlocksTheNextOne_AndIsSaved()
        {
            var store = new MemoryStore();
            var progress = new ProgressService(store);

            progress.MarkCompleted(0);

            Assert.That(progress.IsCompleted(0), Is.True);
            Assert.That(progress.IsUnlocked(1), Is.True);
            Assert.That(progress.IsUnlocked(2), Is.False);
            Assert.That(store.Saved, Is.EqualTo(1));
            Assert.That(new ProgressService(store).IsUnlocked(1), Is.True, "Progress survives a restart.");
        }

        [Test]
        public void ReplayingAnOldLevel_ChangesNothing()
        {
            var store = new MemoryStore { Saved = 3 };
            var progress = new ProgressService(store);

            progress.MarkCompleted(1);

            Assert.That(progress.CompletedCount, Is.EqualTo(3));
            Assert.That(store.Saves, Is.Zero);
        }

        [Test]
        public void NextLevel_ContinuesAfterTheLastCompleted_ClampedToTheLastLevel()
        {
            var progress = new ProgressService(new MemoryStore { Saved = 2 });
            Assert.That(progress.NextLevelIndex(5), Is.EqualTo(2));

            progress.MarkCompleted(4);
            Assert.That(progress.NextLevelIndex(5), Is.EqualTo(4), "Everything done: Play replays the last level.");
            Assert.That(progress.IsUnlocked(4), Is.True);
        }

        [Test]
        public void CorruptNegativeValue_IsTreatedAsNoProgress()
        {
            var progress = new ProgressService(new MemoryStore { Saved = -4 });

            Assert.That(progress.CompletedCount, Is.Zero);
            Assert.That(progress.IsUnlocked(0), Is.True);
            Assert.That(progress.IsUnlocked(-1), Is.False);
        }
    }
}
