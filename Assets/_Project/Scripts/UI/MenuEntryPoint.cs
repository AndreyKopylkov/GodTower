using GodTower.Core;
using VContainer.Unity;

namespace GodTower.UI
{
    /// <summary>Entry point of the Menu scene: marks the session as "not playing" so webhook bumps are ignored.</summary>
    public sealed class MenuEntryPoint : IStartable
    {
        private readonly PlayStateService _playState;

        public MenuEntryPoint(PlayStateService playState) => _playState = playState;

        public void Start() => _playState.Set(PlayState.Menu);
    }
}
