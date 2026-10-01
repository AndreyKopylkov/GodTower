using System;
using Cysharp.Threading.Tasks;
using GodTower.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GodTower.Core
{
    /// <summary>Scene transitions between the menu and levels.</summary>
    public interface ISceneFlow
    {
        /// <summary>A transition is running; further requests are ignored until it ends (no double loads).</summary>
        bool IsLoading { get; }

        UniTask LoadMenuAsync();

        /// <summary>Loads the Game scene with the level at <paramref name="index"/> (0-based catalog index).</summary>
        UniTask LoadLevelAsync(int index);
    }

    /// <summary>
    /// Fade out → load → fade in. The session leaves <see cref="PlayState.Playing"/> before the fade so <c>/bump</c>
    /// answers 409 during transitions, and the time scale is reset (the player may leave from the pause panel).
    /// </summary>
    public sealed class SceneFlow : ISceneFlow
    {
        public const string MenuScene = "Menu";
        public const string GameScene = "Game";

        private readonly SelectedLevel _selectedLevel;
        private readonly PlayStateService _playState;
        private readonly ScreenFader _fader;

        public SceneFlow(SelectedLevel selectedLevel, PlayStateService playState, ScreenFader fader)
        {
            _selectedLevel = selectedLevel;
            _playState = playState;
            _fader = fader;
        }

        public bool IsLoading { get; private set; }

        public UniTask LoadMenuAsync() => LoadAsync(MenuScene, null);

        public UniTask LoadLevelAsync(int index) => LoadAsync(GameScene, index);

        private async UniTask LoadAsync(string scene, int? level)
        {
            if (IsLoading)
                return;

            IsLoading = true;
            try
            {
                if (level.HasValue)
                    _selectedLevel.Select(level.Value);
                _playState.Set(PlayState.Menu);
                await _fader.FadeOutAsync();

                Time.timeScale = 1f;
                await SceneManager.LoadSceneAsync(scene);
                await _fader.FadeInAsync();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Debug.LogException(exception);
                await _fader.FadeInAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
