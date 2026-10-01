using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GodTower.Core;
using GodTower.Webhook;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;

namespace GodTower.Tests.PlayMode
{
    /// <summary>
    /// End-to-end webhook tests over real HTTP against the server started by the root scope.
    /// The play state is driven directly through <see cref="PlayStateService"/> (the level runner does it in the game).
    /// </summary>
    public sealed class BumpWebhookPlayModeTests
    {
        private static readonly string BaseUrl = $"http://localhost:{BumpServerOptions.DefaultPort}";

        private HttpClient _http;
        private PlayStateService _playState;
        private IBumpSignal _bumpSignal;
        private int _bumpsSignalled;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync(TestScenes.Menu);

            IObjectResolver root = VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance().Container;
            Assert.That(root.Resolve<BumpHttpServer>().IsListening, Is.True, "Webhook server is not listening.");

            _playState = root.Resolve<PlayStateService>();
            _bumpSignal = root.Resolve<IBumpSignal>();
            _bumpsSignalled = 0;
            _bumpSignal.BumpRequested += OnBumpRequested;

            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        }

        [TearDown]
        public void TearDown()
        {
            _bumpSignal.BumpRequested -= OnBumpRequested;
            _playState.Set(PlayState.Menu);
            _http.Dispose();
        }

        [UnityTest]
        public IEnumerator Bump_InMenu_Returns409() => UniTask.ToCoroutine(async () =>
        {
            Assert.That(_playState.Current, Is.EqualTo(PlayState.Menu));

            await AssertResponse(HttpMethod.Post, "/bump", 409, "{\"status\":\"ignored\",\"reason\":\"not_playing\"}");
            Assert.That(_bumpsSignalled, Is.Zero);
        });

        [UnityTest]
        public IEnumerator Bump_WhilePlaying_Returns200AndSignalsGameplay() => UniTask.ToCoroutine(async () =>
        {
            _playState.Set(PlayState.Playing);

            await AssertResponse(HttpMethod.Post, "/bump", 200, "{\"status\":\"triggered\"}");
            await AssertResponse(HttpMethod.Get, "/bump?from=test", 200, "{\"status\":\"triggered\"}");
            Assert.That(_bumpsSignalled, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator PostWithBody_WhilePlaying_Returns200() => UniTask.ToCoroutine(async () =>
        {
            _playState.Set(PlayState.Playing);

            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/bump")
            {
                Content = new StringContent("{\"user\":\"commentator\"}", Encoding.UTF8, "application/json")
            };
            using HttpResponseMessage response = await _http.SendAsync(request);

            Assert.That((int)response.StatusCode, Is.EqualTo(200));
        });

        [UnityTest]
        public IEnumerator Bump_WhilePausedOrOnResultScreen_Returns409() => UniTask.ToCoroutine(async () =>
        {
            _playState.Set(PlayState.Paused);
            await AssertResponse(HttpMethod.Post, "/bump", 409, "{\"status\":\"ignored\",\"reason\":\"not_playing\"}");

            _playState.Set(PlayState.Result);
            await AssertResponse(HttpMethod.Get, "/bump", 409, "{\"status\":\"ignored\",\"reason\":\"not_playing\"}");

            Assert.That(_bumpsSignalled, Is.Zero);
        });

        [UnityTest]
        public IEnumerator UnknownPath_Returns404() => UniTask.ToCoroutine(async () =>
        {
            _playState.Set(PlayState.Playing);

            await AssertResponse(HttpMethod.Post, "/punch", 404, "{\"status\":\"error\",\"reason\":\"not_found\"}");
            Assert.That(_bumpsSignalled, Is.Zero);
        });

        [UnityTest]
        public IEnumerator OtherMethod_Returns405WithAllowHeader() => UniTask.ToCoroutine(async () =>
        {
            _playState.Set(PlayState.Playing);

            using HttpResponseMessage response = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Put, BaseUrl + "/bump"));

            Assert.That((int)response.StatusCode, Is.EqualTo(405));
            Assert.That(response.Content.Headers.Allow, Is.EquivalentTo(new[] { "GET", "POST" }));
            Assert.That(_bumpsSignalled, Is.Zero);
        });

        [UnityTest]
        public IEnumerator MalformedRequest_Returns400() => UniTask.ToCoroutine(async () =>
        {
            string raw = await SendRawAsync("THIS IS NOT HTTP\r\n\r\n");

            StringAssert.StartsWith("HTTP/1.1 400 Bad Request\r\n", raw);
        });

        [UnityTest]
        public IEnumerator TenParallelRequests_AreAllAnswered() => UniTask.ToCoroutine(async () =>
        {
            _playState.Set(PlayState.Playing);

            HttpResponseMessage[] responses = await Task.WhenAll(
                Enumerable.Range(0, 10).Select(_ => _http.PostAsync(BaseUrl + "/bump", null)));

            Assert.That(responses.Select(response => (int)response.StatusCode), Is.All.EqualTo(200));
            Assert.That(_bumpsSignalled, Is.EqualTo(10));
            foreach (HttpResponseMessage response in responses)
                response.Dispose();
        });

        private void OnBumpRequested() => _bumpsSignalled++;

        private async Task AssertResponse(HttpMethod method, string path, int expectedStatus, string expectedBody)
        {
            using HttpResponseMessage response = await _http.SendAsync(new HttpRequestMessage(method, BaseUrl + path));
            string body = await response.Content.ReadAsStringAsync();

            Assert.That((int)response.StatusCode, Is.EqualTo(expectedStatus), $"{method} {path}");
            Assert.That(body, Is.EqualTo(expectedBody), $"{method} {path}");
            Assert.That(response.Content.Headers.ContentType.MediaType, Is.EqualTo("application/json"));
        }

        /// <summary>Sends raw bytes over TCP (HttpClient cannot produce malformed requests) and returns the raw reply.</summary>
        private static async Task<string> SendRawAsync(string request)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, BumpServerOptions.DefaultPort);

            NetworkStream stream = client.GetStream();
            byte[] bytes = Encoding.ASCII.GetBytes(request);
            await stream.WriteAsync(bytes, 0, bytes.Length);

            var reply = new StringBuilder();
            var buffer = new byte[1024];
            int read;
            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                reply.Append(Encoding.ASCII.GetString(buffer, 0, read));

            return reply.ToString();
        }
    }
}
