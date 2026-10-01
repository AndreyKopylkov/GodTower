using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace GodTower.UI
{
    public enum BannerSide
    {
        /// <summary>Blue hero banners on the left.</summary>
        Hero,

        /// <summary>Red villain banners on the right.</summary>
        Villain
    }

    /// <summary>
    /// Side banner stacks like the reference: banners slide in from their screen edge at the top of the stack,
    /// push older ones down, stay for a moment and slide out again.
    /// </summary>
    public sealed class EventBannerPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform _heroColumn;
        [SerializeField] private RectTransform _villainColumn;
        [SerializeField] private EventBannerView _heroTemplate;
        [SerializeField] private EventBannerView _villainTemplate;
        [SerializeField, Min(0f)] private float _slotSpacing = 150f;
        [SerializeField, Min(0.05f)] private float _slideDuration = 0.3f;
        [SerializeField, Min(0.1f)] private float _holdDuration = 2.6f;
        [SerializeField, Min(1)] private int _maxVisible = 4;

        private readonly List<EventBannerView> _heroBanners = new();
        private readonly List<EventBannerView> _villainBanners = new();

        private void Awake()
        {
            _heroTemplate.gameObject.SetActive(false);
            _villainTemplate.gameObject.SetActive(false);
        }

        public void Show(BannerSide side, Sprite icon, string text) =>
            ShowAsync(side, icon, text, destroyCancellationToken).Forget();

        private async UniTaskVoid ShowAsync(BannerSide side, Sprite icon, string text, CancellationToken token)
        {
            bool hero = side == BannerSide.Hero;
            List<EventBannerView> stack = hero ? _heroBanners : _villainBanners;
            EventBannerView banner = Instantiate(hero ? _heroTemplate : _villainTemplate, hero ? _heroColumn : _villainColumn);
            banner.Bind(icon, text);
            banner.gameObject.SetActive(true);

            // Off-screen x: banners are anchored to their column's outer edge; one width plus the column inset hides them.
            RectTransform column = hero ? _heroColumn : _villainColumn;
            float hiddenX = (hero ? -1f : 1f) * (banner.Rect.rect.width + Mathf.Abs(column.anchoredPosition.x) + 40f);
            banner.Rect.anchoredPosition = new Vector2(hiddenX, 0f);

            stack.Insert(0, banner);
            if (stack.Count > _maxVisible)
                Dismiss(stack[^1], stack, hiddenX);
            Restack(stack);

            await Tween.UIAnchoredPositionX(banner.Rect, 0f, _slideDuration, Ease.OutBack);
            await UniTask.Delay(System.TimeSpan.FromSeconds(_holdDuration), cancellationToken: token);

            if (banner != null && stack.Contains(banner))
            {
                Dismiss(banner, stack, hiddenX);
                Restack(stack);
            }
        }

        private void Restack(List<EventBannerView> stack)
        {
            for (int i = 0; i < stack.Count; i++)
            {
                float y = -i * _slotSpacing;
                if (!Mathf.Approximately(stack[i].Rect.anchoredPosition.y, y))
                    Tween.UIAnchoredPositionY(stack[i].Rect, y, _slideDuration, Ease.OutQuad);
            }
        }

        private void Dismiss(EventBannerView banner, List<EventBannerView> stack, float hiddenX)
        {
            stack.Remove(banner);
            Tween.StopAll(banner.Rect);
            Tween.UIAnchoredPositionX(banner.Rect, hiddenX, _slideDuration, Ease.InBack)
                .OnComplete(banner, view => Destroy(view.gameObject));
        }

        private void OnDestroy()
        {
            foreach (EventBannerView banner in _heroBanners)
                Tween.StopAll(banner.Rect);
            foreach (EventBannerView banner in _villainBanners)
                Tween.StopAll(banner.Rect);
        }
    }
}
