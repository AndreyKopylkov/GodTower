using GodTower.Levels;
using NUnit.Framework;

namespace GodTower.Tests.EditMode
{
    public sealed class TowerLayoutTests
    {
        [Test]
        public void MultipleOfSegmentHeight_StartsAtZeroAboveTheExtraSegments()
        {
            var layout = new TowerLayout(300f, 3f, extraSegmentsBelow: 4);

            Assert.That(layout.SegmentCount, Is.EqualTo(104));
            Assert.That(layout.BaseY, Is.EqualTo(-12f).Within(1e-4f));
            Assert.That(layout.SegmentY(4), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(layout.TopY, Is.EqualTo(300f));
        }

        [Test]
        public void OtherHeights_ShiftTheStackDown_SoTheTopMatchesExactly()
        {
            var layout = new TowerLayout(400f, 3f, extraSegmentsBelow: 0);

            Assert.That(layout.SegmentCount, Is.EqualTo(134));
            Assert.That(layout.BaseY, Is.EqualTo(-2f).Within(1e-3f));
            Assert.That(layout.SegmentY(layout.SegmentCount - 1) + 3f, Is.EqualTo(400f).Within(1e-3f));
        }
    }
}
