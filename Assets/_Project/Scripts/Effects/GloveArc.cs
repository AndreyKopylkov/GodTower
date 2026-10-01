using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>
    /// Flight path of one glove relative to its (moving) target: a straight approach from <see cref="StartOffset"/>
    /// bent sideways by a half-sine, with a roll that unwinds to zero at contact (the punch-in twist).
    /// t runs from 0 (spawn) to 1 (contact).
    /// </summary>
    public readonly struct GloveArc
    {
        public readonly Vector3 StartOffset;
        public readonly Vector3 Bend;
        public readonly float Roll;

        public GloveArc(Vector3 startOffset, Vector3 bend, float roll)
        {
            StartOffset = startOffset;
            // Keep the bend perpendicular to the approach so the glove never stalls mid-flight.
            Bend = Vector3.ProjectOnPlane(bend, startOffset);
            Roll = roll;
        }

        /// <summary>Offset from the target at <paramref name="t"/> (zero at contact).</summary>
        public Vector3 Offset(float t) => StartOffset * (1f - t) + Bend * Mathf.Sin(t * Mathf.PI);

        /// <summary>Direction of travel at <paramref name="t"/> (derivative of <see cref="Offset"/>).</summary>
        public Vector3 Velocity(float t) => -StartOffset + Bend * (Mathf.PI * Mathf.Cos(t * Mathf.PI));

        /// <summary>Direction the glove travels at contact.</summary>
        public Vector3 PunchDirection => Velocity(1f).normalized;

        /// <summary>Glove orientation at <paramref name="t"/>: the fist (+Z) leads, rolled by the remaining twist.</summary>
        public Quaternion Rotation(float t) =>
            Quaternion.LookRotation(Velocity(t), Vector3.up) * Quaternion.Euler(0f, 0f, Roll * (1f - t));
    }
}
