using GodTower.Audio;
using GodTower.Core;
using GodTower.Levels;
using GodTower.Webhook;
using VContainer;
using UnityEngine;
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
        [SerializeField] private AudioLibrary _audio;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<AppBootstrap>();

            builder.Register<PlayStateService>(Lifetime.Singleton).AsSelf().As<IPlayState>();
            builder.Register<SelectedLevel>(Lifetime.Singleton);
            builder.Register<PlayerPrefsProgressStore>(Lifetime.Singleton).As<IProgressStore>();
            builder.Register<ProgressService>(Lifetime.Singleton);
            builder.Register(_ => new ScreenFader(), Lifetime.Singleton);
            builder.Register<SceneFlow>(Lifetime.Singleton).As<ISceneFlow>();
            builder.RegisterEntryPoint(_ => new AudioService(_audio), Lifetime.Singleton).AsSelf();

            // Webhook: the dispatcher ticks every frame, the server lives for the whole session.
            builder.RegisterEntryPoint<MainThreadDispatcher>().AsSelf();
            builder.Register<BumpGate>(Lifetime.Singleton).AsSelf().As<IBumpSignal>();
            builder.RegisterInstance(new BumpServerOptions());
            builder.RegisterEntryPoint<BumpHttpServer>().AsSelf();
        }
    }
}
