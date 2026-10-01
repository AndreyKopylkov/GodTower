using PrimeTween;
using UnityEngine;

namespace GodTower.UI
{
    /// <summary>A modal panel (saved inactive in the scene): a dimmed full-screen blocker plus a window that pops in (real time, works while paused).</summary>
    public class PopupView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _window;
        [SerializeField, Min(0.01f)] private float _duration = 0.3f;

        public bool IsVisible => gameObject.activeSelf;

        public void Show()
        {
            gameObject.SetActive(true);
            Tween.StopAll(_group);
            Tween.StopAll(_window);
            _group.interactable = true;
            _group.blocksRaycasts = true;
            _group.alpha = 0f;
            _window.localScale = Vector3.one * 0.7f;
            Tween.Alpha(_group, 1f, _duration * 0.6f, Ease.OutQuad, useUnscaledTime: true);
            Tween.Scale(_window, 1f, _duration, Ease.OutBack, useUnscaledTime: true);
        }

        public void Hide()
        {
            if (!IsVisible)
                return;

            _group.interactable = false;
            Tween.StopAll(_group);
            Tween.StopAll(_window);
            Tween.Scale(_window, 0.8f, _duration * 0.6f, Ease.InBack, useUnscaledTime: true);
            Tween.Alpha(_group, 0f, _duration * 0.6f, Ease.InQuad, useUnscaledTime: true)
                .OnComplete(this, popup => popup.gameObject.SetActive(false));
        }

        protected virtual void OnDestroy()
        {
            Tween.StopAll(_group);
            Tween.StopAll(_window);
        }
    }
}
