using System;
using System.Collections;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using GodTower.Core;
using GodTower.Gameplay;
using GodTower.Levels;
using GodTower.Scopes;
using GodTower.Webhook;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;

namespace GodTower.Tests.PlayMode
{
    /// <summary>
    /// Plays Level 1 headless with a simulated mouse (Input System test fixture): hold climbs, release hangs,
    /// swipes change lanes, the webhook answers 200 only while the level is running.
    /// </summary>
    public sealed class LevelPlayModeTests : InputTestFixture
    {
        private static readonly string BumpUrl = $"http://localhost:{BumpServerOptions.DefaultPort}/bump";

        private Mouse _mouse;
        private HttpClient _http;

        public override void Setup()
        {
            base.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        }

        public override void TearDown()
        {
            _http.Dispose();
            Time.timeScale = 1f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Level1_HoldClimbs_ReleaseHangs_BumpAnswers200WhilePlaying() => UniTask.ToCoroutine(async () =>
        {
            LevelRunner runner = await LoadGameAsync();
            PlayStateService playState = RootResolve<PlayStateService>();

            Assert.That(runner.Level.Number, Is.EqualTo(1));
            Assert.That(playState.Current, Is.EqualTo(PlayState.Playing));
            Assert.That(runner.Climber.Height, Is.Zero);

            Press(_mouse.leftButton);
            await UniTask.Delay(TimeSpan.FromSeconds(1.5));
            float climbed = runner.Climber.Height;
            Assert.That(climbed, Is.GreaterThan(5f), "Holding must climb at ~6 m/s.");
            Assert.That(runner.Climber.State, Is.EqualTo(ClimberState.Climb));

            Release(_mouse.leftButton);
            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            float hanging = runner.Climber.Height;
            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            Assert.That(runner.Climber.Height, Is.EqualTo(hanging).Within(0.01f), "Released: hang in place.");
            Assert.That(runner.Climber.State, Is.EqualTo(ClimberState.Idle));

            Assert.That(await PostBumpAsync(), Is.EqualTo(200), "A running level accepts bumps.");

            runner.Pause();
            Assert.That(playState.Current, Is.EqualTo(PlayState.Paused));
            Assert.That(await PostBumpAsync(), Is.EqualTo(409), "A paused level ignores bumps.");
            runner.Resume();
            Assert.That(await PostBumpAsync(), Is.EqualTo(200));

            await SceneManager.LoadSceneAsync(TestScenes.Menu);
            Assert.That(playState.Current, Is.EqualTo(PlayState.Menu));
        });

        [UnityTest]
        public IEnumerator Level1_SwipesChangeLanes_WhileHolding() => UniTask.ToCoroutine(async () =>
        {
            LevelRunner runner = await LoadGameAsync();
            float width = Screen.width;
            float y = Screen.height * 0.5f;

            Set(_mouse.position, new Vector2(width * 0.5f, y));
            Press(_mouse.leftButton);
            await UniTask.DelayFrame(3);

            Set(_mouse.position, new Vector2(width * 0.8f, y));
            await UniTask.Delay(TimeSpan.FromSeconds(0.4));
            Assert.That(runner.Climber.Lanes.Index, Is.EqualTo(2), "Swipe right while holding.");
            Assert.That(runner.Climber.LaneAngle, Is.EqualTo(35f));
            Assert.That(runner.Climber.Height, Is.GreaterThan(0f), "Still climbing during the swipe.");

            Set(_mouse.position, new Vector2(width * 0.5f, y));
            await UniTask.Delay(TimeSpan.FromSeconds(0.4));
            Set(_mouse.position, new Vector2(width * 0.2f, y));
            await UniTask.Delay(TimeSpan.FromSeconds(0.4));
            Assert.That(runner.Climber.Lanes.Index, Is.Zero, "Two swipes left.");

            Release(_mouse.leftButton);
            await SceneManager.LoadSceneAsync(TestScenes.Menu);
        });

        [UnityTest]
        public IEnumerator Level_TimeOut_Loses_AndBumpReturns409OnResult() => UniTask.ToCoroutine(async () =>
        {
            LevelRunner runner = await LoadGameAsync();
            LevelOutcome? outcome = null;
            runner.Ended += result => outcome = result;

            // Nobody climbs: speed the clock up until the time limit runs out.
            Time.timeScale = 20f;
            await UniTask.WaitUntil(() => outcome != null).Timeout(TimeSpan.FromSeconds(30), DelayType.Realtime);
            Time.timeScale = 1f;

            Assert.That(outcome, Is.EqualTo(LevelOutcome.Lost));
            Assert.That(runner.Climber.State, Is.EqualTo(ClimberState.Lose));
            Assert.That(RootResolve<PlayStateService>().Current, Is.EqualTo(PlayState.Result));
            Assert.That(await PostBumpAsync(), Is.EqualTo(409));

            await SceneManager.LoadSceneAsync(TestScenes.Menu);
        });

        private static async UniTask<LevelRunner> LoadGameAsync()
        {
            await SceneManager.LoadSceneAsync(TestScenes.Game);
            var scope = UnityEngine.Object.FindAnyObjectByType<GameLifetimeScope>();
            var runner = scope.Container.Resolve<LevelRunner>();
            await UniTask.WaitUntil(() => runner.Climber != null);
            return runner;
        }

        private static T RootResolve<T>() =>
            VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance().Container.Resolve<T>();

        private async UniTask<int> PostBumpAsync()
        {
            using HttpResponseMessage response = await _http.PostAsync(BumpUrl, null);
            return (int)response.StatusCode;
        }
    }
}
