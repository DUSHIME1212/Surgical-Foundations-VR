using NUnit.Framework;
using SurgicalFoundations.Detection;

namespace SurgicalFoundations.Tests
{
    public class CameraAimTests
    {
        static CameraAim Aim() => new CameraAim(0.15f, 0.03f, 0.30f);

        static void Settle(CameraAim aim)
        {
            for (int i = 0; i < 400 && aim.Moving; i++) aim.Step(0.02f);
        }

        [Test]
        public void A_command_moves_the_target_one_step_and_the_scope_glides_there()
        {
            var aim = Aim();
            Assert.IsTrue(aim.Apply(CameraCommand.PanLeft));
            Assert.AreEqual(-CameraAim.PanStepDeg, aim.TargetYaw);
            Assert.AreEqual(0f, aim.Yaw); // not there yet

            aim.Step(0.1f);
            Assert.AreEqual(-3f, aim.Yaw, 1e-4f); // 30 degrees a second
            Settle(aim);
            Assert.AreEqual(-CameraAim.PanStepDeg, aim.Yaw);
            Assert.IsFalse(aim.Moving);
        }

        [Test]
        public void Opposite_commands_cancel()
        {
            var aim = Aim();
            aim.Apply(CameraCommand.PanUp);
            aim.Apply(CameraCommand.PanDown);
            aim.Apply(CameraCommand.ZoomIn);
            aim.Apply(CameraCommand.ZoomOut);
            Assert.AreEqual(0f, aim.TargetPitch);
            Assert.AreEqual(0.15f, aim.TargetInsertion, 1e-5f);
        }

        [Test]
        public void Panning_stops_at_the_limit()
        {
            var aim = Aim();
            for (int i = 0; i < 4; i++) Assert.IsTrue(aim.Apply(CameraCommand.PanRight));
            Assert.IsTrue(aim.Apply(CameraCommand.PanRight));  // 40 asked for, 35 given
            Assert.AreEqual(35f, aim.TargetYaw);
            Assert.IsFalse(aim.Apply(CameraCommand.PanRight)); // nowhere further to go
        }

        [Test]
        public void Zoom_keeps_the_scope_inside_the_port_and_short_of_the_handle()
        {
            var aim = Aim();
            for (int i = 0; i < 20; i++) aim.Apply(CameraCommand.ZoomOut);
            Assert.AreEqual(0.03f, aim.TargetInsertion, 1e-5f);
            for (int i = 0; i < 40; i++) aim.Apply(CameraCommand.ZoomIn);
            Assert.AreEqual(0.30f, aim.TargetInsertion, 1e-5f);
            Settle(aim);
            Assert.AreEqual(0.30f, aim.Insertion, 1e-5f);
        }
    }
}
