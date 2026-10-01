using GodTower.Events;
using GodTower.Gameplay;
using GodTower.Levels;
using GodTower.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GodTower.Scopes
{
    /// <summary>
    /// Container of the Game scene. Scene objects and configs are wired by <c>SceneBuilder</c>;
    /// <see cref="LevelRunner"/> runs the selected level and drives <c>PlayStateService</c> while the scene is loaded.
    /// </summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private LevelCatalog _levels;
        [SerializeField] private GameplayConfig _gameplay;
        [SerializeField] private TowerSet _towerSet;
        [SerializeField] private Transform _towerRoot;
        [SerializeField] private ClimberView _climber;
        [SerializeField] private CameraRig _cameraRig;
        [SerializeField] private EventsConfig _events;
        [SerializeField] private EventStage _eventStage;
        [SerializeField] private EventBannerPanel _banners;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_levels);
            builder.RegisterInstance(_gameplay);
            builder.Register(resolver => _levels.Get(resolver.Resolve<SelectedLevel>().Index), Lifetime.Scoped);
            builder.Register(_ => new TowerBuilder(_towerSet, _towerRoot), Lifetime.Scoped);

            builder.RegisterComponent(_climber);
            builder.RegisterComponent(_cameraRig);
            builder.Register<PointerClimbInput>(Lifetime.Scoped).As<IClimbInput>();

            builder.RegisterEntryPoint<LevelRunner>().AsSelf();

            // Level events: logic ticks after the runner (registration order), presentation listens.
            builder.RegisterInstance(_events);
            builder.RegisterComponent(_eventStage);
            builder.RegisterComponent(_banners);
            builder.RegisterEntryPoint<EventDirector>().AsSelf();
            builder.RegisterEntryPoint<EventPresenter>();
        }
    }
}
