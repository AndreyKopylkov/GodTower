using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>In-level HUD: height bar, level title, timer and the pause button.</summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private HeightBarView _heightBar;
        [SerializeField] private TimerView _timer;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private Button _pauseButton;

        public HeightBarView HeightBar => _heightBar;
        public TimerView Timer => _timer;
        public Button PauseButton => _pauseButton;

        public event Action PauseClicked;

        private void Awake() => _pauseButton.onClick.AddListener(() => PauseClicked?.Invoke());

        public void SetLevel(int number) => _levelLabel.text = $"LEVEL {number}";

        public void SetPauseInteractable(bool interactable) => _pauseButton.interactable = interactable;
    }
}
