using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>One level-select tile: number, lock when locked, star when completed.</summary>
    public sealed class LevelButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _number;
        [SerializeField] private Image _lock;
        [SerializeField] private Image _star;
        [SerializeField] private Sprite _unlockedSprite;
        [SerializeField] private Sprite _lockedSprite;

        public Button Button => _button;
        public bool IsUnlocked { get; private set; }

        public event Action Clicked;

        private void Awake() => _button.onClick.AddListener(() => Clicked?.Invoke());

        public void Bind(int number, bool unlocked, bool completed)
        {
            IsUnlocked = unlocked;
            _number.text = number.ToString();
            _number.gameObject.SetActive(unlocked);
            _lock.gameObject.SetActive(!unlocked);
            _star.gameObject.SetActive(completed);
            _background.sprite = unlocked ? _unlockedSprite : _lockedSprite;
        }

        /// <summary>"Nope" wiggle for a locked tile.</summary>
        public void Shake()
        {
            Tween.StopAll(transform);
            transform.localRotation = Quaternion.identity;
            Tween.PunchLocalRotation(transform, new Vector3(0f, 0f, 12f), 0.4f, useUnscaledTime: true);
        }

        private void OnDestroy() => Tween.StopAll(transform);
    }
}
