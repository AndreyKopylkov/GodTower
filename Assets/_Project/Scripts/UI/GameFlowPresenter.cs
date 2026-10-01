using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GodTower.Audio;
using GodTower.Core;
using GodTower.Effects;
using GodTower.Gameplay;
using GodTower.Levels;
using VContainer.Unity;

namespace GodTower.UI
{
    /// <summary>
    /// Level flow around the <see cref="LevelRunner"/>: pause panel (Resume / Restart / Menu), the win celebration and
    /// result panels (Next / Retry / Menu), progress unlock on a win, level music and gameplay-sound pause.
    /// </summary>
    public sealed class GameFlowPresenter : IStartable, IDisposable
    {
        private const float ResultPanelDelay = 1.4f;

        private readonly LevelRunner _runner;
        private readonly HudView _hud;
        private readonly PausePanelView _pausePanel;
        private readonly ResultPanelView _resultPanel;
        private readonly WinStage _winStage;
        private readonly ISceneFlow _sceneFlow;
        private readonly ProgressService _progress;
        private readonly LevelCatalog _levels;
        private readonly SelectedLevel _selectedLevel;
        private readonly GameplayConfig _gameplay;
        private readonly IAudioService _audio;
        private readonly CancellationTokenSource _lifetime = new();

        public GameFlowPresenter(LevelRunner runner, HudView hud, PausePanelView pausePanel, ResultPanelView resultPanel,
            WinStage winStage, ISceneFlow sceneFlow, ProgressService progress, LevelCatalog levels, SelectedLevel selectedLevel,
            GameplayConfig gameplay, IAudioService audio)
        {
            _runner = runner;
            _hud = hud;
            _pausePanel = pausePanel;
            _resultPanel = resultPanel;
            _winStage = winStage;
            _sceneFlow = sceneFlow;
            _progress = progress;
            _levels = levels;
            _selectedLevel = selectedLevel;
            _gameplay = gameplay;
            _audio = audio;
        }

        private int LevelIndex => _selectedLevel.Index;

        private bool HasNextLevel => LevelIndex + 1 < _levels.Count;

        public void Start()
        {
            _audio.PlayMusic(MusicId.Level);
            _hud.PauseClicked += Pause;
            _pausePanel.ResumeClicked += Resume;
            _pausePanel.RestartClicked += Restart;
            _pausePanel.MenuClicked += GoToMenu;
            _resultPanel.NextClicked += NextLevel;
            _resultPanel.RetryClicked += Restart;
            _resultPanel.MenuClicked += GoToMenu;
            _runner.Ended += OnEnded;
        }

        public void Dispose()
        {
            _hud.PauseClicked -= Pause;
            _pausePanel.ResumeClicked -= Resume;
            _pausePanel.RestartClicked -= Restart;
            _pausePanel.MenuClicked -= GoToMenu;
            _resultPanel.NextClicked -= NextLevel;
            _resultPanel.RetryClicked -= Restart;
            _resultPanel.MenuClicked -= GoToMenu;
            _runner.Ended -= OnEnded;
            _lifetime.Cancel();
            _lifetime.Dispose();
            _audio.SetGameplayPaused(false);
        }

        private void Pause()
        {
            if (!_runner.IsRunning || _sceneFlow.IsLoading)
                return;

            _audio.Play(SoundId.UiOpen);
            _runner.Pause();
            _audio.SetGameplayPaused(true);
            _pausePanel.Show();
        }

        private void Resume()
        {
            if (!_runner.IsPaused || _sceneFlow.IsLoading)
                return;

            _audio.Play(SoundId.UiClick);
            _pausePanel.Hide();
            _audio.SetGameplayPaused(false);
            _runner.Resume();
        }

        private void Restart() => Load(LevelIndex);

        private void NextLevel() => Load(HasNextLevel ? LevelIndex + 1 : LevelIndex);

        private void Load(int index)
        {
            if (_sceneFlow.IsLoading)
                return;

            _audio.Play(SoundId.UiClick);
            _sceneFlow.LoadLevelAsync(index).Forget();
        }

        private void GoToMenu()
        {
            if (_sceneFlow.IsLoading)
                return;

            _audio.Play(SoundId.UiClick);
            _sceneFlow.LoadMenuAsync().Forget();
        }

        private void OnEnded(LevelOutcome outcome)
        {
            _hud.SetPauseInteractable(false);
            if (outcome == LevelOutcome.Won)
                _progress.MarkCompleted(LevelIndex);

            ShowResultAsync(outcome, _lifetime.Token).Forget();
        }

        private async UniTaskVoid ShowResultAsync(LevelOutcome outcome, CancellationToken token)
        {
            int number = _runner.Level.Number;
            if (outcome == LevelOutcome.Won)
            {
                // The trophy pops up next to the climber once it has hopped onto the deck.
                await UniTask.Delay(TimeSpan.FromSeconds(_gameplay.WinHopDuration), cancellationToken: token);
                _winStage.Celebrate();
                _audio.Play(SoundId.Win);
                await UniTask.Delay(TimeSpan.FromSeconds(ResultPanelDelay), cancellationToken: token);
                _resultPanel.ShowWin(number, HasNextLevel);
            }
            else
            {
                _audio.Play(SoundId.Lose);
                await UniTask.Delay(TimeSpan.FromSeconds(ResultPanelDelay), cancellationToken: token);
                _resultPanel.ShowLose(number);
            }
        }
    }
}
