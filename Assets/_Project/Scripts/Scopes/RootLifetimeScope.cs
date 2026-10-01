using GodTower.Core;
using VContainer;
using VContainer.Unity;

namespace GodTower.Scopes
{
    /// <summary>
    /// Session-wide container, instantiated by VContainer from <c>VContainerSettings</c> before the first scene
    /// and kept alive across scene loads. Scene scopes (<see cref="MenuLifetimeScope"/>, <see cref="GameLifetimeScope"/>)
    /// are its children and can resolve everything registered here.
    /// </summary>
    public sealed class RootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<AppBootstrap>();
        }
    }
}
