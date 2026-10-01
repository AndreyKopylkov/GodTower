using System;

namespace GodTower.Core
{
    /// <summary>Read-only view of the current <see cref="PlayState"/>.</summary>
    public interface IPlayState
    {
        PlayState Current { get; }

        bool IsPlaying { get; }

        /// <summary>Raised on the main thread after the state changes. The argument is the new state.</summary>
        event Action<PlayState> Changed;
    }
}
