using System;
using System.Globalization;

namespace GodTower.Webhook
{
    /// <summary>
    /// Minimal HTTP/1.x request head parser. Pure C#, no I/O.
    /// Accepts the request line plus optional headers; only <c>Content-Length</c> is interpreted
    /// so the server can drain a POST body before answering.
    /// </summary>
    public static class BumpRequestParser
    {
        private const string HttpVersionPrefix = "HTTP/";
        private const string ContentLengthHeader = "Content-Length";
        private const string TokenSeparators = "\"(),/:;<=>?@[\\]{}";

        private static readonly string[] LineSeparators = { "\r\n", "\n" };
        private static readonly char[] PathTerminators = { '?', '#' };

        /// <param name="head">Request text up to (not including) the blank line that ends the headers.</param>
        /// <param name="request">The parsed request when the method returns <c>true</c>.</param>
        /// <returns><c>false</c> when the request line or a header is malformed.</returns>
        public static bool TryParse(string head, out HttpRequestHead request)
        {
            request = default;
            if (string.IsNullOrEmpty(head))
                return false;

            string[] lines = head.Split(LineSeparators, StringSplitOptions.None);
            if (!TryParseRequestLine(lines[0], out string method, out string path))
                return false;

            int contentLength = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Length == 0)
                    break;

                if (!TryParseHeader(line, out string name, out string value))
                    return false;

                bool isContentLength = name.Equals(ContentLengthHeader, StringComparison.OrdinalIgnoreCase);
                if (isContentLength && !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out contentLength))
                    return false;
            }

            request = new HttpRequestHead(method, path, contentLength);
            return true;
        }

        private static bool TryParseRequestLine(string line, out string method, out string path)
        {
            method = null;
            path = null;

            string[] parts = line.Split(' ');
            if (parts.Length != 3)
                return false;

            if (!IsToken(parts[0]) || !parts[2].StartsWith(HttpVersionPrefix, StringComparison.Ordinal))
                return false;

            // Methods are case-sensitive per RFC 9110; accepting "post" is harmless for a debug webhook.
            method = parts[0].ToUpperInvariant();
            return TryExtractPath(parts[1], out path);
        }

        /// <summary>Supports origin-form (<c>/bump?x=1</c>) and absolute-form (<c>http://localhost:56789/bump</c>).</summary>
        private static bool TryExtractPath(string target, out string path)
        {
            path = null;
            if (target.Length == 0)
                return false;

            if (target[0] == '/')
            {
                int end = target.IndexOfAny(PathTerminators);
                path = end < 0 ? target : target.Substring(0, end);
                return true;
            }

            bool isAbsoluteHttpUri = Uri.TryCreate(target, UriKind.Absolute, out Uri uri) &&
                                     (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            if (!isAbsoluteHttpUri)
                return false;

            path = uri.AbsolutePath;
            return true;
        }

        private static bool TryParseHeader(string line, out string name, out string value)
        {
            name = null;
            value = null;

            int colon = line.IndexOf(':');
            if (colon <= 0)
                return false;

            name = line.Substring(0, colon);
            if (!IsToken(name))
                return false;

            value = line.Substring(colon + 1).Trim();
            return true;
        }

        /// <summary>RFC 9110 token: visible ASCII except separators.</summary>
        private static bool IsToken(string text)
        {
            if (text.Length == 0)
                return false;

            foreach (char c in text)
            {
                bool isTokenChar = c > ' ' && c < 127 && TokenSeparators.IndexOf(c) < 0;
                if (!isTokenChar)
                    return false;
            }

            return true;
        }
    }
}
