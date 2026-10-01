using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GodTower.Webhook
{
    /// <summary>Reads a raw HTTP/1.x request from a stream: the head as text, then discards the body.</summary>
    public static class HttpRequestReader
    {
        public const int MaxHeadBytes = 8 * 1024;
        public const int MaxBodyBytes = 64 * 1024;

        private static readonly byte[] HeadTerminator = { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
        private static readonly byte[] BareHeadTerminator = { (byte)'\n', (byte)'\n' };

        /// <summary>Result of <see cref="ReadHeadAsync"/>.</summary>
        public readonly struct HeadReadResult
        {
            /// <summary>Head text without the terminating blank line; <c>null</c> when nothing usable arrived.</summary>
            public readonly string Head;

            /// <summary>Bytes past the head that were already consumed from the stream (start of the body).</summary>
            public readonly int BodyBytesRead;

            public HeadReadResult(string head, int bodyBytesRead)
            {
                Head = head;
                BodyBytesRead = bodyBytesRead;
            }
        }

        /// <summary>
        /// Reads until the blank line that ends the headers. If the peer stops sending earlier, whatever arrived
        /// is returned as the head (lets a bare <c>"GET /bump HTTP/1.1"</c> work). Heads over
        /// <see cref="MaxHeadBytes"/> yield a <c>null</c> head.
        /// </summary>
        public static async Task<HeadReadResult> ReadHeadAsync(Stream stream, CancellationToken cancellationToken)
        {
            var buffer = new byte[MaxHeadBytes];
            int length = 0;

            while (length < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer, length, buffer.Length - length, cancellationToken)
                    .ConfigureAwait(false);
                if (read == 0)
                    return new HeadReadResult(length == 0 ? null : Decode(buffer, length), 0);

                length += read;

                if (TryFindTerminator(buffer, length, out int headLength, out int terminatorLength))
                    return new HeadReadResult(Decode(buffer, headLength), length - headLength - terminatorLength);
            }

            return new HeadReadResult(null, 0);
        }

        /// <summary>Reads and discards the rest of the body so closing the socket does not reset the connection.</summary>
        public static async Task DrainBodyAsync(Stream stream, int remainingBytes, CancellationToken cancellationToken)
        {
            int toDrain = Math.Min(remainingBytes, MaxBodyBytes);
            var scratch = new byte[Math.Min(Math.Max(toDrain, 1), 4096)];

            while (toDrain > 0)
            {
                int read = await stream.ReadAsync(scratch, 0, Math.Min(scratch.Length, toDrain), cancellationToken)
                    .ConfigureAwait(false);
                if (read == 0)
                    return;

                toDrain -= read;
            }
        }

        private static bool TryFindTerminator(byte[] buffer, int length, out int headLength, out int terminatorLength)
        {
            headLength = IndexOf(buffer, length, HeadTerminator);
            terminatorLength = HeadTerminator.Length;
            if (headLength >= 0)
                return true;

            headLength = IndexOf(buffer, length, BareHeadTerminator);
            terminatorLength = BareHeadTerminator.Length;
            return headLength >= 0;
        }

        private static int IndexOf(byte[] buffer, int length, byte[] pattern)
        {
            for (int i = 0; i <= length - pattern.Length; i++)
            {
                int matched = 0;
                while (matched < pattern.Length && buffer[i + matched] == pattern[matched])
                    matched++;

                if (matched == pattern.Length)
                    return i;
            }

            return -1;
        }

        private static string Decode(byte[] buffer, int length) => Encoding.ASCII.GetString(buffer, 0, length);
    }
}
