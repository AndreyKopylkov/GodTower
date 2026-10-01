using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GodTower.Core;
using GodTower.Effects;
using GodTower.Events;
using GodTower.Levels;
using GodTower.Scopes;
using GodTower.UI;
using GodTower.Webhook;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using Random = System.Random;

namespace GodTower.Tests.PlayMode
{
    /// <summary>
    /// U-76 full flow, the way a player goes through the game: fresh progress → Menu → Play → Level 1 … Level 5, each level
    /// played by <see cref="ClimbBot"/> while "commentators" send <c>/bump</c> over real HTTP every 4–10 s (level clock),
    /// Next on every win screen, "CHAMPION!" after Level 5 (no Next), back to the Menu with every level unlocked.
    /// Checked on the way: unlocks after each win, every in-play <c>/bump</c> = 200, <c>/bump</c> = 409 on result screens
    /// and in the menu, no error or exception logged. Levels run at <see cref="TimeScale"/>× (≈ 3 min real time).
    /// </summary>
    public sealed class FullFlowPlayModeTests : InputTestFixture
    {
        private const float TimeScale = 4f;
        private const float MinBumpInterval = 4f;
        private const float MaxBumpInterval = 10f;
        private const float LevelRealTimeBudget = 90f;
        private static readonly string BumpUrl = $"http://localhost:{BumpServerOptions.DefaultPort}/bump";

        private readonly List<string> _errors = new();
        private HttpClient _http;
        private int _savedProgress;

        private static IObjectResolver Root => VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance().Container;

        public override void Setup()
        {
            base.Setup();
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            _errors.Clear();
            Application.logMessageReceived += CollectErrors;
            _savedProgress = PlayerPrefs.GetInt(PlayerPrefsProgressStore.Key, 0);
            // Start from a fresh install: nothing completed (stored value and the root service's cached count).
            PlayerPrefs.SetInt(PlayerPrefsProgressStore.Key, 0);
            SetCompletedCount(Root.Resolve<ProgressService>(), 0);
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= CollectErrors;
            Time.timeScale = 1f;
            _http.Dispose();
            PlayerPrefs.SetInt(PlayerPrefsProgressStore.Key, _savedProgress);
            SetCompletedCount(Root.Resolve<ProgressService>(), _savedProgress);
            // Other tests load the Game scene directly and expect Level 1.
            Root.Resolve<SelectedLevel>().Select(0);
            base.TearDown();
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Menu_PlaysAllFiveLevels_WithUnlocksAndBumps() => UniTask.ToCoroutine(async () =>
        {
            var progress = Root.Resolve<ProgressService>();
            var flow = Root.Resolve<ISceneFlow>();

            await SceneManager.LoadSceneAsync(TestScenes.Menu);
            MenuView menu = UnityEngine.Object.FindAnyObjectByType<MenuView>();
            int levelCount = menu.LevelButtons.Count;
            Assert.That(levelCount, Is.EqualTo(5));
            await UniTask.Delay(TimeSpan.FromSeconds(0.3), ignoreTimeScale: true); // presenters bind on Start
            Assert.That(menu.LevelButtons.Select(b => b.IsUnlocked), Is.EqualTo(new[] { true, false, false, false, false }),
                "Fresh progress: only Level 1 is unlocked.");
            Assert.That(await PostBumpAsync(), Is.EqualTo(409), "Menu: /bump is ignored.");

            // Play = first unfinished level = Level 1.
            await ClickAsync(menu.PlayButton);
            var log = new List<string>();
            Mouse mouse = null;
            for (int number = 1; number <= levelCount; number++)
            {
                LevelRunner runner = await WaitForLevelAsync(flow, number);
                if (mouse != null)
                    InputSystem.RemoveDevice(mouse);
                mouse = InputSystem.AddDevice<Mouse>(); // after the scene load, see GameFlowPlayModeTests

                log.Add(await PlayLevelAsync(runner, mouse));
                Assert.That(progress.CompletedCount, Is.EqualTo(number), $"Winning Level {number} unlocks the next one.");

                ResultPanelView result = Scope().Container.Resolve<ResultPanelView>();
                await WaitAsync(() => result.IsVisible, 10f, "result panel");
                await UniTask.Delay(TimeSpan.FromSeconds(0.6), ignoreTimeScale: true);
                Assert.That(await PostBumpAsync(), Is.EqualTo(409), "Result screen: /bump is ignored.");

                bool last = number == levelCount;
                Assert.That(result.NextButton.gameObject.activeSelf, Is.EqualTo(!last), "Next on every win but the last.");
                if (!last)
                {
                    await ClickAsync(result.NextButton);
                    continue;
                }

                // Level 5 won: "CHAMPION!" panel with Retry / Menu.
                Assert.That(result.RetryButton.gameObject.activeSelf, Is.True);
                ScreenshotCapture.Save("FullFlow_Champion");
                await ClickAsync(result.MenuButton);
            }

            await WaitAsync(() => !flow.IsLoading && SceneManager.GetActiveScene().name == TestScenes.Menu, 10f, "menu scene");
            menu = UnityEngine.Object.FindAnyObjectByType<MenuView>();
            await UniTask.Delay(TimeSpan.FromSeconds(0.3), ignoreTimeScale: true);
            Assert.That(menu.LevelButtons.All(b => b.IsUnlocked), Is.True, "All levels unlocked after the run.");
            Assert.That(PlayerPrefs.GetInt(PlayerPrefsProgressStore.Key), Is.EqualTo(levelCount), "Progress is saved.");
            Assert.That(await PostBumpAsync(), Is.EqualTo(409));
            Assert.That(_errors, Is.Empty, "No errors or exceptions in the log.");
            Debug.Log("[FullFlow]\n" + string.Join("\n", log));
        });

        /// <summary>Bot + commentators until the level ends; returns a one-line summary. The level must be won.</summary>
        private async UniTask<string> PlayLevelAsync(LevelRunner runner, Mouse mouse)
        {
            IObjectResolver container = Scope().Container;
            var director = container.Resolve<EventDirector>();
            var bumps = container.Resolve<BumpDirector>();
            var bot = new ClimbBot(this, mouse, runner, director);
            int bumpHits = 0;
            bumps.Hit += _ => bumpHits++;

            int number = runner.Level.Number;
            var commentators = new Random(104729 * number);
            float nextBump = NextInterval(commentators);
            var requests = new List<Task<(int Status, bool InPlay)>>();

            await UniTask.DelayFrame(3);
            bot.Hold();
            Time.timeScale = TimeScale;
            float realStart = Time.realtimeSinceStartup;
            while (runner.Outcome == null && Time.realtimeSinceStartup - realStart < LevelRealTimeBudget)
            {
                await bot.StepAsync();
                if (runner.IsRunning && runner.Elapsed >= nextBump)
                {
                    requests.Add(SendBumpAsync(runner));
                    nextBump = runner.Elapsed + NextInterval(commentators);
                }

                await UniTask.Yield();
            }

            Time.timeScale = 1f;
            bot.Release();
            (int Status, bool InPlay)[] results = await Task.WhenAll(requests);
            string summary = $"Level {number}: {runner.Outcome?.ToString() ?? "unfinished"} at {runner.Elapsed:0.0}/{runner.Level.TimeLimit:0} s, " +
                             $"bumps {requests.Count} (hits {bumpHits}).";

            Assert.That(runner.Outcome, Is.EqualTo(LevelOutcome.Won), summary);
            Assert.That(requests, Is.Not.Empty, "Commentators sent bumps.");
            Assert.That(results.Where(r => r.InPlay).Select(r => r.Status), Is.All.EqualTo(200), summary);
            Assert.That(bumpHits, Is.GreaterThan(0), "Accepted bumps hit the climber.");
            return summary;
        }

        private static GameLifetimeScope Scope() => UnityEngine.Object.FindAnyObjectByType<GameLifetimeScope>();

        private static float NextInterval(Random random) =>
            MinBumpInterval + (float)random.NextDouble() * (MaxBumpInterval - MinBumpInterval);

        /// <summary>
        /// The root <see cref="ProgressService"/> lives for the whole test run and caches the completed count, so a fresh
        /// install is simulated by rewriting it (test-only; the game has no "reset progress" feature).
        /// </summary>
        private static void SetCompletedCount(ProgressService progress, int count) =>
            typeof(ProgressService).GetProperty(nameof(ProgressService.CompletedCount), BindingFlags.Instance | BindingFlags.Public)
                .SetValue(progress, count);

        private static async UniTask<LevelRunner> WaitForLevelAsync(ISceneFlow flow, int expectedNumber)
        {
            await UniTask.Yield();
            await WaitAsync(() => !flow.IsLoading && SceneManager.GetActiveScene().name == TestScenes.Game &&
                                  Scope() != null && Scope().Container.Resolve<LevelRunner>().Climber != null, 15f, $"Level {expectedNumber}");
            var runner = Scope().Container.Resolve<LevelRunner>();
            Assert.That(runner.Level.Number, Is.EqualTo(expectedNumber));
            return runner;
        }

        private static async UniTask WaitAsync(Func<bool> condition, float seconds, string what)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > seconds)
                    throw new TimeoutException($"Timed out waiting for the {what}.");
                await UniTask.Yield();
            }
        }

        /// <summary>EventSystem raycast at the button centre must hit it first, then the click is dispatched (see GameFlowPlayModeTests).</summary>
        private static async UniTask ClickAsync(Button button)
        {
            await UniTask.Yield();
            var rect = (RectTransform)button.transform;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = screen, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, $"Nothing under {button.name}.");
            Assert.That(hits[0].gameObject.transform.IsChildOf(button.transform), Is.True, $"{hits[0].gameObject.name} blocks {button.name}.");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            await UniTask.Yield();
        }

        private async Task<(int Status, bool InPlay)> SendBumpAsync(LevelRunner runner)
        {
            bool playing = runner.IsRunning;
            using HttpResponseMessage response = await _http.PostAsync(BumpUrl, null);
            return ((int)response.StatusCode, playing && runner.Outcome == null);
        }

        private async UniTask<int> PostBumpAsync()
        {
            using HttpResponseMessage response = await _http.PostAsync(BumpUrl, null);
            return (int)response.StatusCode;
        }

        private void CollectErrors(string message, string stackTrace, LogType type)
        {
            if (type is LogType.Error or LogType.Exception or LogType.Assert)
                _errors.Add($"{type}: {message}");
        }
    }
}
