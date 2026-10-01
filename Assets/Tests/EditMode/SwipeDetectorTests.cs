using GodTower.Gameplay;
using NUnit.Framework;

namespace GodTower.Tests.EditMode
{
    public sealed class SwipeDetectorTests
    {
        private const float ScreenWidth = 1000f; // threshold = 8% = 80 px

        [Test]
        public void FastHorizontalTravel_IsSwipeInItsDirection()
        {
            var detector = new SwipeDetector();
            detector.Begin(0f, 500f);

            Assert.That(detector.Update(0.05f, 540f, ScreenWidth), Is.Zero);
            Assert.That(detector.Update(0.10f, 585f, ScreenWidth), Is.EqualTo(1));

            detector.Begin(1f, 500f);
            Assert.That(detector.Update(1.1f, 410f, ScreenWidth), Is.EqualTo(-1));
        }

        [Test]
        public void TravelBelowThreshold_IsNotSwipe()
        {
            var detector = new SwipeDetector();
            detector.Begin(0f, 500f);

            Assert.That(detector.Update(0.1f, 579f, ScreenWidth), Is.Zero);
        }

        [Test]
        public void SlowDrag_IsNotSwipe()
        {
            var detector = new SwipeDetector();
            detector.Begin(0f, 500f);

            // 20 px every 0.1 s: 80 px only after 0.4 s, longer than the 0.35 s window.
            int swipe = 0;
            for (int i = 1; i <= 10; i++)
                swipe |= detector.Update(i * 0.1f, 500f + i * 20f, ScreenWidth);

            Assert.That(swipe, Is.Zero);
        }

        [Test]
        public void SeveralSwipes_InOneLongPress()
        {
            var detector = new SwipeDetector();
            detector.Begin(0f, 500f);

            // Holding still for a while (climbing), then swiping right twice and left once.
            Assert.That(detector.Update(1.0f, 500f, ScreenWidth), Is.Zero);
            Assert.That(detector.Update(1.1f, 600f, ScreenWidth), Is.EqualTo(1));
            Assert.That(detector.Update(1.15f, 620f, ScreenWidth), Is.Zero, "The consumed travel must not count again.");
            Assert.That(detector.Update(1.2f, 700f, ScreenWidth), Is.EqualTo(1));
            Assert.That(detector.Update(1.4f, 600f, ScreenWidth), Is.EqualTo(-1));
        }

        [Test]
        public void VerticalMovement_IsIgnored()
        {
            var detector = new SwipeDetector();
            detector.Begin(0f, 500f);

            Assert.That(detector.Update(0.1f, 500f, ScreenWidth), Is.Zero);
        }
    }
}
