using System.Collections.Generic;
using GodTower.Core;
using NUnit.Framework;

namespace GodTower.Tests.EditMode
{
    public sealed class PlayStateServiceTests
    {
        [Test]
        public void StartsInMenu()
        {
            var service = new PlayStateService();

            Assert.That(service.Current, Is.EqualTo(PlayState.Menu));
            Assert.That(service.IsPlaying, Is.False);
        }

        [Test]
        public void RaisesChanged_OnlyWhenStateActuallyChanges()
        {
            var service = new PlayStateService();
            var changes = new List<PlayState>();
            service.Changed += changes.Add;

            service.Set(PlayState.Playing);
            service.Set(PlayState.Playing);
            service.Set(PlayState.Paused);

            Assert.That(changes, Is.EqualTo(new[] { PlayState.Playing, PlayState.Paused }));
            Assert.That(service.IsPlaying, Is.False);
        }
    }
}
