using System;
using System.Collections.Generic;

namespace GodTower.Gameplay
{
    /// <summary>
    /// Lanes on the front arc of the column, ordered from screen-left to screen-right
    /// (angles in degrees around the column axis, 0 = facing the camera). Shifting past an edge is rejected.
    /// </summary>
    public sealed class LaneModel
    {
        private readonly float[] _angles;

        public LaneModel(IReadOnlyList<float> anglesDegrees, int startIndex)
        {
            if (anglesDegrees == null || anglesDegrees.Count == 0)
                throw new ArgumentException("At least one lane is required.", nameof(anglesDegrees));
            if (startIndex < 0 || startIndex >= anglesDegrees.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            _angles = new float[anglesDegrees.Count];
            for (int i = 0; i < _angles.Length; i++)
                _angles[i] = anglesDegrees[i];

            Index = startIndex;
        }

        public int Count => _angles.Length;

        public int Index { get; private set; }

        public float Angle => _angles[Index];

        public int CenterIndex => Count / 2;

        public float AngleOf(int index) => _angles[index];

        public bool IsValid(int index) => index >= 0 && index < Count;

        /// <summary>Moves one lane towards <paramref name="direction"/> (-1 left, +1 right).</summary>
        /// <returns><c>false</c> if already at that edge (lane unchanged).</returns>
        public bool TryShift(int direction)
        {
            int target = Index + Math.Sign(direction);
            if (direction == 0 || !IsValid(target))
                return false;

            Index = target;
            return true;
        }
    }
}
