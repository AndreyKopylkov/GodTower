using GodTower.Webhook;
using NUnit.Framework;

namespace GodTower.Tests.EditMode
{
    public sealed class BumpEndpointTests
    {
        [TestCase("GET")]
        [TestCase("POST")]
        public void Routes_GetAndPostOnBump(string method)
        {
            Assert.That(BumpEndpoint.Route(new HttpRequestHead(method, "/bump", 0)), Is.EqualTo(BumpRoute.Bump));
        }

        [TestCase("PUT")]
        [TestCase("DELETE")]
        [TestCase("HEAD")]
        [TestCase("OPTIONS")]
        public void Rejects_OtherMethodsOnBump(string method)
        {
            Assert.That(BumpEndpoint.Route(new HttpRequestHead(method, "/bump", 0)), Is.EqualTo(BumpRoute.MethodNotAllowed));
        }

        [TestCase("GET", "/")]
        [TestCase("POST", "/bumps")]
        [TestCase("POST", "/Bump")]
        [TestCase("PUT", "/other")]
        public void Returns_NotFoundForOtherPaths(string method, string path)
        {
            Assert.That(BumpEndpoint.Route(new HttpRequestHead(method, path, 0)), Is.EqualTo(BumpRoute.NotFound));
        }
    }
}
