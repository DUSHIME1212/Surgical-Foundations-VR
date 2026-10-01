using UnityEngine;

namespace SurgicalFoundations.Interaction
{
    public struct FulcrumPose
    {
        public Vector3 position;
        public Quaternion rotation;
        /// <summary>Distance along the shaft from the instrument's origin to the port (before clamping).</summary>
        public float rawPortZ;
        /// <summary>The same distance after the shaft limits were applied.</summary>
        public float portZ;
    }

    /// <summary>
    /// The fulcrum effect of keyhole surgery (FR-10): the shaft must pass through the port, so the hand can only pivot
    /// the instrument about that point, slide it in and out, and roll it. Moving the hand left swings the tip right.
    /// </summary>
    public static class FulcrumSolver
    {
        /// <param name="grip">Where the hand holds the instrument (world).</param>
        /// <param name="handUp">The hand's up direction: its roll about the shaft becomes the instrument's roll.</param>
        /// <param name="port">The fulcrum point on the abdominal wall (world).</param>
        /// <param name="gripLocal">The grip point in the instrument's own space (long axis +Z, tip at +Z).</param>
        /// <param name="minPortZ">Closest the port may come to the handle (fully inserted).</param>
        /// <param name="maxPortZ">Furthest the port may be along the shaft (tip at the port: withdrawn).</param>
        public static FulcrumPose Solve(Vector3 grip, Vector3 handUp, Vector3 port, Vector3 gripLocal, float minPortZ, float maxPortZ)
        {
            var forward = port - grip;
            if (forward.sqrMagnitude < 1e-8f) forward = Vector3.down;
            forward.Normalize();

            // The grip sits off the shaft's axis (pistol handle), so aim the axis, not the grip, at the port.
            // A few passes settle it: the offset is centimetres against a lever of tens of centimetres.
            var up = Vector3.up;
            for (int i = 0; i < 4; i++)
            {
                up = Perpendicular(handUp, forward);
                var right = Vector3.Cross(up, forward);
                var origin = grip - (right * gripLocal.x + up * gripLocal.y + forward * gripLocal.z);
                var aim = port - origin;
                if (aim.sqrMagnitude < 1e-8f) break;
                forward = aim.normalized;
            }
            up = Perpendicular(handUp, forward);

            var rotation = Quaternion.LookRotation(forward, up);
            var rawZ = Vector3.Dot(port - (grip - rotation * gripLocal), forward);
            var z = Mathf.Clamp(rawZ, minPortZ, maxPortZ);
            return new FulcrumPose
            {
                rotation = rotation,
                // Measured back from the port, so the axis passes exactly through it whatever the hand does.
                position = port - forward * z,
                rawPortZ = rawZ,
                portZ = z
            };
        }

        static Vector3 Perpendicular(Vector3 v, Vector3 axis)
        {
            var p = v - axis * Vector3.Dot(v, axis);
            if (p.sqrMagnitude < 1e-6f)
            {
                // Hand up lies along the shaft: fall back to any stable perpendicular.
                p = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right);
            }
            return p.normalized;
        }
    }
}
