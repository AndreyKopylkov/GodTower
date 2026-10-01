using System;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>Pause menu: Resume / Restart / Menu.</summary>
    public sealed class PausePanelView : PopupView
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;

        public Button ResumeButton => _resumeButton;
        public Button RestartButton => _restartButton;
        public Button MenuButton => _menuButton;

        public event Action ResumeClicked;
        public event Action RestartClicked;
        public event Action MenuClicked;

        private void Awake()
        {
            _resumeButton.onClick.AddListener(() => ResumeClicked?.Invoke());
            _restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
            _menuButton.onClick.AddListener(() => MenuClicked?.Invoke());
        }
    }
}
