using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GodTower.Webhook;
using NUnit.Framework;

namespace GodTower.Tests.EditMode
{
    public sealed class BumpResponseTests
    {
        private static IEnumerable<TestCaseData> AllResponses()
        {
            yield return new TestCaseData(BumpResponse.Triggered, 200, "OK", "{\"status\":\"triggered\"}");
            yield return new TestCaseData(BumpResponse.NotPlaying, 409, "Conflict", "{\"status\":\"ignored\",\"reason\":\"not_playing\"}");
            yield return new TestCaseData(BumpResponse.BadRequest, 400, "Bad Request", "{\"status\":\"error\",\"reason\":\"bad_request\"}");
            yield return new TestCaseData(BumpResponse.NotFound, 404, "Not Found", "{\"status\":\"error\",\"reason\":\"not_found\"}");
            yield return new TestCaseData(BumpResponse.MethodNotAllowed, 405, "Method Not Allowed", "{\"status\":\"error\",\"reason\":\"method_not_allowed\"}");
            yield return new TestCaseData(BumpResponse.InternalError, 500, "Internal Server Error", "{\"status\":\"error\",\"reason\":\"internal_error\"}");
            yield return new TestCaseData(BumpResponse.MainThreadTimeout, 503, "Service Unavailable", "{\"status\":\"error\",\"reason\":\"main_thread_timeout\"}");
        }

        [TestCaseSource(nameof(AllResponses))]
        public void Serializes_StatusLineAndBody(BumpResponse response, int code, string reason, string body)
        {
            ParsedResponse parsed = Parse(response.Serialize());

            Assert.That(parsed.StatusLine, Is.EqualTo($"HTTP/1.1 {code} {reason}"));
            Assert.That(parsed.Body, Is.EqualTo(body));
        }

        [TestCaseSource(nameof(AllResponses))]
        public void Serializes_JsonAndConnectionCloseHeaders(BumpResponse response, int code, string reason, string body)
        {
            ParsedResponse parsed = Parse(response.Serialize());

            Assert.That(parsed.Headers["Content-Type"], Is.EqualTo("application/json; charset=utf-8"));
            Assert.That(parsed.Headers["Connection"], Is.EqualTo("close"));
            Assert.That(int.Parse(parsed.Headers["Content-Length"]), Is.EqualTo(Encoding.UTF8.GetByteCount(body)));
        }

        [Test]
        public void MethodNotAllowed_AdvertisesAllowedMethods()
        {
            ParsedResponse parsed = Parse(BumpResponse.MethodNotAllowed.Serialize());
            Assert.That(parsed.Headers["Allow"], Is.EqualTo("GET, POST"));
        }

        [Test]
        public void OtherResponses_HaveNoAllowHeader()
        {
            Assert.That(Parse(BumpResponse.Triggered.Serialize()).Headers.ContainsKey("Allow"), Is.False);
        }

        [Test]
        public void ToBytes_IsUtf8OfSerializedText()
        {
            Assert.That(BumpResponse.NotPlaying.ToBytes(), Is.EqualTo(Encoding.UTF8.GetBytes(BumpResponse.NotPlaying.Serialize())));
        }

        [TestCase(BumpRoute.NotFound, 404)]
        [TestCase(BumpRoute.MethodNotAllowed, 405)]
        public void ForRejectedRoute_MapsRouteToResponse(BumpRoute route, int expectedCode)
        {
            Assert.That(BumpResponse.ForRejectedRoute(route).StatusCode, Is.EqualTo(expectedCode));
        }

        private static ParsedResponse Parse(string raw)
        {
            int split = raw.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            Assert.That(split, Is.GreaterThan(0), "Response has no header terminator.");

            string[] headLines = raw.Substring(0, split).Split(new[] { "\r\n" }, StringSplitOptions.None);
            Dictionary<string, string> headers = headLines.Skip(1)
                .Select(line => line.Split(new[] { ": " }, 2, StringSplitOptions.None))
                .ToDictionary(parts => parts[0], parts => parts[1]);

            return new ParsedResponse(headLines[0], headers, raw.Substring(split + 4));
        }

        private readonly struct ParsedResponse
        {
            public readonly string StatusLine;
            public readonly Dictionary<string, string> Headers;
            public readonly string Body;

            public ParsedResponse(string statusLine, Dictionary<string, string> headers, string body)
            {
                StatusLine = statusLine;
                Headers = headers;
                Body = body;
            }
        }
    }
}
