using VContainer;
using VContainer.Unity;

namespace GodTower.Scopes
{
    /// <summary>
    /// Container of the Game scene. Gameplay services (level runner, climber, events, bump effect) register here;
    /// the level runner is the component that drives <c>PlayStateService</c> while a level is loaded.
    /// </summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
        }
    }
}
