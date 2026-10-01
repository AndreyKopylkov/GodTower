using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Cysharp.Threading.Tasks;
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
    /// Level 1 autoplay with a simulated mouse: a bot holds to climb and swipes away from telegraphed lanes
    /// (it reads the telegraphs from <see cref="EventDirector"/> — it is a test). Runs at an accelerated time scale.
    /// </summary>
    public sealed class LevelEventsPlayModeTests : InputTestFixture
    {
        private const float TimeScale = 4f;
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
        public IEnumerator Level1_BotDodgesTelegraphedVillains_GetsJetpack_AndWins() => UniTask.ToCoroutine(async () =>
        {
            (LevelRunner runner, EventDirector director) = await LoadGameAsync();
            var bot = new ClimbBot(this, _mouse, runner, director);
            var landed = new List<(VillainStrike Strike, bool Hit)>();
            var boosts = new List<HeroBoost>();
            director.Landed += (strike, hit) => landed.Add((strike, hit));
            director.BoostStarted += boosts.Add;
            var bumpStatuses = new List<int>();
            float nextBump = 3f;

            bot.Hold();
            Time.timeScale = TimeScale;
            float realStart = Time.realtimeSinceStartup;
            while (runner.Outcome == null && Time.realtimeSinceStartup - realStart < 60f)
            {
                await bot.StepAsync();

                if (runner.Elapsed >= nextBump)
                {
                    nextBump += 7f;
                    bumpStatuses.Add(await PostBumpAsync());
                }

                await UniTask.Yield();
            }

            Time.timeScale = 1f;
            bot.Release();

            Assert.That(runner.Outcome, Is.EqualTo(LevelOutcome.Won), $"Level time {runner.Elapsed:0.0}s, height {runner.Climber.Height:0.0}.");
            Assert.That(boosts.Select(b => b.Kind), Is.EqualTo(new[] { HeroKind.Jetpack }));
            Assert.That(landed, Is.Not.Empty);
            Assert.That(landed.Select(l => l.Strike.Kind), Is.All.EqualTo(VillainKind.Missile));
            Assert.That(landed.Count(l => l.Hit), Is.Zero, "The bot dodges every telegraphed villain.");
            Assert.That(landed.Select(l => l.Strike.Id), Is.EqualTo(director.Timeline.Strikes.Take(landed.Count).Select(s => s.Id)),
                "Villains land in schedule order.");
            Assert.That(bumpStatuses, Is.Not.Empty.And.All.EqualTo(200), "Every /bump during play is accepted.");

            await SceneManager.LoadSceneAsync(TestScenes.Menu);
        });

        [UnityTest]
        public IEnumerator Level1_StayingInTargetedLane_IsKnockedDown8Percent() => UniTask.ToCoroutine(async () =>
        {
            (LevelRunner runner, EventDirector director) = await LoadGameAsync();
            VillainStrike first = director.Timeline.Strikes[0];
            int lane = Enumerable.Range(0, runner.Climber.Lanes.Count).First(first.Hits);
            bool? firstHit = null;
            director.Landed += (strike, hit) => firstHit ??= hit;
            var bot = new ClimbBot(this, _mouse, runner, director);

            // Move into the lane of the first missile, climb, then wait for it.
            bot.Hold();
            await UniTask.DelayFrame(2);
            int direction = Math.Sign(lane - runner.Climber.Lanes.Index);
            if (direction != 0)
                await bot.SwipeAsync(direction);
            await UniTask.Delay(TimeSpan.FromSeconds(0.3));
            Assert.That(runner.Climber.Lanes.Index, Is.EqualTo(lane));

            Time.timeScale = TimeScale;
            await UniTask.WaitUntil(() => runner.Elapsed >= first.ImpactTime - 0.3f);
            bot.Release();
            await UniTask.WaitUntil(() => firstHit != null);
            float heightAtImpact = runner.Climber.Height;
            Assert.That(firstHit, Is.True);
            Assert.That(runner.Climber.State, Is.EqualTo(ClimberState.Hit));

            await UniTask.WaitUntil(() => runner.Climber.State == ClimberState.Idle);
            Time.timeScale = 1f;
            float expected = Mathf.Max(0f, heightAtImpact - 0.08f * runner.Level.TowerHeight);
            Assert.That(runner.Climber.Height, Is.EqualTo(expected).Within(0.05f));

            await SceneManager.LoadSceneAsync(TestScenes.Menu);
        });

        private async UniTask<(LevelRunner, EventDirector)> LoadGameAsync()
        {
            await SceneManager.LoadSceneAsync(TestScenes.Game);
            var scope = UnityEngine.Object.FindAnyObjectByType<GameLifetimeScope>();
            var runner = scope.Container.Resolve<LevelRunner>();
            await UniTask.WaitUntil(() => runner.Climber != null);
            return (runner, scope.Container.Resolve<EventDirector>());
        }

        private async UniTask<int> PostBumpAsync()
        {
            using HttpResponseMessage response = await _http.PostAsync(BumpUrl, null);
            return (int)response.StatusCode;
        }
    }
}
