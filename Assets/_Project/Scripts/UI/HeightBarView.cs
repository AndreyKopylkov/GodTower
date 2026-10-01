using TMPro;
using UnityEngine;

namespace GodTower.UI
{
    /// <summary>
    /// Left vertical height bar like the reference: tower height on top, a fill up to the climber's height and
    /// a marker (hero badge + current height) riding the fill's top edge.
    /// </summary>
    public sealed class HeightBarView : MonoBehaviour
    {
        [Tooltip("Inner area of the bar; the fill and the marker move inside it.")]
        [SerializeField] private RectTransform _track;
        [SerializeField] private RectTransform _fill;
        [SerializeField] private RectTransform _marker;
        [SerializeField] private TMP_Text _maxLabel;
        [SerializeField] private TMP_Text _heightLabel;

        private int _shownHeight = -1;

        public void SetMax(float meters) => _maxLabel.text = Format(meters);

        public void SetHeight(float meters, float maxMeters)
        {
            float fraction = maxMeters > 0f ? Mathf.Clamp01(meters / maxMeters) : 0f;

            // Anchor-relative, so the bar stays right on any resolution; the fill never gets shorter than it is wide
            // (keeps the rounded caps of the sliced sprite intact).
            float trackHeight = _track.rect.height;
            float minFraction = trackHeight > 0f ? Mathf.Clamp01(_fill.rect.width / trackHeight) : 0f;
            _fill.anchorMax = new Vector2(_fill.anchorMax.x, Mathf.Max(fraction, minFraction));
            _marker.anchorMin = _marker.anchorMax = new Vector2(_marker.anchorMin.x, fraction);

            int rounded = Mathf.FloorToInt(meters);
            if (rounded == _shownHeight)
                return;

            _shownHeight = rounded;
            _heightLabel.text = Format(rounded);
        }

        private static string Format(float meters) => Mathf.FloorToInt(meters).ToString();
    }
}
