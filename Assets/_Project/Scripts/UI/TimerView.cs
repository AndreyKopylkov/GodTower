using PrimeTween;
using TMPro;
using UnityEngine;

namespace GodTower.UI
{
    /// <summary>Level countdown "m:ss"; turns red and pulses once per second in the last seconds.</summary>
    public sealed class TimerView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Color _normalColor = new(1f, 0.86f, 0.1f);
        [SerializeField] private Color _warningColor = new(1f, 0.25f, 0.2f);
        [SerializeField, Min(0f)] private float _warningSeconds = 10f;

        private int _shownSeconds = -1;

        /// <summary>Raised when the shown whole second changes inside the warning window (for the tick sound).</summary>
        public event System.Action<int> WarningSecond;

        public void SetTimeLeft(float seconds)
        {
            int whole = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            if (whole == _shownSeconds)
                return;

            _shownSeconds = whole;
            _label.text = $"{whole / 60}:{whole % 60:00}";
            bool warning = whole <= _warningSeconds;
            _label.color = warning ? _warningColor : _normalColor;
            if (!warning || whole == 0)
                return;

            Tween.StopAll(_label.transform);
            _label.transform.localScale = Vector3.one;
            Tween.PunchScale(_label.transform, Vector3.one * 0.25f, 0.35f);
            WarningSecond?.Invoke(whole);
        }

        private void OnDestroy() => Tween.StopAll(_label.transform);
    }
}
