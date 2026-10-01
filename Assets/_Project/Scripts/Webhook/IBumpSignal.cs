using System;

namespace GodTower.Webhook
{
    /// <summary>Notifies gameplay that an accepted <c>/bump</c> request should play the boxing-glove event.</summary>
    public interface IBumpSignal
    {
        /// <summary>Raised on the main thread, only while a level is being played.</summary>
        event Action BumpRequested;
    }
}
