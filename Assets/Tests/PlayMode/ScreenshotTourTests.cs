using System;
using System.Collections;
using System.Linq;
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
using VContainer.Unity;

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

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            SelectedLevel.Select(0); // other tests load the Game scene directly and expect Level 1
        }

        private static SelectedLevel SelectedLevel =>
            VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance().Container.Resolve<SelectedLevel>();

        [UnityTest]
        public IEnumerator BumpWave() => UniTask.ToCoroutine(async () =>
        {
            (IObjectResolver container, LevelRunner runner) = await LoadLevelAsync(0);
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

            // A telegraphed missile, just before it lands.
            var director = container.Resolve<EventDirector>();
            VillainStrike strike = director.Timeline.Strikes[0];
            Time.timeScale = 8f;
            await UniTask.WaitUntil(() => runner.Elapsed >= strike.ImpactTime - 0.3f);
            Time.timeScale = 1f;
            ScreenshotCapture.Save("Villain_Telegraph");

            // Level 1 jetpack boost (t = 30 s), fast-forwarded.
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

        /// <summary>Every level's sky preset (Day → Dusk) a few seconds in, then the Level 4 phoenix boost.</summary>
        [UnityTest]
        public IEnumerator SkyPresetsAndPhoenix() => UniTask.ToCoroutine(async () =>
        {
            for (int index = 0; index < 5; index++)
            {
                (IObjectResolver _, LevelRunner runner) = await LoadLevelAsync(index);
                runner.Climber.Carry(60f, 0.5f);
                await UniTask.Delay(TimeSpan.FromSeconds(1.2));
                ScreenshotCapture.Save($"Sky_{index + 1:00}");
            }

            (IObjectResolver container, LevelRunner level4) = await LoadLevelAsync(3);
            HeroBoost phoenix = container.Resolve<EventDirector>().Timeline.Boosts.First(boost => boost.Kind == HeroKind.Phoenix);
            Time.timeScale = 8f;
            await UniTask.WaitUntil(() => level4.Elapsed >= phoenix.Time + 0.1f);
            Time.timeScale = 1f;
            foreach (float at in new[] { 0.3f, 1.2f, 2.4f })
            {
                await UniTask.WaitUntil(() => level4.Elapsed >= phoenix.Time + at);
                ScreenshotCapture.Save($"Boost_Phoenix_{at * 1000f:0000}ms");
            }

            await SceneManager.LoadSceneAsync(TestScenes.Menu);
            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            ScreenshotCapture.Save("Menu_Sky");
        });

        private static async UniTask<(IObjectResolver, LevelRunner)> LoadLevelAsync(int index)
        {
            SelectedLevel.Select(index);
            await SceneManager.LoadSceneAsync(TestScenes.Game);
            IObjectResolver container = UnityEngine.Object.FindAnyObjectByType<GameLifetimeScope>().Container;
            var runner = container.Resolve<LevelRunner>();
            await UniTask.WaitUntil(() => runner.Climber != null);
            return (container, runner);
        }
    }
}
