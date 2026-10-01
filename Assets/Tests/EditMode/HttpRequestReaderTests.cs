using System.IO;
using System.Text;
using System.Threading;
using GodTower.Webhook;
using NUnit.Framework;

namespace GodTower.Tests.EditMode
{
    public sealed class HttpRequestReaderTests
    {
        [Test]
        public void ReadsHead_UpToBlankLine_AndCountsBufferedBody()
        {
            Stream stream = StreamOf("POST /bump HTTP/1.1\r\nContent-Length: 4\r\n\r\nbody");

            HttpRequestReader.HeadReadResult result = ReadHead(stream);

            Assert.That(result.Head, Is.EqualTo("POST /bump HTTP/1.1\r\nContent-Length: 4"));
            Assert.That(result.BodyBytesRead, Is.EqualTo(4));
        }

        [Test]
        public void ReadsHead_WithLineFeedOnlyTerminator()
        {
            HttpRequestReader.HeadReadResult result = ReadHead(StreamOf("GET /bump HTTP/1.1\nHost: x\n\n"));

            Assert.That(result.Head, Is.EqualTo("GET /bump HTTP/1.1\nHost: x"));
            Assert.That(result.BodyBytesRead, Is.Zero);
        }

        [Test]
        public void ReturnsEverything_WhenPeerStopsBeforeBlankLine()
        {
            Assert.That(ReadHead(StreamOf("GET /bump HTTP/1.1")).Head, Is.EqualTo("GET /bump HTTP/1.1"));
        }

        [Test]
        public void ReturnsNullHead_ForEmptyStream()
        {
            Assert.That(ReadHead(new MemoryStream()).Head, Is.Null);
        }

        [Test]
        public void ReturnsNullHead_WhenHeadExceedsLimit()
        {
            string oversized = "GET /bump HTTP/1.1\r\nX-Padding: " + new string('a', HttpRequestReader.MaxHeadBytes) + "\r\n\r\n";
            Assert.That(ReadHead(StreamOf(oversized)).Head, Is.Null);
        }

        [Test]
        public void DrainBody_ConsumesRemainingBytes()
        {
            var stream = new MemoryStream(new byte[100]);

            HttpRequestReader.DrainBodyAsync(stream, 60, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(stream.Position, Is.EqualTo(60));
        }

        [Test]
        public void DrainBody_StopsAtEndOfStream()
        {
            var stream = new MemoryStream(new byte[10]);

            HttpRequestReader.DrainBodyAsync(stream, 1000, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(stream.Position, Is.EqualTo(10));
        }

        private static HttpRequestReader.HeadReadResult ReadHead(Stream stream) =>
            HttpRequestReader.ReadHeadAsync(stream, CancellationToken.None).GetAwaiter().GetResult();

        private static Stream StreamOf(string text) => new MemoryStream(Encoding.ASCII.GetBytes(text));
    }
}
