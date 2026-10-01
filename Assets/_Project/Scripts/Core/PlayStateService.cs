using System;

namespace GodTower.Core
{
    /// <summary>
    /// Owns the session <see cref="PlayState"/>. Lives in the root scope so that session-wide services
    /// (the webhook gate) can read it; scene flow code (menu, level runner) writes it.
    /// Main thread only.
    /// </summary>
    public sealed class PlayStateService : IPlayState
    {
        public PlayState Current { get; private set; } = PlayState.Menu;

        public bool IsPlaying => Current == PlayState.Playing;

        public event Action<PlayState> Changed;

        public void Set(PlayState state)
        {
            if (state == Current)
                return;

            Current = state;
            Changed?.Invoke(state);
        }
    }
}
