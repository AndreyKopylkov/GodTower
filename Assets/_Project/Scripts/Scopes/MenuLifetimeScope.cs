using GodTower.Environment;
using GodTower.Levels;
using GodTower.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GodTower.Scopes
{
    /// <summary>Container of the Menu scene (main menu and level select). Scene objects are wired by <c>SceneBuilder</c>.</summary>
    public sealed class MenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private LevelCatalog _levels;
        [SerializeField] private TowerSet _towerSet;
        [SerializeField] private Transform _towerRoot;
        [SerializeField] private MenuView _menuView;
        [SerializeField] private SkyView _sky;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_levels);
            builder.Register(_ => new TowerBuilder(_towerSet, _towerRoot), Lifetime.Scoped);
            builder.RegisterComponent(_menuView);

            builder.RegisterEntryPoint<MenuBackdrop>();
            builder.RegisterEntryPoint<MenuPresenter>();

            // The menu uses the first level's sky.
            builder.RegisterComponent(_sky);
            builder.Register(_ => _levels.Get(0).Sky, Lifetime.Scoped);
            builder.RegisterEntryPoint<SkyPresenter>();
        }
    }
}
