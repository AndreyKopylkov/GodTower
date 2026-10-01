using System;
using GodTower.Audio;
using GodTower.Gameplay;
using GodTower.Levels;
using VContainer.Unity;

namespace GodTower.UI
{
    /// <summary>Feeds the HUD from the running level: height bar, timer (with a tick sound in the last seconds), level title.</summary>
    public sealed class HudPresenter : IStartable, ITickable, IDisposable
    {
        private readonly LevelRunner _runner;
        private readonly HudView _hud;
        private readonly IAudioService _audio;

        public HudPresenter(LevelRunner runner, HudView hud, IAudioService audio)
        {
            _runner = runner;
            _hud = hud;
            _audio = audio;
        }

        public void Start()
        {
            _hud.SetLevel(_runner.Level.Number);
            _hud.HeightBar.SetMax(_runner.Level.TowerHeight);
            _hud.Timer.WarningSecond += OnWarningSecond;
            Tick();
        }

        public void Tick()
        {
            ClimberMotor climber = _runner.Climber;
            if (climber == null)
                return;

            _hud.HeightBar.SetHeight(climber.Height, climber.TopHeight);
            _hud.Timer.SetTimeLeft(_runner.TimeLeft);
        }

        public void Dispose() => _hud.Timer.WarningSecond -= OnWarningSecond;

        private void OnWarningSecond(int seconds)
        {
            if (_runner.IsRunning)
                _audio.Play(SoundId.CountdownTick);
        }
    }
}
