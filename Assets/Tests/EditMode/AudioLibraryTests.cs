using System;
using GodTower.Audio;
using NUnit.Framework;
using UnityEditor;

namespace GodTower.Tests.EditMode
{
    /// <summary>The generated <see cref="AudioLibrary"/> maps every sound and both music loops to lane S's clips.</summary>
    public sealed class AudioLibraryTests
    {
        private const string LibraryPath = "Assets/_Project/Configs/AudioLibrary.asset";

        private static AudioLibrary Library()
        {
            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            Assert.That(library, Is.Not.Null, $"{LibraryPath} missing — run BuildTools.SetupProject.");
            return library;
        }

        [Test]
        public void EverySoundHasAClip([Values] SoundId id)
        {
            SoundEntry entry = Library().Find(id);

            Assert.That(entry, Is.Not.Null, $"No entry for {id}.");
            Assert.That(entry.PickClip(), Is.Not.Null, $"{id} has no clip.");
            Assert.That(entry.Volume, Is.GreaterThan(0f));
        }

        [Test]
        public void BothMusicLoopsAreSet()
        {
            AudioLibrary library = Library();

            Assert.That(library.Music(MusicId.Menu), Is.Not.Null);
            Assert.That(library.Music(MusicId.Level), Is.Not.Null);
            Assert.That(library.Music(MusicId.None), Is.Null);
        }

        [Test]
        public void UiSoundsIgnoreThePause()
        {
            AudioLibrary library = Library();

            foreach (SoundId id in new[] { SoundId.UiClick, SoundId.UiLocked, SoundId.UiOpen, SoundId.Win, SoundId.Lose })
                Assert.That(library.Find(id).IsUi, Is.True, $"{id} must play while paused / on the result screen.");
            Assert.That(library.Find(SoundId.Punch).IsUi, Is.False);
        }
    }
}
