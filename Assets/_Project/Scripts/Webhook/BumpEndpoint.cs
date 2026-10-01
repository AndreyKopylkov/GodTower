namespace GodTower.Webhook
{
    /// <summary>What the server should do with a well-formed request.</summary>
    public enum BumpRoute
    {
        /// <summary>A valid bump request; the answer depends on the play state.</summary>
        Bump,
        NotFound,
        MethodNotAllowed
    }

    /// <summary>Routing rules of the webhook: <c>GET</c> or <c>POST</c> on <c>/bump</c>.</summary>
    public static class BumpEndpoint
    {
        public const string Path = "/bump";
        public const string AllowedMethods = "GET, POST";

        public static BumpRoute Route(in HttpRequestHead request)
        {
            if (request.Path != Path)
                return BumpRoute.NotFound;

            return request.Method is "GET" or "POST" ? BumpRoute.Bump : BumpRoute.MethodNotAllowed;
        }
    }
}
