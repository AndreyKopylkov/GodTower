using VContainer.Unity;

namespace GodTower.Environment
{
    /// <summary>Applies the scene's sky preset (the selected level's, or the first level's in the menu) when the scope starts.</summary>
    public sealed class SkyPresenter : IStartable
    {
        private readonly SkyView _view;
        private readonly SkyPreset _preset;

        public SkyPresenter(SkyView view, SkyPreset preset)
        {
            _view = view;
            _preset = preset;
        }

        public void Start() => _view.Apply(_preset);
    }
}
