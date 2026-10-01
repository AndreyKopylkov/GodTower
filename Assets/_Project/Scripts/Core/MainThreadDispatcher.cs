using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using VContainer.Unity;

namespace GodTower.Core
{
    /// <summary>
    /// Runs work queued from background threads on Unity's main thread (drained every frame in <see cref="Tick"/>).
    /// Each call can time out: work that has not started before the timeout is abandoned and never runs,
    /// so a caller that gave up never causes a late side effect.
    /// </summary>
    public sealed class MainThreadDispatcher : ITickable
    {
        private readonly ConcurrentQueue<IWorkItem> _queue = new();

        /// <summary>Thread-safe. Queues <paramref name="work"/> for the main thread and awaits its result.</summary>
        /// <exception cref="TimeoutException">The main thread did not pick the work up within <paramref name="timeout"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled first.</exception>
        public Task<T> InvokeAsync<T>(Func<T> work, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            var item = new WorkItem<T>(work);
            _queue.Enqueue(item);
            return item.WaitAsync(timeout, cancellationToken);
        }

        /// <summary>Main thread. Executes everything queued so far.</summary>
        public void Tick()
        {
            while (_queue.TryDequeue(out IWorkItem item))
                item.Execute();
        }

        private interface IWorkItem
        {
            void Execute();
        }

        private sealed class WorkItem<T> : IWorkItem
        {
            private const int Pending = 0;
            private const int Claimed = 1;
            private const int Abandoned = 2;

            private readonly Func<T> _work;
            private readonly TaskCompletionSource<T> _completion =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private int _state = Pending;

            public WorkItem(Func<T> work) => _work = work;

            public void Execute()
            {
                if (Interlocked.CompareExchange(ref _state, Claimed, Pending) != Pending)
                    return;

                try
                {
                    _completion.SetResult(_work());
                }
                catch (Exception exception)
                {
                    _completion.SetException(exception);
                }
            }

            public async Task<T> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
            {
                using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutSource.CancelAfter(timeout);

                using (timeoutSource.Token.Register(() => Abandon(cancellationToken)))
                    return await _completion.Task.ConfigureAwait(false);
            }

            private void Abandon(CancellationToken callerToken)
            {
                if (Interlocked.CompareExchange(ref _state, Abandoned, Pending) != Pending)
                    return;

                if (callerToken.IsCancellationRequested)
                    _completion.SetCanceled();
                else
                    _completion.SetException(new TimeoutException("The main thread did not process the request in time."));
            }
        }
    }
}
