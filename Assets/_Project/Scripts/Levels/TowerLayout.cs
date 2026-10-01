using System;
using UnityEngine;

namespace GodTower.Levels
{
    /// <summary>
    /// Where tower segments go so that the climbable top lands exactly on the level height:
    /// the stack is shifted down (base below height 0) when the height is not a multiple of the segment height,
    /// and a few extra segments continue below the start so the camera never sees the column end.
    /// </summary>
    public readonly struct TowerLayout
    {
        public TowerLayout(float climbHeight, float segmentHeight, int extraSegmentsBelow)
        {
            if (climbHeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(climbHeight));
            if (segmentHeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(segmentHeight));

            ClimbHeight = climbHeight;
            SegmentHeight = segmentHeight;
            int aboveStart = Mathf.CeilToInt(climbHeight / segmentHeight - 0.0001f);
            SegmentCount = aboveStart + Mathf.Max(0, extraSegmentsBelow);
            BaseY = climbHeight - SegmentCount * segmentHeight;
        }

        public float ClimbHeight { get; }

        public float SegmentHeight { get; }

        public int SegmentCount { get; }

        /// <summary>World Y of the lowest segment's pivot.</summary>
        public float BaseY { get; }

        /// <summary>World Y of the top platform pivot (= climbable height).</summary>
        public float TopY => ClimbHeight;

        public float SegmentY(int index) => BaseY + index * SegmentHeight;
    }
}
