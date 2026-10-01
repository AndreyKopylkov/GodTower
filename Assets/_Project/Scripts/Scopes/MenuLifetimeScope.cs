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

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_levels);
            builder.Register(_ => new TowerBuilder(_towerSet, _towerRoot), Lifetime.Scoped);
            builder.RegisterComponent(_menuView);

            builder.RegisterEntryPoint<MenuBackdrop>();
            builder.RegisterEntryPoint<MenuPresenter>();
        }
    }
}
