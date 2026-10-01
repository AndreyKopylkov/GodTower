using System;
using System.Collections;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using GodTower.Effects;
using GodTower.Events;
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

            // Level 1 jetpack boost (t = 30 s), fast-forwarded.
            var director = container.Resolve<EventDirector>();
            HeroBoost jetpack = director.Timeline.Boosts[0];
            Time.timeScale = 8f;
            await UniTask.WaitUntil(() => runner.Elapsed >= jetpack.Time + 0.8f);
            Time.timeScale = 1f;
            ScreenshotCapture.Save("Boost_Jetpack");

            // Win on the deck: trophy beside the climber, before the result panel.
            runner.Climber.Carry(runner.Climber.TopHeight, 0.5f);
            await UniTask.WaitUntil(() => runner.Outcome != null);
            await UniTask.Delay(TimeSpan.FromSeconds(1.6));
            ScreenshotCapture.Save("Win_Deck");
            await UniTask.Delay(TimeSpan.FromSeconds(1.2));
            ScreenshotCapture.Save("Win_Panel");
            await SceneManager.LoadSceneAsync(TestScenes.Menu);
        });
    }
}
