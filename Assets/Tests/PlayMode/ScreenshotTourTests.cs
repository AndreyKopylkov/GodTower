using System;
using System.Collections;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using GodTower.Effects;
using GodTower.Levels;
using GodTower.Scopes;
using GodTower.Webhook;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;

namespace GodTower.Tests.PlayMode
{
    /// <summary>
    /// Screenshot tour for visual review (<see cref="ScreenshotCapture"/>), not a regression test: run it explicitly with
    /// <c>-testFilter GodTower.Tests.PlayMode.ScreenshotTourTests</c> (without <c>-nographics</c>).
    /// </summary>
    [Explicit("Visual review only: renders screenshots to TestResults/Screenshots.")]
    public sealed class ScreenshotTourTests
    {
        private static readonly string BumpUrl = $"http://localhost:{BumpServerOptions.DefaultPort}/bump";

        [UnityTest]
        public IEnumerator BumpWave() => UniTask.ToCoroutine(async () =>
        {
            await SceneManager.LoadSceneAsync(TestScenes.Game);
            IObjectResolver container = UnityEngine.Object.FindAnyObjectByType<GameLifetimeScope>().Container;
            var runner = container.Resolve<LevelRunner>();
            await UniTask.WaitUntil(() => runner.Climber != null);
            runner.Climber.Carry(30f, 0.5f);
            await UniTask.Delay(TimeSpan.FromSeconds(1.5));
            ScreenshotCapture.Save("Hud_Play");

            using var http = new HttpClient();
            await http.PostAsync(BumpUrl, null);
            float sent = Time.time;
            foreach (float at in new[] { 0.1f, 0.2f, 0.3f, 0.38f, 0.5f, 0.7f })
            {
                await UniTask.WaitUntil(() => Time.time - sent >= at);
                ScreenshotCapture.Save($"Bump_{at * 1000f:000}ms");
            }

            var bumps = container.Resolve<BumpDirector>();
            await UniTask.WaitUntil(() => bumps.Queue.IsIdle);
            await SceneManager.LoadSceneAsync(TestScenes.Menu);
        });
    }
}
