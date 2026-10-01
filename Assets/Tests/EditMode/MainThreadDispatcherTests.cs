using System;
using System.Threading;
using System.Threading.Tasks;
using GodTower.Core;
using NUnit.Framework;

namespace GodTower.Tests.EditMode
{
    public sealed class MainThreadDispatcherTests
    {
        private static readonly TimeSpan LongTimeout = TimeSpan.FromSeconds(5);

        [Test]
        public void Work_RunsOnTick_OnTheTickingThread()
        {
            var dispatcher = new MainThreadDispatcher();
            int tickThread = Thread.CurrentThread.ManagedThreadId;

            Task<int> task = Task.Run(() => dispatcher.InvokeAsync(() => Thread.CurrentThread.ManagedThreadId, LongTimeout));
            TickUntilCompleted(dispatcher, task);

            Assert.That(task.Wait(LongTimeout), Is.True);
            Assert.That(task.Result, Is.EqualTo(tickThread));
        }

        [Test]
        public void Work_DoesNotRunBeforeTick()
        {
            var dispatcher = new MainThreadDispatcher();
            bool ran = false;

            Task<bool> task = dispatcher.InvokeAsync(() => ran = true, LongTimeout);

            Assert.That(ran, Is.False);
            Assert.That(task.IsCompleted, Is.False);

            dispatcher.Tick();
            Assert.That(task.Wait(LongTimeout), Is.True);
            Assert.That(ran, Is.True);
        }

        [Test]
        public void TimesOut_AndAbandonedWorkNeverRuns()
        {
            var dispatcher = new MainThreadDispatcher();
            bool ran = false;

            Task<bool> task = dispatcher.InvokeAsync(() => ran = true, TimeSpan.FromMilliseconds(50));

            AggregateException error = Assert.Throws<AggregateException>(() => task.Wait(LongTimeout));
            Assert.That(error.InnerException, Is.TypeOf<TimeoutException>());

            dispatcher.Tick();
            Assert.That(ran, Is.False);
        }

        [Test]
        public void CallerCancellation_CancelsTask()
        {
            var dispatcher = new MainThreadDispatcher();
            using var cancellation = new CancellationTokenSource();

            Task<int> task = dispatcher.InvokeAsync(() => 1, LongTimeout, cancellation.Token);
            cancellation.Cancel();

            Assert.Throws<AggregateException>(() => task.Wait(LongTimeout));
            Assert.That(task.IsCanceled, Is.True);
        }

        [Test]
        public void WorkException_PropagatesToCaller()
        {
            var dispatcher = new MainThreadDispatcher();

            Task<int> task = dispatcher.InvokeAsync<int>(() => throw new InvalidOperationException("boom"), LongTimeout);
            dispatcher.Tick();

            AggregateException error = Assert.Throws<AggregateException>(() => task.Wait(LongTimeout));
            Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Tick_RunsAllQueuedWorkInOrder()
        {
            var dispatcher = new MainThreadDispatcher();
            var order = new System.Collections.Generic.List<int>();

            for (int i = 0; i < 5; i++)
            {
                int value = i;
                dispatcher.InvokeAsync(() => { order.Add(value); return value; }, LongTimeout);
            }

            dispatcher.Tick();

            Assert.That(order, Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
        }

        /// <summary>Plays the role of the main-thread player loop until the background caller got its result.</summary>
        private static void TickUntilCompleted(MainThreadDispatcher dispatcher, Task task)
        {
            DateTime deadline = DateTime.UtcNow + LongTimeout;
            while (!task.IsCompleted && DateTime.UtcNow < deadline)
            {
                dispatcher.Tick();
                Thread.Sleep(1);
            }
        }
    }
}
