using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GodTower.Effects;
using GodTower.Events;
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

namespace GodTower.Tests.PlayMode
{
    /// <summary>
    /// The webhook glove wave end to end over real HTTP: knockdown and input lock of a single bump, and a request storm
    /// (20 requests in 2 s) that must layer at most 3 waves, never error and leave the level completable.
    /// </summary>
    public sealed class BumpEffectPlayModeTests : InputTestFixture
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
            Time.timeScale = 1f;
            _http.Dispose();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator SingleBump_GlovesHitClimber_Knockdown3Percent_InputLockedAboutHalfASecond() => UniTask.ToCoroutine(async () =>
        {
            Game game = await LoadGameAsync();
            ClimberMotor climber = game.Runner.Climber;

            // Climb a bit first so the knockdown is not floored at the bottom.
            Press(_mouse.leftButton);
            Time.timeScale = 4f;
            await UniTask.WaitUntil(() => climber.Height >= 20f);
            Time.timeScale = 1f;
            Release(_mouse.leftButton);
            await UniTask.WaitUntil(() => climber.State == ClimberState.Idle);

            float? hitMeters = null;
            float hitTime = 0f;
            game.Bumps.Hit += meters =>
            {
                hitMeters = meters;
                hitTime = Time.time;
            };
            float heightBefore = climber.Height;
            int maxGloves = 0;

            Assert.That(await PostBumpAsync(), Is.EqualTo(200));
            while (hitMeters == null)
            {
                maxGloves = Mathf.Max(maxGloves, game.Stage.GlovesInFlight);
                await UniTask.Yield();
            }

            Assert.That(climber.State, Is.EqualTo(ClimberState.Hit), "The first glove contact plays the Hit reaction.");
            Assert.That(climber.AcceptsInput, Is.False, "Input is locked while reacting.");

            await UniTask.WaitUntil(() => climber.AcceptsInput);
            float lockDuration = Time.time - hitTime;
            float expectedMeters = 0.03f * game.Runner.Level.TowerHeight;
            Assert.That(hitMeters, Is.EqualTo(expectedMeters).Within(0.01f));
            Assert.That(climber.Height, Is.EqualTo(heightBefore - expectedMeters).Within(0.05f));
            Assert.That(lockDuration, Is.InRange(0.45f, 0.75f), "Input lock ≈ 0.5 s (hit 0.25 s + short fall).");
            Assert.That(maxGloves, Is.InRange(6, 8), "One wave = 6–8 gloves.");

            await UniTask.WaitUntil(() => game.Bumps.Queue.IsIdle && game.Stage.GlovesInFlight == 0);
            Assert.That(game.Bumps.WavesStarted, Is.EqualTo(1));

            await SceneManager.LoadSceneAsync(TestScenes.Menu);
        });

        [UnityTest]
        public IEnumerator Storm_20RequestsIn2Seconds_LayersMax3Waves_NoErrors_LevelStillCompletable() => UniTask.ToCoroutine(async () =>
        {
            Game game = await LoadGameAsync();
            var bot = new ClimbBot(this, _mouse, game.Runner, game.Director);
            var hits = new List<float>();
            game.Bumps.Hit += hits.Add;
            bot.Hold();

            // 20 requests spread over 2 s (fire and forget), while the bot keeps playing.
            var requests = new List<Task<int>>();
            float burstStart = Time.realtimeSinceStartup;
            int maxActive = 0;
            while (requests.Count < 20 || requests.Any(request => !request.IsCompleted))
            {
                if (requests.Count < 20 && Time.realtimeSinceStartup - burstStart >= requests.Count * 0.1f)
                    requests.Add(PostBumpAsync());

                maxActive = Mathf.Max(maxActive, game.Bumps.Queue.Active);
                await bot.StepAsync();
                await UniTask.Yield();
            }

            Assert.That(requests.Select(request => request.Result), Is.All.EqualTo(200), "Every /bump during play is accepted.");

            // Let the queue drain while playing.
            float drainStart = Time.realtimeSinceStartup;
            while (!game.Bumps.Queue.IsIdle && Time.realtimeSinceStartup - drainStart < 30f)
            {
                maxActive = Mathf.Max(maxActive, game.Bumps.Queue.Active);
                await bot.StepAsync();
                await UniTask.Yield();
            }

            Assert.That(game.Bumps.Queue.IsIdle, Is.True, "The bump queue drains (no soft-lock).");
            Assert.That(maxActive, Is.EqualTo(3), "Waves layer up to the limit of 3.");
            Assert.That(game.Bumps.WavesStarted, Is.EqualTo(20), "Queued requests all play.");
            Assert.That(game.Bumps.Queue.Dropped, Is.Zero);
            Assert.That(hits, Has.Count.EqualTo(20));
            float cap = 0.06f * game.Runner.Level.TowerHeight;
            Assert.That(hits.Sum(), Is.LessThanOrEqualTo(cap * (Mathf.Ceil(game.Runner.Elapsed / 5f) + 1f)), "Bump knockdown stays capped.");

            // Play on to the top.
            Time.timeScale = 4f;
            float realStart = Time.realtimeSinceStartup;
            while (game.Runner.Outcome == null && Time.realtimeSinceStartup - realStart < 60f)
            {
                await bot.StepAsync();
                await UniTask.Yield();
            }

            Time.timeScale = 1f;
            bot.Release();
            Assert.That(game.Runner.Outcome, Is.EqualTo(LevelOutcome.Won),
                $"Level time {game.Runner.Elapsed:0.0}s, height {game.Runner.Climber.Height:0.0}.");

            await SceneManager.LoadSceneAsync(TestScenes.Menu);
        });

        private async Task<int> PostBumpAsync()
        {
            using HttpResponseMessage response = await _http.PostAsync(BumpUrl, null);
            return (int)response.StatusCode;
        }

        private static async UniTask<Game> LoadGameAsync()
        {
            await SceneManager.LoadSceneAsync(TestScenes.Game);
            IObjectResolver container = UnityEngine.Object.FindAnyObjectByType<GameLifetimeScope>().Container;
            var game = new Game(container);
            await UniTask.WaitUntil(() => game.Runner.Climber != null);
            return game;
        }

        private sealed class Game
        {
            public readonly LevelRunner Runner;
            public readonly EventDirector Director;
            public readonly BumpDirector Bumps;
            public readonly BumpStage Stage;

            public Game(IObjectResolver container)
            {
                Runner = container.Resolve<LevelRunner>();
                Director = container.Resolve<EventDirector>();
                Bumps = container.Resolve<BumpDirector>();
                Stage = container.Resolve<BumpStage>();
            }
        }
    }
}
