using System;
using Cysharp.Threading.Tasks;
using GodTower.Audio;
using GodTower.Core;
using GodTower.Levels;
using VContainer.Unity;

namespace GodTower.UI
{
    /// <summary>
    /// Entry point of the Menu scene: marks the session as "not playing" (webhook bumps are ignored), plays the menu music
    /// and drives <see cref="MenuView"/>: Play continues with the first unfinished level, the level select shows locks.
    /// </summary>
    public sealed class MenuPresenter : IStartable, IDisposable
    {
        private readonly MenuView _view;
        private readonly PlayStateService _playState;
        private readonly ProgressService _progress;
        private readonly ISceneFlow _sceneFlow;
        private readonly LevelCatalog _levels;
        private readonly IAudioService _audio;

        public MenuPresenter(MenuView view, PlayStateService playState, ProgressService progress, ISceneFlow sceneFlow,
            LevelCatalog levels, IAudioService audio)
        {
            _view = view;
            _playState = playState;
            _progress = progress;
            _sceneFlow = sceneFlow;
            _levels = levels;
            _audio = audio;
        }

        public void Start()
        {
            _playState.Set(PlayState.Menu);
            _audio.PlayMusic(MusicId.Menu);
            RefreshLevels();

            _view.PlayClicked += OnPlay;
            _view.LevelsClicked += OnLevels;
            _view.BackClicked += OnBack;
            _view.LevelClicked += OnLevel;
        }

        public void Dispose()
        {
            _view.PlayClicked -= OnPlay;
            _view.LevelsClicked -= OnLevels;
            _view.BackClicked -= OnBack;
            _view.LevelClicked -= OnLevel;
        }

        private void OnPlay() => Load(_progress.NextLevelIndex(_levels.Count));

        private void OnLevels()
        {
            _audio.Play(SoundId.UiOpen);
            RefreshLevels();
            _view.ShowLevelSelect();
        }

        private void OnBack()
        {
            _audio.Play(SoundId.UiClick);
            _view.HideLevelSelect();
        }

        private void OnLevel(int index)
        {
            if (index >= _levels.Count || !_progress.IsUnlocked(index))
            {
                _audio.Play(SoundId.UiLocked);
                _view.LevelButtons[index].Shake();
                return;
            }

            Load(index);
        }

        private void Load(int index)
        {
            if (_sceneFlow.IsLoading)
                return;

            _audio.Play(SoundId.UiClick);
            _sceneFlow.LoadLevelAsync(index).Forget();
        }

        private void RefreshLevels()
        {
            for (int i = 0; i < _view.LevelButtons.Count; i++)
                _view.LevelButtons[i].Bind(i + 1, i < _levels.Count && _progress.IsUnlocked(i), _progress.IsCompleted(i));
        }
    }
}
