using UnityEngine;

namespace GodTower.Levels
{
    /// <summary>Instantiates the tower of a level under a root transform, from a <see cref="TowerSet"/>.</summary>
    public sealed class TowerBuilder
    {
        /// <summary>Segments below the start height so the column never visibly ends under the climber.</summary>
        public const int ExtraSegmentsBelow = 6;

        private readonly TowerSet _set;
        private readonly Transform _root;

        public TowerBuilder(TowerSet set, Transform root)
        {
            _set = set;
            _root = root;
        }

        public TowerLayout Build(float climbHeight, int seed)
        {
            var layout = new TowerLayout(climbHeight, _set.SegmentHeight, ExtraSegmentsBelow);
            var random = new System.Random(seed);
            int previous = -1;

            for (int i = 0; i < layout.SegmentCount; i++)
            {
                var position = new Vector3(0f, layout.SegmentY(i), 0f);
                int count = _set.Segments.Count;
                if (count == 0)
                {
                    CreatePlaceholderSegment(position, i);
                    continue;
                }

                // Random order without immediate repeats, deterministic per level.
                int pick = random.Next(count);
                if (pick == previous && count > 1)
                    pick = (pick + 1 + random.Next(count - 1)) % count;
                previous = pick;

                GameObject segment = Object.Instantiate(_set.Segments[pick], position, Quaternion.identity, _root);
                segment.name = $"Segment_{i:000}";
            }

            CreateTop(layout.TopY);
            return layout;
        }

        /// <summary>Where the climber stands after climbing over the top edge.</summary>
        public Vector3 DeckPoint(TowerLayout layout) =>
            new(0f, layout.TopY + _set.DeckHeight, -_set.DeckLandingRadius);

        private void CreateTop(float topY)
        {
            var position = new Vector3(0f, topY, 0f);
            if (_set.Top != null)
            {
                Object.Instantiate(_set.Top, position, Quaternion.identity, _root).name = "Top";
                return;
            }

            const float deckThickness = 0.4f;
            float deckRadius = _set.DeckLandingRadius * 2f;
            GameObject deck = CreatePrimitive("Top",
                position + Vector3.up * (_set.DeckHeight - deckThickness * 0.5f),
                new Vector3(deckRadius * 2f, deckThickness * 0.5f, deckRadius * 2f));
            CreatePrimitive("TopNeck", position + Vector3.up * (_set.DeckHeight * 0.5f),
                new Vector3(_set.ColumnRadius * 2f, _set.DeckHeight * 0.5f, _set.ColumnRadius * 2f)).transform.SetParent(deck.transform, true);
        }

        private void CreatePlaceholderSegment(Vector3 basePosition, int index)
        {
            float height = _set.SegmentHeight;
            float diameter = _set.ColumnRadius * 2f;
            // Unity's cylinder primitive is 2 units tall with its pivot at the centre.
            GameObject segment = CreatePrimitive($"Segment_{index:000}",
                basePosition + Vector3.up * (height * 0.5f),
                new Vector3(diameter, height * 0.5f, diameter));

            // A thin ring per segment gives the placeholder column a readable vertical rhythm.
            CreatePrimitive("Ring", basePosition, new Vector3(diameter * 1.15f, 0.08f, diameter * 1.15f))
                .transform.SetParent(segment.transform, true);
        }

        private GameObject CreatePrimitive(string name, Vector3 position, Vector3 scale)
        {
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            primitive.name = name;
            Object.Destroy(primitive.GetComponent<Collider>());
            primitive.transform.SetParent(_root, false);
            primitive.transform.localPosition = position;
            primitive.transform.localScale = scale;
            if (_set.PlaceholderMaterial != null)
                primitive.GetComponent<MeshRenderer>().sharedMaterial = _set.PlaceholderMaterial;
            return primitive;
        }
    }
}
