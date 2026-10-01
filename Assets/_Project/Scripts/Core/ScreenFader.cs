using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GodTower.Core
{
    /// <summary>
    /// Session-wide full-screen fade used by scene transitions. Built from code on first use (one overlay canvas above
    /// everything, kept across scene loads); it blocks input while visible. Real-time tweens, so it works while paused.
    /// </summary>
    public sealed class ScreenFader : IDisposable
    {
        private const int SortingOrder = 1000;
        private static readonly Color FadeColor = new(0.05f, 0.1f, 0.25f, 1f);

        private readonly float _duration;
        private CanvasGroup _group;

        public ScreenFader(float duration = 0.25f) => _duration = duration;

        public bool IsVisible => _group != null && _group.alpha > 0f;

        public async UniTask FadeOutAsync()
        {
            EnsureCreated();
            _group.blocksRaycasts = true;
            Tween.StopAll(_group);
            await Tween.Alpha(_group, 1f, _duration * (1f - _group.alpha), Ease.InQuad, useUnscaledTime: true);
        }

        public async UniTask FadeInAsync()
        {
            EnsureCreated();
            Tween.StopAll(_group);
            await Tween.Alpha(_group, 0f, _duration * _group.alpha, Ease.OutQuad, useUnscaledTime: true);
            _group.blocksRaycasts = false;
        }

        public void Dispose()
        {
            if (_group == null)
                return;

            Tween.StopAll(_group);
            Object.Destroy(_group.gameObject);
            _group = null;
        }

        private void EnsureCreated()
        {
            if (_group != null)
                return;

            var root = new GameObject("ScreenFader", typeof(RectTransform));
            Object.DontDestroyOnLoad(root);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            root.AddComponent<GraphicRaycaster>();

            var image = root.AddComponent<Image>();
            image.color = FadeColor;

            _group = root.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }
    }
}
