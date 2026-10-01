using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GodTower.Core;
using UnityEngine;
using VContainer.Unity;

namespace GodTower.Webhook
{
    /// <summary>
    /// Minimal HTTP/1.1 server for <c>GET|POST http://localhost:56789/bump</c>.
    /// Accepts connections on background threads (IPv4 and, when available, IPv6 loopback), answers 400/404/405
    /// directly and asks the main thread — through <see cref="MainThreadDispatcher"/> and <see cref="BumpGate"/> —
    /// whether a bump is accepted (200) or ignored (409). Lives in the root scope for the whole session.
    /// A busy port is logged and the game keeps running without the webhook.
    /// </summary>
    public sealed class BumpHttpServer : IStartable, IDisposable
    {
        private const string LogTag = "[BumpHttpServer]";

        private readonly BumpServerOptions _options;
        private readonly MainThreadDispatcher _dispatcher;
        private readonly BumpGate _gate;
        private readonly CancellationTokenSource _lifetime = new();
        private readonly List<TcpListener> _listeners = new();
        private readonly List<Thread> _acceptThreads = new();

        public BumpHttpServer(BumpServerOptions options, MainThreadDispatcher dispatcher, BumpGate gate)
        {
            _options = options;
            _dispatcher = dispatcher;
            _gate = gate;
        }

        public bool IsListening => _listeners.Count > 0;

        public void Start()
        {
            TryListen(IPAddress.Loopback, isRequired: true);
            if (Socket.OSSupportsIPv6)
                TryListen(IPAddress.IPv6Loopback, isRequired: false);

            if (IsListening)
                Debug.Log($"{LogTag} Listening on http://localhost:{_options.Port}{BumpEndpoint.Path}");
        }

        public void Dispose()
        {
            if (_lifetime.IsCancellationRequested)
                return;

            _lifetime.Cancel();
            foreach (TcpListener listener in _listeners)
                listener.Stop();

            foreach (Thread thread in _acceptThreads)
                thread.Join(TimeSpan.FromSeconds(1));

            // _lifetime is intentionally not disposed: in-flight connections may still link to its token.
            _listeners.Clear();
            _acceptThreads.Clear();
        }

        private void TryListen(IPAddress address, bool isRequired)
        {
            var listener = new TcpListener(address, _options.Port);
            try
            {
                listener.Start();
            }
            catch (SocketException exception)
            {
                // Port taken or address family unavailable: the game must keep running without the webhook.
                string message = $"{LogTag} Cannot listen on {address}:{_options.Port} ({exception.SocketErrorCode}).";
                if (isRequired)
                    Debug.LogError(message);
                else
                    Debug.LogWarning(message);
                return;
            }

            var thread = new Thread(() => AcceptLoop(listener))
            {
                IsBackground = true,
                Name = $"BumpHttpServer {address}"
            };

            _listeners.Add(listener);
            _acceptThreads.Add(thread);
            thread.Start();
        }

        private void AcceptLoop(TcpListener listener)
        {
            CancellationToken lifetime = _lifetime.Token;
            while (!lifetime.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (Exception exception) when (exception is SocketException or ObjectDisposedException or InvalidOperationException)
                {
                    // Listener stopped by Dispose.
                    break;
                }

                // Handle each connection on the thread pool so slow clients never block accepting.
                _ = Task.Run(() => HandleClientAsync(client, lifetime));
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken lifetime)
        {
            using (client)
            using (var connection = CancellationTokenSource.CreateLinkedTokenSource(lifetime))
            {
                connection.CancelAfter(_options.ConnectionTimeout);

                // Socket reads ignore cancellation tokens on some runtimes; closing the client unblocks them.
                using (connection.Token.Register(client.Close))
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        BumpResponse response = await ProcessRequestAsync(stream, connection.Token).ConfigureAwait(false);

                        byte[] bytes = response.ToBytes();
                        await stream.WriteAsync(bytes, 0, bytes.Length, connection.Token).ConfigureAwait(false);
                        client.Client.Shutdown(SocketShutdown.Send);
                    }
                    catch (Exception exception) when (IsConnectionFailure(exception))
                    {
                        // Client disconnected, timed out, or the server is shutting down: nobody to answer.
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }
        }

        private async Task<BumpResponse> ProcessRequestAsync(Stream stream, CancellationToken cancellationToken)
        {
            HttpRequestReader.HeadReadResult read =
                await HttpRequestReader.ReadHeadAsync(stream, cancellationToken).ConfigureAwait(false);

            if (!BumpRequestParser.TryParse(read.Head, out HttpRequestHead request))
                return BumpResponse.BadRequest;

            int unreadBody = request.ContentLength - read.BodyBytesRead;
            await HttpRequestReader.DrainBodyAsync(stream, unreadBody, cancellationToken).ConfigureAwait(false);

            BumpRoute route = BumpEndpoint.Route(request);
            if (route != BumpRoute.Bump)
                return BumpResponse.ForRejectedRoute(route);

            return await DecideOnMainThreadAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<BumpResponse> DecideOnMainThreadAsync(CancellationToken cancellationToken)
        {
            try
            {
                bool triggered = await _dispatcher
                    .InvokeAsync(_gate.TryTrigger, _options.MainThreadTimeout, cancellationToken)
                    .ConfigureAwait(false);
                return triggered ? BumpResponse.Triggered : BumpResponse.NotPlaying;
            }
            catch (TimeoutException)
            {
                return BumpResponse.MainThreadTimeout;
            }
            catch (Exception exception) when (!IsConnectionFailure(exception))
            {
                // A gameplay subscriber threw; the bump was accepted but report the failure to the caller.
                Debug.LogException(exception);
                return BumpResponse.InternalError;
            }
        }

        private static bool IsConnectionFailure(Exception exception) =>
            exception is IOException or SocketException or ObjectDisposedException or OperationCanceledException;
    }
}
