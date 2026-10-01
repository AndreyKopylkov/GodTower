using GodTower.Levels;
using VContainer.Unity;

namespace GodTower.UI
{
    /// <summary>Builds a short decorated column (with the top deck) behind the main menu, the hero hanging on it.</summary>
    public sealed class MenuBackdrop : IStartable
    {
        public const float TowerHeight = 30f;
        private const int Seed = 7;

        private readonly TowerBuilder _towerBuilder;

        public MenuBackdrop(TowerBuilder towerBuilder) => _towerBuilder = towerBuilder;

        public void Start() => _towerBuilder.Build(TowerHeight, Seed);
    }
}
