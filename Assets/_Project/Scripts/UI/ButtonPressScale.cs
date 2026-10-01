using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GodTower.UI
{
    /// <summary>Squashes a button while pressed and springs it back on release (real time, works in the pause menu).</summary>
    public sealed class ButtonPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField, Range(0.5f, 1f)] private float _pressedScale = 0.92f;

        public void OnPointerDown(PointerEventData eventData) => ScaleTo(_pressedScale, 0.08f, Ease.OutQuad);

        public void OnPointerUp(PointerEventData eventData) => ScaleTo(1f, 0.25f, Ease.OutBack);

        private void ScaleTo(float scale, float duration, Ease ease)
        {
            Tween.StopAll(transform);
            Tween.Scale(transform, scale, duration, ease, useUnscaledTime: true);
        }

        private void OnDisable()
        {
            Tween.StopAll(transform);
            transform.localScale = Vector3.one;
        }
    }
}
