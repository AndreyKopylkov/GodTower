using System;
using GodTower.Core;

namespace GodTower.Webhook
{
    /// <summary>
    /// Main-thread decision point for webhook bumps: accepts a bump only while a level is being played
    /// and forwards accepted bumps to gameplay through <see cref="IBumpSignal"/>.
    /// </summary>
    public sealed class BumpGate : IBumpSignal
    {
        private readonly IPlayState _playState;

        public event Action BumpRequested;

        public BumpGate(IPlayState playState) => _playState = playState;

        /// <summary>Main thread only.</summary>
        /// <returns><c>true</c> if the bump was accepted (HTTP 200), <c>false</c> if ignored (HTTP 409).</returns>
        public bool TryTrigger()
        {
            if (!_playState.IsPlaying)
                return false;

            BumpRequested?.Invoke();
            return true;
        }
    }
}
