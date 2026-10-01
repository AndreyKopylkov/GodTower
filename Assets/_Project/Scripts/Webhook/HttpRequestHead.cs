namespace GodTower.Webhook
{
    /// <summary>The parts of an HTTP/1.x request head the webhook cares about.</summary>
    public readonly struct HttpRequestHead
    {
        /// <summary>Upper-case method token, e.g. <c>GET</c>.</summary>
        public readonly string Method;

        /// <summary>Request path without query string or fragment, e.g. <c>/bump</c>.</summary>
        public readonly string Path;

        /// <summary>Value of the <c>Content-Length</c> header, 0 when absent.</summary>
        public readonly int ContentLength;

        public HttpRequestHead(string method, string path, int contentLength)
        {
            Method = method;
            Path = path;
            ContentLength = contentLength;
        }
    }
}
