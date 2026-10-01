using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>Main menu (logo, Play, Levels) and the level-select popup.</summary>
    public sealed class MenuView : MonoBehaviour
    {
        [SerializeField] private RectTransform _logo;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _levelsButton;
        [SerializeField] private PopupView _levelSelect;
        [SerializeField] private Button _backButton;
        [SerializeField] private LevelButtonView[] _levelButtons = Array.Empty<LevelButtonView>();

        public Button PlayButton => _playButton;
        public Button LevelsButton => _levelsButton;
        public Button BackButton => _backButton;
        public IReadOnlyList<LevelButtonView> LevelButtons => _levelButtons;
        public bool IsLevelSelectVisible => _levelSelect.IsVisible;

        public event Action PlayClicked;
        public event Action LevelsClicked;
        public event Action BackClicked;

        /// <summary>The argument is the 0-based level index.</summary>
        public event Action<int> LevelClicked;

        private void Awake()
        {
            _playButton.onClick.AddListener(() => PlayClicked?.Invoke());
            _levelsButton.onClick.AddListener(() => LevelsClicked?.Invoke());
            _backButton.onClick.AddListener(() => BackClicked?.Invoke());
            for (int i = 0; i < _levelButtons.Length; i++)
            {
                int index = i;
                _levelButtons[i].Clicked += () => LevelClicked?.Invoke(index);
            }
        }

        private void Start()
        {
            // Idle bob of the logo and a gentle pulse of the Play button.
            Tween.UIAnchoredPositionY(_logo, _logo.anchoredPosition.y + 18f, 1.6f, Ease.InOutSine, -1, CycleMode.Yoyo, useUnscaledTime: true);
            Tween.Scale(_playButton.transform, 1.05f, 0.8f, Ease.InOutSine, -1, CycleMode.Yoyo, useUnscaledTime: true);
        }

        public void ShowLevelSelect() => _levelSelect.Show();

        public void HideLevelSelect() => _levelSelect.Hide();

        private void OnDestroy()
        {
            Tween.StopAll(_logo);
            Tween.StopAll(_playButton.transform);
        }
    }
}
