using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>One side banner ("Missile ×1") with the event icon in its badge.</summary>
    public sealed class EventBannerView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _label;

        public RectTransform Rect => (RectTransform)transform;

        public void Bind(Sprite icon, string text)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _label.text = text;
        }
    }
}
