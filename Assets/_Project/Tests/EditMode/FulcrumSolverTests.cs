using NUnit.Framework;
using SurgicalFoundations.Interaction;
using UnityEngine;

namespace SurgicalFoundations.Tests
{
    public class FulcrumSolverTests
    {
        // A laparoscopic grasper as built: pistol grip below and behind the shaft, 35 cm of shaft and jaws.
        static readonly Vector3 Grip = new Vector3(0f, -0.055f, -0.03f);
        const float TipZ = 0.38f, MinZ = 0.05f, MaxZ = 0.374f;
        static readonly Vector3 Port = new Vector3(0f, 1.17f, 0f);

        static Vector3 TipOf(FulcrumPose pose) => pose.position + pose.rotation * (Vector3.forward * TipZ);

        static float DistanceFromAxis(FulcrumPose pose, Vector3 point)
        {
            var forward = pose.rotation * Vector3.forward;
            var to = point - pose.position;
            return (to - forward * Vector3.Dot(to, forward)).magnitude;
        }

        [Test]
        public void Shaft_always_passes_through_the_port()
        {
            var hands = new[]
            {
                new Vector3(0f, 1.45f, -0.1f), new Vector3(0.2f, 1.35f, -0.15f), new Vector3(-0.18f, 1.5f, 0.05f),
                new Vector3(0.05f, 1.22f, -0.3f), new Vector3(0f, 2.2f, 0f), new Vector3(0f, 1.19f, 0.01f)
            };
            foreach (var hand in hands)
            {
                var pose = FulcrumSolver.Solve(hand, Vector3.up, Port, Grip, MinZ, MaxZ);
                Assert.Less(DistanceFromAxis(pose, Port), 1e-4f, "hand " + hand);
            }
        }

        [Test]
        public void Hand_stays_on_the_grip_while_within_the_shaft_limits()
        {
            var hand = new Vector3(0.12f, 1.38f, -0.14f);
            var pose = FulcrumSolver.Solve(hand, Vector3.up, Port, Grip, MinZ, MaxZ);
            Assert.AreEqual(pose.rawPortZ, pose.portZ, 1e-5f);
            Assert.Less((pose.position + pose.rotation * Grip - hand).magnitude, 0.001f);
        }

        [Test]
        public void Moving_the_hand_left_swings_the_tip_right()
        {
            var centre = FulcrumSolver.Solve(new Vector3(0f, 1.42f, -0.05f), Vector3.forward, Port, Grip, MinZ, MaxZ);
            var left = FulcrumSolver.Solve(new Vector3(-0.08f, 1.42f, -0.05f), Vector3.forward, Port, Grip, MinZ, MaxZ);
            Assert.Greater(TipOf(left).x, TipOf(centre).x + 0.02f);
        }

        [Test]
        public void Pushing_in_stops_when_the_handle_reaches_the_port()
        {
            var pose = FulcrumSolver.Solve(Port + new Vector3(0f, 0.02f, -0.01f), Vector3.forward, Port, Grip, MinZ, MaxZ);
            Assert.AreEqual(MinZ, pose.portZ, 1e-5f);
            Assert.Less(pose.rawPortZ, MinZ);
        }

        [Test]
        public void Pulling_out_stops_with_the_tip_at_the_port_and_reports_the_overshoot()
        {
            var pose = FulcrumSolver.Solve(Port + Vector3.up * 0.6f, Vector3.forward, Port, Grip, MinZ, MaxZ);
            Assert.AreEqual(MaxZ, pose.portZ, 1e-5f);
            Assert.Greater(pose.rawPortZ, MaxZ + 0.04f); // enough for the instrument to come out
            Assert.Less((TipOf(pose) - Port).magnitude, 0.01f);
        }

        [Test]
        public void Wrist_roll_rolls_the_shaft()
        {
            var hand = new Vector3(0f, 1.45f, 0f) - new Vector3(0f, 0f, 0.03f);
            var a = FulcrumSolver.Solve(hand, Vector3.forward, Port, Grip, MinZ, MaxZ);
            var rolled = Quaternion.AngleAxis(60f, a.rotation * Vector3.forward) * Vector3.forward;
            var b = FulcrumSolver.Solve(hand, rolled, Port, Grip, MinZ, MaxZ);
            var angle = Vector3.Angle(a.rotation * Vector3.up, b.rotation * Vector3.up);
            Assert.AreEqual(60f, angle, 6f);
        }

        [Test]
        public void Hand_up_along_the_shaft_still_gives_a_valid_pose()
        {
            var pose = FulcrumSolver.Solve(Port + Vector3.up * 0.3f, Vector3.down, Port, Grip, MinZ, MaxZ);
            Assert.IsFalse(float.IsNaN(pose.position.x) || float.IsNaN(pose.rotation.x));
            Assert.Less(DistanceFromAxis(pose, Port), 1e-4f);
        }
    }
}
