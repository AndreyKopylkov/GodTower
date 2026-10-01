using UnityEngine;
using VContainer.Unity;

namespace GodTower.Core
{
    /// <summary>Applies session-wide runtime settings once at app launch.</summary>
    public sealed class AppBootstrap : IStartable
    {
        public const int TargetFrameRate = 60;

        public void Start()
        {
            Application.targetFrameRate = TargetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
