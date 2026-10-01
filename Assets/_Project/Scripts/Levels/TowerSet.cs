using System;
using System.Collections.Generic;
using UnityEngine;

namespace GodTower.Levels
{
    /// <summary>
    /// Modular tower pieces: stackable column segments (pivot at the base centre, all the same height)
    /// and the top platform (pivot at the joint with the last segment). Empty slots fall back to primitives.
    /// </summary>
    [CreateAssetMenu(menuName = "God Tower/Tower Set", fileName = "TowerSet")]
    public sealed class TowerSet : ScriptableObject
    {
        [SerializeField] private GameObject[] _segments = Array.Empty<GameObject>();
        [SerializeField, Min(0.5f)] private float _segmentHeight = 3f;
        [SerializeField] private GameObject _top;

        [Tooltip("Height of the walkable deck above the top pivot.")]
        [SerializeField, Min(0f)] private float _deckHeight = 3.4f;

        [Tooltip("Distance from the axis where the climber lands on the deck.")]
        [SerializeField, Min(0f)] private float _deckLandingRadius = 3f;

        [Tooltip("Column radius of the primitive placeholder.")]
        [SerializeField, Min(0.1f)] private float _columnRadius = 1.5f;

        [Tooltip("Material of the primitive placeholders.")]
        [SerializeField] private Material _placeholderMaterial;

        public IReadOnlyList<GameObject> Segments => _segments;
        public float SegmentHeight => _segmentHeight;
        public GameObject Top => _top;
        public float DeckHeight => _deckHeight;
        public float DeckLandingRadius => _deckLandingRadius;
        public float ColumnRadius => _columnRadius;
        public Material PlaceholderMaterial => _placeholderMaterial;
    }
}
