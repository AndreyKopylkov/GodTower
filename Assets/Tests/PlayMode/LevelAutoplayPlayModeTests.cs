using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GodTower.Effects;
using GodTower.Events;
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
using Random = System.Random;

namespace GodTower.Tests.PlayMode
{
    /// <summary>
    /// M7 autoplay (U-70…U-75): every level is played start to finish by <see cref="ClimbBot"/> (hold to climb, swipe away
    /// from telegraphed lanes) while "commentators" send <c>/bump</c> over real HTTP at seeded random intervals of 4–10 s.
    /// Pass = Win before the timer, no error or exception logged, every in-play <c>/bump</c> answered 200.
    /// Screenshots (start, first villain, first bump, hero boost, win) go to <c>TestResults/Screenshots/Level_0N_*.png</c>.
    /// The level runs at <see cref="TimeScale"/>× speed: all gameplay advances on scaled delta time (frame-rate independent),
    /// and the commentator schedule uses the level clock, so the bump density per level second is the real one.
    /// </summary>
    public sealed class LevelAutoplayPlayModeTests : InputTestFixture
    {
        private const float TimeScale = 4f;
        private const float MinBumpInterval = 4f;
        private const float MaxBumpInterval = 10f;
        private const float RealTimeBudget = 90f;
        private static readonly string BumpUrl = $"http://localhost:{BumpServerOptions.DefaultPort}/bump";

        private readonly List<string> _errors = new();
        private HttpClient _http;

        public override void Setup()
        {
            base.Setup();
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            _errors.Clear();
            Application.logMessageReceived += CollectErrors;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= CollectErrors;
            Time.timeScale = 1f;
            _http.Dispose();
            // Other tests load the Game scene directly and expect Level 1.
            Root.Resolve<SelectedLevel>().Select(0);
            base.TearDown();
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator Level1_Autoplay_WinsWithBumps() => PlayLevel(1);

        [UnityTest, Timeout(180000)]
        public IEnumerator Level2_Autoplay_WinsWithBumps() => PlayLevel(2);

        [UnityTest, Timeout(180000)]
        public IEnumerator Level3_Autoplay_WinsWithBumps() => PlayLevel(3);

        [UnityTest, Timeout(180000)]
        public IEnumerator Level4_Autoplay_WinsWithBumps() => PlayLevel(4);

        [UnityTest, Timeout(180000)]
        public IEnumerator Level5_Autoplay_WinsWithBumps() => PlayLevel(5);

        private static IObjectResolver Root => VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance().Container;

        private IEnumerator PlayLevel(int number) => UniTask.ToCoroutine(async () =>
        {
            Root.Resolve<SelectedLevel>().Select(number - 1);
            await SceneManager.LoadSceneAsync(TestScenes.Game);
            IObjectResolver container = UnityEngine.Object.FindAnyObjectByType<GameLifetimeScope>().Container;
            var runner = container.Resolve<LevelRunner>();
            var director = container.Resolve<EventDirector>();
            var bumps = container.Resolve<BumpDirector>();
            await UniTask.WaitUntil(() => runner.Climber != null);
            Assert.That(runner.Level.Number, Is.EqualTo(number));

            // Added after the scene load (see GameFlowPlayModeTests: UI modules re-resolve devices on scene changes).
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            var bot = new ClimbBot(this, mouse, runner, director);
            var shots = new ScreenshotPlan($"Level_{number:00}");
            var landed = new List<(VillainStrike Strike, bool Hit)>();
            var boosts = new List<HeroBoost>();
            var bumpHits = new List<float>();
            director.Landed += (strike, hit) => landed.Add((strike, hit));
            director.BoostStarted += boosts.Add;
            bumps.Hit += bumpHits.Add;

            var commentators = new Random(7919 * number);
            float nextBump = NextInterval(commentators);
            var requests = new List<Task<BumpResult>>();

            // A few frames first: the Cinemachine brain places the camera in its LateUpdate.
            await UniTask.DelayFrame(3, PlayerLoopTiming.PostLateUpdate);
            shots.Take("Start");
            bot.Hold();
            Time.timeScale = TimeScale;
            float realStart = Time.realtimeSinceStartup;
            while (runner.Outcome == null && Time.realtimeSinceStartup - realStart < RealTimeBudget)
            {
                await bot.StepAsync();

                if (runner.IsRunning && runner.Elapsed >= nextBump)
                {
                    requests.Add(SendBumpAsync(runner));
                    nextBump = runner.Elapsed + NextInterval(commentators);
                }

                float now = runner.Elapsed;
                if (landed.Count > 0)
                    shots.TakeOnce(landed[0].Hit ? "FirstVillain_Hit" : "FirstVillain_Dodged");
                if (landed.Any(l => l.Hit))
                    shots.TakeOnce("VillainHit");
                if (bumpHits.Count > 0)
                    shots.TakeOnce("FirstBump");
                if (boosts.Count > 0 && now >= boosts[0].Time + 0.8f)
                    shots.TakeOnce("HeroBoost");

                await UniTask.Yield();
            }

            Time.timeScale = 1f;
            bot.Release();
            if (runner.Outcome == LevelOutcome.Won)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1.6), ignoreTimeScale: true);
                shots.Take("Win");
            }

            BumpResult[] results = await Task.WhenAll(requests);
            string summary = $"Level {number}: {runner.Outcome?.ToString() ?? "unfinished"} at {runner.Elapsed:0.0}/{runner.Level.TimeLimit:0} s, " +
                             $"height {runner.Climber.Height:0}/{runner.Climber.TopHeight:0} m, villains {landed.Count} (hit {landed.Count(l => l.Hit)}), " +
                             $"bumps {requests.Count} (knockdown {bumpHits.Sum():0} m), boosts {string.Join(",", boosts.Select(b => b.Kind))}.";
            Debug.Log("[Autoplay] " + summary);

            Assert.That(runner.Outcome, Is.EqualTo(LevelOutcome.Won), summary);
            Assert.That(runner.Elapsed, Is.LessThan(runner.Level.TimeLimit), summary);
            Assert.That(requests, Is.Not.Empty, "Commentators sent bumps.");
            Assert.That(results.Where(r => r.SentWhilePlaying).Select(r => r.Status), Is.All.EqualTo(200),
                "Every /bump sent while the level is played is accepted.");
            // Requests still queued when the level ends are dropped (no waves after the win).
            Assert.That(bumpHits.Count, Is.InRange(1, results.Count(r => r.Status == 200)), "Accepted bumps hit the climber.");
            Assert.That(boosts.Select(b => b.Kind), Is.EqualTo(runner.Level.HeroEvents.Select(e => e.Kind)), "Every hero event fired.");
            Assert.That(landed, Is.Not.Empty);
            Assert.That(_errors, Is.Empty, "No errors or exceptions in the log.");

            await SceneManager.LoadSceneAsync(TestScenes.Menu);
        });

        private static float NextInterval(Random random) =>
            MinBumpInterval + (float)random.NextDouble() * (MaxBumpInterval - MinBumpInterval);

        /// <summary>Posts one bump; the level state is sampled when it is sent (main thread) to tell in-play requests apart.</summary>
        private async Task<BumpResult> SendBumpAsync(LevelRunner runner)
        {
            bool playing = runner.IsRunning;
            using HttpResponseMessage response = await _http.PostAsync(BumpUrl, null);
            return new BumpResult((int)response.StatusCode, playing && runner.Outcome == null);
        }

        private void CollectErrors(string message, string stackTrace, LogType type)
        {
            if (type is LogType.Error or LogType.Exception or LogType.Assert)
                _errors.Add($"{type}: {message}");
        }

        private readonly struct BumpResult
        {
            public readonly int Status;

            /// <summary>Sent and answered while the level ran (a request racing the win is not "in play").</summary>
            public readonly bool SentWhilePlaying;

            public BumpResult(int status, bool sentWhilePlaying)
            {
                Status = status;
                SentWhilePlaying = sentWhilePlaying;
            }
        }

        /// <summary>Saves each named screenshot once.</summary>
        private sealed class ScreenshotPlan
        {
            private readonly string _prefix;
            private readonly HashSet<string> _taken = new();

            public ScreenshotPlan(string prefix) => _prefix = prefix;

            public void Take(string name) => ScreenshotCapture.Save($"{_prefix}_{name}");

            public void TakeOnce(string name)
            {
                if (_taken.Add(name))
                    Take(name);
            }
        }
    }
}
