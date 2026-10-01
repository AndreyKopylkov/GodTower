using GodTower.Core;
using GodTower.Webhook;
using NUnit.Framework;

namespace GodTower.Tests.EditMode
{
    public sealed class BumpGateTests
    {
        private PlayStateService _playState;
        private BumpGate _gate;
        private int _bumps;

        [SetUp]
        public void SetUp()
        {
            _playState = new PlayStateService();
            _gate = new BumpGate(_playState);
            _bumps = 0;
            _gate.BumpRequested += () => _bumps++;
        }

        [Test]
        public void AcceptsAndSignals_WhilePlaying()
        {
            _playState.Set(PlayState.Playing);

            Assert.That(_gate.TryTrigger(), Is.True);
            Assert.That(_bumps, Is.EqualTo(1));
        }

        [TestCase(PlayState.Menu)]
        [TestCase(PlayState.Paused)]
        [TestCase(PlayState.Result)]
        public void IgnoresWithoutSignal_WhenNotPlaying(PlayState state)
        {
            _playState.Set(state);

            Assert.That(_gate.TryTrigger(), Is.False);
            Assert.That(_bumps, Is.Zero);
        }

        [Test]
        public void FollowsStateChanges()
        {
            _playState.Set(PlayState.Playing);
            _gate.TryTrigger();
            _playState.Set(PlayState.Paused);
            _gate.TryTrigger();
            _playState.Set(PlayState.Playing);
            _gate.TryTrigger();

            Assert.That(_bumps, Is.EqualTo(2));
        }
    }
}
