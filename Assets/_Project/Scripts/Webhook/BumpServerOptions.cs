using System;

namespace GodTower.Webhook
{
    /// <summary>Settings of <see cref="BumpHttpServer"/>. Defaults match the webhook contract of the brief.</summary>
    public sealed class BumpServerOptions
    {
        public const int DefaultPort = 56789;

        public int Port { get; set; } = DefaultPort;

        /// <summary>How long a request waits for the main thread to decide 200 vs 409.</summary>
        public TimeSpan MainThreadTimeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>Upper bound for a whole connection (read request, decide, write response).</summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }
}
