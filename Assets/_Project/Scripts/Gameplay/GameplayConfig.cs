using UnityEngine;

namespace GodTower.Gameplay
{
    /// <summary>Level-independent gameplay tuning.</summary>
    [CreateAssetMenu(menuName = "God Tower/Gameplay Config", fileName = "GameplayConfig")]
    public sealed class GameplayConfig : ScriptableObject
    {
        [SerializeField] private ClimberSettings _climber = new();
        [SerializeField] private KnockdownSettings _knockdown = new();

        [Tooltip("Duration of the hop from the last segment onto the top deck.")]
        [SerializeField, Min(0.1f)] private float _winHopDuration = 0.7f;

        public ClimberSettings Climber => _climber;
        public KnockdownSettings Knockdown => _knockdown;
        public float WinHopDuration => _winHopDuration;
    }
}
