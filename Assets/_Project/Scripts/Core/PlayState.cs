namespace GodTower.Core
{
    /// <summary>High-level state of the session, used to gate gameplay-only features such as the /bump webhook.</summary>
    public enum PlayState
    {
        /// <summary>Main menu, level select or loading — no level is running.</summary>
        Menu,

        /// <summary>A level is running and accepts gameplay input.</summary>
        Playing,

        /// <summary>A level is loaded but paused by the player.</summary>
        Paused,

        /// <summary>A level has ended and the win or lose screen is shown.</summary>
        Result
    }
}
