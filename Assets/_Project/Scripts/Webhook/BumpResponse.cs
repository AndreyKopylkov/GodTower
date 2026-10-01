using System.Text;

namespace GodTower.Webhook
{
    /// <summary>Immutable HTTP response of the webhook. Every response is JSON and closes the connection.</summary>
    public sealed class BumpResponse
    {
        public const string ContentType = "application/json; charset=utf-8";

        public static readonly BumpResponse Triggered =
            new(200, "OK", "{\"status\":\"triggered\"}");

        public static readonly BumpResponse NotPlaying =
            new(409, "Conflict", "{\"status\":\"ignored\",\"reason\":\"not_playing\"}");

        public static readonly BumpResponse BadRequest =
            new(400, "Bad Request", Error("bad_request"));

        public static readonly BumpResponse NotFound =
            new(404, "Not Found", Error("not_found"));

        public static readonly BumpResponse MethodNotAllowed =
            new(405, "Method Not Allowed", Error("method_not_allowed"), BumpEndpoint.AllowedMethods);

        public static readonly BumpResponse InternalError =
            new(500, "Internal Server Error", Error("internal_error"));

        /// <summary>The main thread did not answer in time (e.g. the app is suspended in the background).</summary>
        public static readonly BumpResponse MainThreadTimeout =
            new(503, "Service Unavailable", Error("main_thread_timeout"));

        public int StatusCode { get; }
        public string ReasonPhrase { get; }
        public string Body { get; }

        /// <summary>Value of the <c>Allow</c> header; only set for 405.</summary>
        public string Allow { get; }

        private BumpResponse(int statusCode, string reasonPhrase, string body, string allow = null)
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Body = body;
            Allow = allow;
        }

        /// <summary>Response for routes that are answered without asking the game (404, 405).</summary>
        public static BumpResponse ForRejectedRoute(BumpRoute route) => route switch
        {
            BumpRoute.NotFound => NotFound,
            BumpRoute.MethodNotAllowed => MethodNotAllowed,
            _ => InternalError
        };

        /// <summary>Full HTTP/1.1 response text: status line, headers, blank line, body.</summary>
        public string Serialize()
        {
            var builder = new StringBuilder(256)
                .Append("HTTP/1.1 ").Append(StatusCode).Append(' ').Append(ReasonPhrase).Append("\r\n")
                .Append("Content-Type: ").Append(ContentType).Append("\r\n")
                .Append("Content-Length: ").Append(Encoding.UTF8.GetByteCount(Body)).Append("\r\n")
                .Append("Connection: close\r\n");

            if (Allow != null)
                builder.Append("Allow: ").Append(Allow).Append("\r\n");

            return builder.Append("\r\n").Append(Body).ToString();
        }

        public byte[] ToBytes() => Encoding.UTF8.GetBytes(Serialize());

        private static string Error(string reason) => "{\"status\":\"error\",\"reason\":\"" + reason + "\"}";
    }
}
