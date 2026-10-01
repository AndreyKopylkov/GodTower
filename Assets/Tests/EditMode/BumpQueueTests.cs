using System;
using GodTower.Effects;
using NUnit.Framework;

namespace GodTower.Tests.EditMode
{
    public sealed class BumpQueueTests
    {
        [Test]
        public void StartsUpToMaxActiveWaves_QueuesTheRest()
        {
            var queue = new BumpQueue(maxActive: 3, maxPending: 10);
            for (int i = 0; i < 5; i++)
                Assert.That(queue.Enqueue(), Is.True);

            int started = 0;
            while (queue.TryBegin())
                started++;

            Assert.That(started, Is.EqualTo(3));
            Assert.That(queue.Active, Is.EqualTo(3));
            Assert.That(queue.Pending, Is.EqualTo(2));
        }

        [Test]
        public void EndingAWave_FreesASlotForTheNextRequest()
        {
            var queue = new BumpQueue(maxActive: 1, maxPending: 5);
            queue.Enqueue();
            queue.Enqueue();
            Assert.That(queue.TryBegin(), Is.True);
            Assert.That(queue.TryBegin(), Is.False);

            queue.End();

            Assert.That(queue.TryBegin(), Is.True);
            Assert.That(queue.Pending, Is.Zero);
            queue.End();
            Assert.That(queue.IsIdle, Is.True);
        }

        [Test]
        public void RequestsBeyondTheQueueAndFreeSlots_AreDropped()
        {
            var queue = new BumpQueue(maxActive: 3, maxPending: 2);

            int accepted = 0;
            for (int i = 0; i < 8; i++)
                if (queue.Enqueue())
                    accepted++;

            Assert.That(accepted, Is.EqualTo(5), "3 free slots + 2 queued.");
            Assert.That(queue.Dropped, Is.EqualTo(3));

            while (queue.TryBegin())
            {
            }

            Assert.That(queue.Active, Is.EqualTo(3));
            Assert.That(queue.Pending, Is.EqualTo(2));
            Assert.That(queue.Enqueue(), Is.False, "All slots busy and the queue is full.");
        }

        [Test]
        public void ClearPending_ForgetsWaitingRequests_KeepsActiveWaves()
        {
            var queue = new BumpQueue(maxActive: 1, maxPending: 5);
            queue.Enqueue();
            queue.Enqueue();
            queue.TryBegin();

            queue.ClearPending();

            Assert.That(queue.Pending, Is.Zero);
            Assert.That(queue.Active, Is.EqualTo(1));
            Assert.That(queue.TryBegin(), Is.False);
        }

        [Test]
        public void EndWithoutActiveWave_Throws() =>
            Assert.Throws<InvalidOperationException>(() => new BumpQueue(1, 1).End());

        [Test]
        public void InvalidLimits_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BumpQueue(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BumpQueue(1, -1));
        }
    }
}
