using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SurgicalFoundations.Contracts;
using SurgicalFoundations.Detection;

namespace SurgicalFoundations.Tests
{
    public class TrayAndBleedTests
    {
        readonly List<(EventClass cls, string code, string message)> events = new List<(EventClass, string, string)>();

        [SetUp]
        public void Clear() => events.Clear();

        void Log(EventClass cls, string code, string message) => events.Add((cls, code, message));
        int Count(string code) => events.Count(e => e.code == code);

        // ───────────── opening count ─────────────

        TrayCheck Tray() => new TrayCheck(new[] { ("Trocar, 5 mm", 2), ("Scissors", 1), ("Swabs", 3) }, Log);

        [Test]
        public void A_line_is_done_when_every_item_on_it_is_confirmed()
        {
            var tray = Tray();
            Assert.IsTrue(tray.Confirm(0));
            Assert.IsFalse(tray.LineDone(0));
            Assert.AreEqual(0, events.Count);
            Assert.IsTrue(tray.Confirm(0));
            Assert.IsTrue(tray.LineDone(0));
            Assert.IsFalse(tray.Confirm(0)); // a third trocar isn't on the table
            Assert.AreEqual(1, Count(EventCodes.TrayItemConfirmed));
            Assert.AreEqual(2, tray.TotalConfirmed);
            Assert.AreEqual(6, tray.TotalItems);
        }

        [Test]
        public void Counting_everything_completes_the_opening_count()
        {
            var tray = Tray();
            foreach (var line in new[] { 0, 0, 1, 2, 2, 2 }) tray.Confirm(line);
            Assert.IsTrue(tray.Complete);
            Assert.AreEqual(1, Count(EventCodes.TrayComplete));
            tray.SkipRemaining();
            Assert.IsFalse(events.Any(e => e.cls == EventClass.Deviation));
        }

        [Test]
        public void Confirming_the_field_early_logs_each_unfinished_line()
        {
            var tray = Tray();
            tray.Confirm(1);
            tray.Confirm(2);
            tray.SkipRemaining();
            Assert.AreEqual(2, tray.LinesUnchecked);
            Assert.AreEqual(2, Count(EventCodes.TrayItemUnchecked));
            StringAssert.Contains("1 of 3", events.Last().message);
        }

        // ───────────── port-site bleed ─────────────

        static void Run(BleedControl bleed, bool withdrawn, bool pressing, bool watching, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.1f) bleed.Tick(withdrawn, pressing, watching, 0.1f);
        }

        [Test]
        public void Withdraw_press_then_watch_controls_the_bleed()
        {
            var bleed = new BleedControl("Right working", 5f, 3f, Log);
            Run(bleed, false, true, true, 2f); // pressing with the trocar still deep does nothing
            Assert.AreEqual(BleedPhase.Withdraw, bleed.Phase);

            Run(bleed, true, false, false, 0.2f);
            Assert.AreEqual(BleedPhase.Pressure, bleed.Phase);
            Run(bleed, true, true, false, 5.2f);
            Assert.AreEqual(BleedPhase.Observe, bleed.Phase);
            Run(bleed, true, false, true, 3.2f);

            Assert.IsTrue(bleed.Controlled);
            Assert.AreEqual(StepState.Done, bleed.State(BleedPhase.Observe));
            var done = events.Single(e => e.code == EventCodes.BleedControlled);
            Assert.AreEqual(EventClass.Delayed, done.cls); // recovered, but it happened
            Assert.Greater(bleed.Elapsed, 10f);
        }

        [Test]
        public void Letting_go_early_restarts_the_hold()
        {
            var bleed = new BleedControl("Camera", 5f, 3f, Log);
            Run(bleed, true, false, false, 0.2f);
            Run(bleed, true, true, false, 3f);
            Run(bleed, true, false, false, 0.2f);
            Assert.AreEqual(1, bleed.Interruptions);
            Assert.AreEqual(5f, bleed.Remaining, 0.01f);
            Run(bleed, true, true, false, 3f);
            Assert.AreEqual(BleedPhase.Pressure, bleed.Phase);
        }

        [Test]
        public void Watching_only_counts_with_the_hand_off_and_eyes_on_the_site()
        {
            var bleed = new BleedControl("Camera", 1f, 3f, Log);
            Run(bleed, true, false, false, 0.2f);
            Run(bleed, true, true, false, 1.2f);
            Assert.AreEqual(BleedPhase.Observe, bleed.Phase);
            Run(bleed, true, true, true, 4f);   // still pressing
            Run(bleed, true, false, false, 4f); // looking away
            Assert.IsFalse(bleed.Controlled);
            Run(bleed, true, false, true, 3.2f);
            Assert.IsTrue(bleed.Controlled);
        }

        [Test]
        public void Carrying_on_without_control_is_a_deviation()
        {
            var bleed = new BleedControl("Camera", 5f, 3f, Log);
            Run(bleed, true, false, false, 0.2f);
            bleed.Skip();
            bleed.Skip();
            Assert.IsTrue(bleed.Skipped);
            Assert.AreEqual(1, Count(EventCodes.BleedUnmanaged));
            Assert.AreEqual(StepState.Done, bleed.State(BleedPhase.Withdraw));
            Assert.AreEqual(StepState.Skipped, bleed.State(BleedPhase.Pressure));
            Run(bleed, true, true, true, 10f);
            Assert.IsFalse(bleed.Controlled);
        }
    }
}
