using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>End-of-level panel: win (trophy, Next / Menu) or lose (Retry / Menu).</summary>
    public sealed class ResultPanelView : PopupView
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private Image _icon;
        [SerializeField] private Sprite _winIcon;
        [SerializeField] private Sprite _loseIcon;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _menuButton;

        public Button NextButton => _nextButton;
        public Button RetryButton => _retryButton;
        public Button MenuButton => _menuButton;

        public event Action NextClicked;
        public event Action RetryClicked;
        public event Action MenuClicked;

        private void Awake()
        {
            _nextButton.onClick.AddListener(() => NextClicked?.Invoke());
            _retryButton.onClick.AddListener(() => RetryClicked?.Invoke());
            _menuButton.onClick.AddListener(() => MenuClicked?.Invoke());
        }

        public void ShowWin(int levelNumber, bool hasNextLevel)
        {
            _title.text = hasNextLevel ? "YOU WIN!" : "CHAMPION!";
            _subtitle.text = hasNextLevel ? $"Level {levelNumber} complete" : "All levels complete!";
            Present(_winIcon, next: hasNextLevel, retry: !hasNextLevel);
        }

        public void ShowLose(int levelNumber)
        {
            _title.text = "TIME'S UP!";
            _subtitle.text = $"Level {levelNumber} — try again";
            Present(_loseIcon, next: false, retry: true);
        }

        private void Present(Sprite icon, bool next, bool retry)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _nextButton.gameObject.SetActive(next);
            _retryButton.gameObject.SetActive(retry);
            Show();

            // Trophy / icon bounce after the window pops in.
            Tween.StopAll(_icon.transform);
            _icon.transform.localScale = Vector3.zero;
            Tween.Scale(_icon.transform, 1f, 0.5f, Ease.OutBack, startDelay: 0.2f, useUnscaledTime: true);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Tween.StopAll(_icon.transform);
        }
    }
}
