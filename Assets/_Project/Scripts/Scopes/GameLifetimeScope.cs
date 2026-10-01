using GodTower.Audio;
using GodTower.Effects;
using GodTower.Events;
using GodTower.Environment;
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
        [SerializeField] private BumpEffectConfig _bumpEffect;
        [SerializeField] private BumpStage _bumpStage;
        [SerializeField] private ScreenFlash _screenFlash;
        [SerializeField] private HudView _hud;
        [SerializeField] private PausePanelView _pausePanel;
        [SerializeField] private ResultPanelView _resultPanel;
        [SerializeField] private WinStage _winStage;
        [SerializeField] private SkyView _sky;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_levels);
            builder.RegisterInstance(_gameplay);
            builder.Register(resolver => _levels.Get(resolver.Resolve<SelectedLevel>().Index), Lifetime.Scoped);
            builder.Register(_ => new TowerBuilder(_towerSet, _towerRoot), Lifetime.Scoped);

            builder.RegisterComponent(_climber);
            builder.RegisterComponent(_cameraRig);
            builder.Register<UiPointerFilter>(Lifetime.Scoped);
            builder.Register<PointerClimbInput>(Lifetime.Scoped).As<IClimbInput>();

            builder.RegisterEntryPoint<LevelRunner>().AsSelf();

            // Level events: logic ticks after the runner (registration order), presentation listens.
            builder.RegisterInstance(_events);
            builder.RegisterComponent(_eventStage);
            builder.RegisterComponent(_banners);
            builder.RegisterEntryPoint<EventDirector>().AsSelf();
            builder.RegisterEntryPoint<EventPresenter>();

            // Webhook bump: glove waves layered on top of the level.
            builder.RegisterInstance(_bumpEffect);
            builder.RegisterComponent(_bumpStage);
            builder.RegisterComponent(_screenFlash);
            builder.RegisterEntryPoint<BumpDirector>().AsSelf();

            // HUD, pause and result panels, win celebration.
            builder.RegisterComponent(_hud);
            builder.RegisterComponent(_pausePanel);
            builder.RegisterComponent(_resultPanel);
            builder.RegisterComponent(_winStage);
            builder.RegisterEntryPoint<HudPresenter>();
            builder.RegisterEntryPoint<GameFlowPresenter>();
            builder.RegisterEntryPoint<GameAudioPresenter>();

            // Sky, fog and light of the selected level.
            builder.RegisterComponent(_sky);
            builder.Register(resolver => resolver.Resolve<LevelConfig>().Sky, Lifetime.Scoped);
            builder.RegisterEntryPoint<SkyPresenter>();
        }
    }
}
