using System;

namespace SurgicalFoundations.Detection
{
    public enum CameraCommand { PanLeft, PanRight, PanUp, PanDown, ZoomIn, ZoomOut }

    /// <summary>
    /// Where the camera assistant is holding the laparoscope: pan angles about the port and how far the scope is in.
    /// Each command moves the target by one step, within limits; the scope then glides to the target the way a person
    /// would move it, not in a jump.
    /// </summary>
    public class CameraAim
    {
        public const float PanStepDeg = 8f;
        public const float ZoomStep = 0.02f;
        public const float PanSpeedDeg = 30f;   // per second
        public const float ZoomSpeed = 0.06f;   // metres per second

        readonly float maxPanDeg;
        readonly float minInsertion;
        readonly float maxInsertion;

        public CameraAim(float insertion, float minInsertion, float maxInsertion, float maxPanDeg = 35f)
        {
            this.minInsertion = minInsertion;
            this.maxInsertion = maxInsertion;
            this.maxPanDeg = maxPanDeg;
            Insertion = TargetInsertion = Clamp(insertion, minInsertion, maxInsertion);
        }

        /// <summary>Degrees right of where the scope started (negative = left).</summary>
        public float Yaw { get; private set; }
        /// <summary>Degrees below where the scope started (negative = up).</summary>
        public float Pitch { get; private set; }
        public float Insertion { get; private set; }
        public float TargetYaw { get; private set; }
        public float TargetPitch { get; private set; }
        public float TargetInsertion { get; private set; }
        public bool Moving => Yaw != TargetYaw || Pitch != TargetPitch || Insertion != TargetInsertion;

        /// <summary>False if the scope is already at its limit that way.</summary>
        public bool Apply(CameraCommand command)
        {
            float yaw = TargetYaw, pitch = TargetPitch, insertion = TargetInsertion;
            switch (command)
            {
                case CameraCommand.PanLeft: yaw -= PanStepDeg; break;
                case CameraCommand.PanRight: yaw += PanStepDeg; break;
                case CameraCommand.PanUp: pitch -= PanStepDeg; break;
                case CameraCommand.PanDown: pitch += PanStepDeg; break;
                case CameraCommand.ZoomIn: insertion += ZoomStep; break;
                case CameraCommand.ZoomOut: insertion -= ZoomStep; break;
            }
            yaw = Clamp(yaw, -maxPanDeg, maxPanDeg);
            pitch = Clamp(pitch, -maxPanDeg, maxPanDeg);
            insertion = Clamp(insertion, minInsertion, maxInsertion);
            var changed = yaw != TargetYaw || pitch != TargetPitch || insertion != TargetInsertion;
            TargetYaw = yaw; TargetPitch = pitch; TargetInsertion = insertion;
            return changed;
        }

        public void Step(float dt)
        {
            Yaw = Towards(Yaw, TargetYaw, PanSpeedDeg * dt);
            Pitch = Towards(Pitch, TargetPitch, PanSpeedDeg * dt);
            Insertion = Towards(Insertion, TargetInsertion, ZoomSpeed * dt);
        }

        static float Towards(float value, float target, float maxDelta) =>
            Math.Abs(target - value) <= maxDelta ? target : value + Math.Sign(target - value) * maxDelta;

        static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
    }
}
