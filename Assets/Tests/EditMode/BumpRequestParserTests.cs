using GodTower.Webhook;
using NUnit.Framework;

namespace GodTower.Tests.EditMode
{
    public sealed class BumpRequestParserTests
    {
        [Test]
        public void Parses_BareGetRequestLine()
        {
            Assert.That(BumpRequestParser.TryParse("GET /bump HTTP/1.1", out HttpRequestHead request), Is.True);
            Assert.That(request.Method, Is.EqualTo("GET"));
            Assert.That(request.Path, Is.EqualTo("/bump"));
            Assert.That(request.ContentLength, Is.Zero);
        }

        [Test]
        public void Parses_CurlPostWithHeaders()
        {
            const string head = "POST /bump HTTP/1.1\r\n" +
                                "Host: localhost:56789\r\n" +
                                "User-Agent: curl/8.4.0\r\n" +
                                "Accept: */*\r\n" +
                                "Content-Type: application/json\r\n" +
                                "Content-Length: 17";

            Assert.That(BumpRequestParser.TryParse(head, out HttpRequestHead request), Is.True);
            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.Path, Is.EqualTo("/bump"));
            Assert.That(request.ContentLength, Is.EqualTo(17));
        }

        [Test]
        public void Parses_HeaderNamesCaseInsensitively()
        {
            Assert.That(BumpRequestParser.TryParse("POST /bump HTTP/1.1\r\ncontent-length: 3", out HttpRequestHead request), Is.True);
            Assert.That(request.ContentLength, Is.EqualTo(3));
        }

        [TestCase("GET /bump?source=chat&n=2 HTTP/1.1", "/bump")]
        [TestCase("GET /bump#fragment HTTP/1.1", "/bump")]
        [TestCase("GET /bump/ HTTP/1.1", "/bump/")]
        [TestCase("GET http://localhost:56789/bump?x=1 HTTP/1.1", "/bump")]
        [TestCase("GET / HTTP/1.0", "/")]
        public void Extracts_PathWithoutQuery(string head, string expectedPath)
        {
            Assert.That(BumpRequestParser.TryParse(head, out HttpRequestHead request), Is.True);
            Assert.That(request.Path, Is.EqualTo(expectedPath));
        }

        [TestCase("get", "GET")]
        [TestCase("Post", "POST")]
        [TestCase("delete", "DELETE")]
        public void Normalizes_MethodToUpperCase(string method, string expected)
        {
            Assert.That(BumpRequestParser.TryParse(method + " /bump HTTP/1.1", out HttpRequestHead request), Is.True);
            Assert.That(request.Method, Is.EqualTo(expected));
        }

        [Test]
        public void Accepts_LineFeedOnlyLineEndings()
        {
            Assert.That(BumpRequestParser.TryParse("POST /bump HTTP/1.1\nHost: localhost\nContent-Length: 2", out HttpRequestHead request), Is.True);
            Assert.That(request.ContentLength, Is.EqualTo(2));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("hello world")]
        [TestCase("GET /bump")]
        [TestCase("GET  /bump HTTP/1.1")]
        [TestCase("GET /bump HTTP/1.1 extra")]
        [TestCase("GET /bump FTP/1.0")]
        [TestCase("G(E)T /bump HTTP/1.1")]
        [TestCase("GET bump HTTP/1.1")]
        [TestCase("GET ftp://localhost/bump HTTP/1.1")]
        [TestCase("\u0016\u0003\u0001\u0002\u0000\u0001")]
        public void Rejects_MalformedRequestLine(string head)
        {
            Assert.That(BumpRequestParser.TryParse(head, out _), Is.False);
        }

        [TestCase("POST /bump HTTP/1.1\r\nno colon here")]
        [TestCase("POST /bump HTTP/1.1\r\n: empty name")]
        [TestCase("POST /bump HTTP/1.1\r\nBad Name: value")]
        [TestCase("POST /bump HTTP/1.1\r\nContent-Length: abc")]
        [TestCase("POST /bump HTTP/1.1\r\nContent-Length: -5")]
        public void Rejects_MalformedHeaders(string head)
        {
            Assert.That(BumpRequestParser.TryParse(head, out _), Is.False);
        }
    }
}
