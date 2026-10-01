using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using GodTower.Core;
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

namespace GodTower.Tests.PlayMode
{
    /// <summary>
    /// Menu → Level 1 → Win → Next → Level 2 → Pause → Menu, clicking the real UI (EventSystem raycasts), with fade
    /// transitions, progress unlock, UI-blocks-gameplay input and the webhook gate checked on the way.
    /// </summary>
    public sealed class GameFlowPlayModeTests : InputTestFixture
    {
        private static readonly string BumpUrl = $"http://localhost:{BumpServerOptions.DefaultPort}/bump";

        private Mouse _mouse;
        private HttpClient _http;
        private int _savedProgress;
        private int _gameLoads;

        public override void Setup()
        {
            base.Setup();
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            _savedProgress = PlayerPrefs.GetInt(PlayerPrefsProgressStore.Key, 0);
            SceneManager.sceneLoaded += CountGameLoads;
        }

        public override void TearDown()
        {
            SceneManager.sceneLoaded -= CountGameLoads;
            PlayerPrefs.SetInt(PlayerPrefsProgressStore.Key, _savedProgress);
            // Other tests load the Game scene directly and expect Level 1.
            VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance().Container.Resolve<SelectedLevel>().Select(0);
            Time.timeScale = 1f;
            _http.Dispose();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Menu_Level1_Win_Next_Level2_Pause_Menu() => UniTask.ToCoroutine(async () =>
        {
            await SceneManager.LoadSceneAsync(TestScenes.Menu);
            // Added once the menu is up: a UI module left over from a previous test re-resolves its actions on unload,
            // which invalidates devices added before (seen as "control has no associated state").
            _mouse = InputSystem.AddDevice<Mouse>();
            IObjectResolver root = VContainerSettings.Instance.GetOrCreateRootLifetimeScopeInstance().Container;
            var playState = root.Resolve<PlayStateService>();
            var flow = root.Resolve<ISceneFlow>();
            var progress = root.Resolve<ProgressService>();
            MenuView menu = UnityEngine.Object.FindAnyObjectByType<MenuView>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(playState.Current, Is.EqualTo(PlayState.Menu));
            await UniTask.Delay(TimeSpan.FromSeconds(0.3), ignoreTimeScale: true);
            ScreenshotCapture.Save("Menu_Main");

            // Menu → level select → Level 1.
            await ClickAsync(menu.LevelsButton);
            await UniTask.Delay(TimeSpan.FromSeconds(0.5), ignoreTimeScale: true);
            Assert.That(menu.IsLevelSelectVisible, Is.True);
            Assert.That(menu.LevelButtons[0].IsUnlocked, Is.True);
            ScreenshotCapture.Save("Menu_LevelSelect");
            await ClickAsync(menu.LevelButtons[0].Button);
            LevelRunner runner = await WaitForLevelAsync(flow, expectedNumber: 1);
            Assert.That(_gameLoads, Is.EqualTo(1));
            Assert.That(playState.Current, Is.EqualTo(PlayState.Playing));

            // Win (the flow is under test, not the climbing): lift the climber to the top.
            runner.Climber.Carry(runner.Climber.TopHeight, 0.3f);
            ResultPanelView result = Scope().Container.Resolve<ResultPanelView>();
            await WaitAsync(() => result.IsVisible, 10f, "result panel");
            await UniTask.Delay(TimeSpan.FromSeconds(0.6), ignoreTimeScale: true);
            Assert.That(runner.Outcome, Is.EqualTo(LevelOutcome.Won));
            Assert.That(result.NextButton.gameObject.activeSelf, Is.True);
            Assert.That(progress.IsUnlocked(1), Is.True, "Winning Level 1 unlocks Level 2.");
            Assert.That(PlayerPrefs.GetInt(PlayerPrefsProgressStore.Key), Is.GreaterThanOrEqualTo(1));
            ScreenshotCapture.Save("Result_Win");

            // Next → Level 2. A second click right away (before the fader blocks the UI) must not load twice.
            await ClickAsync(result.NextButton);
            result.NextButton.onClick.Invoke();
            runner = await WaitForLevelAsync(flow, expectedNumber: 2);
            Assert.That(_gameLoads, Is.EqualTo(2), "One load per transition.");

            // Pause through the HUD button: the press must not make the climber climb.
            HudView hud = Scope().Container.Resolve<HudView>();
            PausePanelView pause = Scope().Container.Resolve<PausePanelView>();
            await UniTask.Delay(TimeSpan.FromSeconds(0.3), ignoreTimeScale: true);
            await ClickAsync(hud.PauseButton, holdMouse: true);
            await UniTask.Delay(TimeSpan.FromSeconds(0.4), ignoreTimeScale: true);
            Assert.That(runner.IsPaused, Is.True);
            Assert.That(pause.IsVisible, Is.True);
            Assert.That(runner.Climber.Height, Is.Zero, "A press on the pause button is not a climb input.");
            Assert.That(playState.Current, Is.EqualTo(PlayState.Paused));
            Assert.That(await PostBumpAsync(), Is.EqualTo(409), "Paused: /bump is ignored.");
            ScreenshotCapture.Save("Pause");

            // Pause → Menu.
            await ClickAsync(pause.MenuButton);
            await WaitAsync(() => !flow.IsLoading && SceneManager.GetActiveScene().name == TestScenes.Menu, 10f, "menu scene");
            Assert.That(playState.Current, Is.EqualTo(PlayState.Menu));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(await PostBumpAsync(), Is.EqualTo(409));
            Assert.That(UnityEngine.Object.FindAnyObjectByType<MenuView>().LevelButtons[1].IsUnlocked, Is.True);
        });

        private static GameLifetimeScope Scope() => UnityEngine.Object.FindAnyObjectByType<GameLifetimeScope>();

        private static async UniTask<LevelRunner> WaitForLevelAsync(ISceneFlow flow, int expectedNumber)
        {
            await UniTask.Yield();
            await WaitAsync(() => !flow.IsLoading && SceneManager.GetActiveScene().name == TestScenes.Game, 10f, "game scene");
            var runner = Scope().Container.Resolve<LevelRunner>();
            await WaitAsync(() => runner.Climber != null, 5f, "level start");
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

        /// <summary>
        /// Clicks the centre of a button: the EventSystem raycast there must hit the button first (visible, not blocked),
        /// then the click is dispatched to it. With <paramref name="holdMouse"/> the simulated mouse is also held down over
        /// the button meanwhile, so gameplay input sees the press (and must ignore it). Clicks are dispatched directly
        /// rather than through the Input System UI module, whose shared default actions do not survive the input
        /// fixture's resets reliably across tests.
        /// </summary>
        private async UniTask ClickAsync(Button button, bool holdMouse = false)
        {
            await UniTask.Yield(); // let canvases settle (e.g. after a screenshot switched their render mode)
            var rect = (RectTransform)button.transform;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = screen, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, $"Nothing under {button.name}.");
            Assert.That(hits[0].gameObject.transform.IsChildOf(button.transform), Is.True,
                $"{hits[0].gameObject.name} blocks {button.name}.");

            if (holdMouse)
            {
                Set(_mouse.position, screen);
                Press(_mouse.leftButton);
                await UniTask.Yield();
                await UniTask.Yield();
            }

            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            await UniTask.Yield();
            if (holdMouse)
                Release(_mouse.leftButton);
            await UniTask.Yield();
        }

        private async UniTask<int> PostBumpAsync()
        {
            using HttpResponseMessage response = await _http.PostAsync(BumpUrl, null);
            return (int)response.StatusCode;
        }

        private void CountGameLoads(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == TestScenes.Game)
                _gameLoads++;
        }
    }
}
