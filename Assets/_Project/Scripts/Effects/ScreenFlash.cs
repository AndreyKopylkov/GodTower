using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.Effects
{
    /// <summary>Full-screen colour flash (an overlay image that never blocks input).</summary>
    [RequireComponent(typeof(Image))]
    public sealed class ScreenFlash : MonoBehaviour
    {
        [SerializeField] private Image _image;

        private Tween _tween;

        private void Awake()
        {
            _image.raycastTarget = false;
            SetAlpha(0f);
        }

        /// <summary>Jumps to <paramref name="alpha"/> and fades out over <paramref name="duration"/> (real time, so a pause never freezes it on screen).</summary>
        public void Flash(Color color, float alpha, float duration)
        {
            _tween.Stop();
            color.a = alpha;
            _image.color = color;
            _image.enabled = true;
            _tween = Tween.Alpha(_image, 0f, duration, Ease.OutQuad, useUnscaledTime: true)
                .OnComplete(this, flash => flash._image.enabled = false);
        }

        private void SetAlpha(float alpha)
        {
            Color color = _image.color;
            color.a = alpha;
            _image.color = color;
            _image.enabled = alpha > 0f;
        }

        private void OnDestroy() => _tween.Stop();
    }
}
