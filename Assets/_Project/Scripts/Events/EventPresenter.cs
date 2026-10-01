using System;
using GodTower.UI;
using VContainer.Unity;

namespace GodTower.Events
{
    /// <summary>Routes <see cref="EventDirector"/> events to the side banners and the world-space <see cref="EventStage"/>.</summary>
    public sealed class EventPresenter : IStartable, IDisposable
    {
        private readonly EventDirector _director;
        private readonly EventsConfig _config;
        private readonly EventStage _stage;
        private readonly EventBannerPanel _banners;

        public EventPresenter(EventDirector director, EventsConfig config, EventStage stage, EventBannerPanel banners)
        {
            _director = director;
            _config = config;
            _stage = stage;
            _banners = banners;
        }

        public void Start()
        {
            _director.Telegraphed += OnTelegraphed;
            _director.Landed += OnLanded;
            _director.BoostStarted += OnBoostStarted;
        }

        public void Dispose()
        {
            _director.Telegraphed -= OnTelegraphed;
            _director.Landed -= OnLanded;
            _director.BoostStarted -= OnBoostStarted;
        }

        public static string BannerText(string displayName) => $"{displayName} ×1";

        private void OnTelegraphed(VillainStrike strike)
        {
            VillainEventConfig config = _config.Get(strike.Kind);
            _banners.Show(BannerSide.Villain, config.Icon, BannerText(config.DisplayName));
            _stage.BeginStrike(strike, config);
        }

        private void OnLanded(VillainStrike strike, bool hit) => _stage.LandStrike(strike, hit);

        private void OnBoostStarted(HeroBoost boost)
        {
            HeroEventConfig config = _config.Get(boost.Kind);
            _banners.Show(BannerSide.Hero, config.Icon, BannerText(config.DisplayName));
            _stage.PlayBoost(boost, config);
        }
    }
}
