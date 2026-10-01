using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SurgicalFoundations.Contracts;
using SurgicalFoundations.Detection;
using UnityEngine;

namespace SurgicalFoundations.Tests
{
    public class DetectionLogicTests
    {
        readonly List<(EventClass cls, string code, string message)> events = new List<(EventClass, string, string)>();

        [SetUp]
        public void Clear() => events.Clear();

        void Log(EventClass cls, string code, string message) => events.Add((cls, code, message));
        int Count(string code) => events.Count(e => e.code == code);

        // ───────────── scrub ─────────────

        static ScrubSample ActionFor(int step)
        {
            switch ((ScrubStep)step)
            {
                case ScrubStep.Inspect: return new ScrubSample { inspecting = true };
                case ScrubStep.PreWash: return new ScrubSample { inWater = true };
                case ScrubStep.CleanNails: return new ScrubSample { inWater = true, handsTogether = true };
                case ScrubStep.Scrub: return new ScrubSample { handsTogether = true, rubbing = true };
                case ScrubStep.Rinse: return new ScrubSample { inWater = true, fingertipsUp = true };
                default: return new ScrubSample { atTowel = true };
            }
        }

        static void Hold(ScrubSequence scrub, ScrubSample sample, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.1f) scrub.Tick(sample, 0.1f);
        }

        [Test]
        public void Scrub_in_order_completes_with_no_deviation()
        {
            var scrub = new ScrubSequence(15f, Log);
            for (int step = 0; step < ScrubSequence.StepCount; step++)
            {
                Assert.AreEqual(step, scrub.Expected);
                Assert.AreEqual(StepState.Active, scrub.State(step));
                Hold(scrub, ActionFor(step), 16f);
                Assert.AreEqual(StepState.Done, scrub.State(step));
            }
            Assert.IsTrue(scrub.Complete);
            Assert.AreEqual(6, Count(EventCodes.ScrubStepDone));
            Assert.AreEqual(1, Count(EventCodes.ScrubComplete));
            Assert.IsFalse(events.Any(e => e.cls == EventClass.Deviation));
        }

        [Test]
        public void Scrub_step_needs_its_full_time()
        {
            var scrub = new ScrubSequence(15f, Log);
            Hold(scrub, ActionFor(0), 3f);
            Hold(scrub, ActionFor(1), 6f);
            Hold(scrub, ActionFor(2), 6f);
            Hold(scrub, ActionFor(3), 10f);
            Assert.AreEqual((int)ScrubStep.Scrub, scrub.Expected);
            Assert.AreEqual(5f, scrub.RemainingSeconds(3), 0.3f);
        }

        [Test]
        public void Jumping_ahead_skips_the_steps_between_as_deviations()
        {
            var scrub = new ScrubSequence(15f, Log);
            Hold(scrub, ActionFor(0), 3f);
            // Straight from inspection to scrubbing: pre-wash and nails never happened.
            Hold(scrub, ActionFor((int)ScrubStep.Scrub), 3f);
            Assert.AreEqual(StepState.Skipped, scrub.State(1));
            Assert.AreEqual(StepState.Skipped, scrub.State(2));
            Assert.AreEqual(StepState.Active, scrub.State(3));
            Assert.AreEqual(2, Count(EventCodes.ScrubStepSkipped));
        }

        [Test]
        public void A_brief_wrong_action_does_not_skip_anything()
        {
            var scrub = new ScrubSequence(15f, Log);
            Hold(scrub, ActionFor((int)ScrubStep.Dry), 1f);
            Assert.AreEqual(0, scrub.Expected);
            Assert.AreEqual(0, events.Count);
        }

        [Test]
        public void Skipping_the_rest_logs_every_unfinished_step()
        {
            var scrub = new ScrubSequence(15f, Log);
            Hold(scrub, ActionFor(0), 3f);
            scrub.SkipRemaining();
            Assert.IsTrue(scrub.Complete);
            Assert.AreEqual(5, Count(EventCodes.ScrubStepSkipped));
            Assert.AreEqual(StepState.Done, scrub.State(0));
        }

        [Test]
        public void Dropping_hands_after_the_rinse_is_logged_once()
        {
            var scrub = new ScrubSequence(15f, Log);
            for (int step = 0; step <= (int)ScrubStep.Rinse; step++) Hold(scrub, ActionFor(step), 16f);
            Hold(scrub, new ScrubSample { handsDropped = true }, 2f);
            Assert.AreEqual(1, Count(EventCodes.ScrubHandsDropped));
        }

        // ───────────── sterility ─────────────

        [Test]
        public void Only_a_gloved_hand_can_be_contaminated()
        {
            var state = new SterilityState(Log);
            state.Contact(true, "the back table edge");
            Assert.IsFalse(state.AnyContaminated);

            state.MarkScrubbed();
            state.Glove();
            Assert.IsTrue(state.Sterile);
            Assert.AreEqual(1, Count(EventCodes.Gloved));

            state.Contact(true, "the back table edge");
            state.Contact(true, "the back table edge"); // still the same break
            Assert.AreEqual(HandSterility.Contaminated, state.Left);
            Assert.AreEqual(HandSterility.Gloved, state.Right);
            Assert.AreEqual(1, Count(EventCodes.Contamination));
            StringAssert.Contains("Left glove", state.LastBreak);
        }

        [Test]
        public void Regloving_restores_sterility_and_is_logged()
        {
            var state = new SterilityState(Log);
            state.StartGloved();
            Assert.AreEqual(0, events.Count);
            state.Contact(false, "the floor");
            state.Glove();
            Assert.IsTrue(state.Sterile);
            Assert.AreEqual(1, Count(EventCodes.Regloved));
        }

        // ───────────── port sites ─────────────

        PortSitePlan Plan() => new PortSitePlan(new[]
        {
            ("Camera", new Vector3(0f, 1f, 0f)), ("Left working", new Vector3(-0.1f, 1f, 0f)), ("Right working", new Vector3(0.1f, 1f, 0f)),
        }, 0.02f, Log);

        [Test]
        public void Mark_on_target_and_off_target()
        {
            var plan = Plan();
            var camera = plan.Mark(new Vector3(0.005f, 1.3f, 0f)); // height above the skin is ignored
            Assert.AreEqual("Camera", camera.name);
            Assert.IsTrue(camera.onTarget);
            Assert.AreEqual(5f, camera.errorMm, 0.1f);

            var left = plan.Mark(new Vector3(-0.14f, 1f, 0f));
            Assert.AreEqual("Left working", left.name);
            Assert.IsFalse(left.onTarget);
            Assert.AreEqual(1, Count(EventCodes.PortOffTarget));
            Assert.IsFalse(plan.AllMarked);
        }

        [Test]
        public void Remarking_corrects_a_port_without_a_second_deviation()
        {
            var plan = Plan();
            plan.Mark(new Vector3(0f, 1f, 0f));
            plan.Mark(new Vector3(0.1f, 1f, 0f));
            plan.Mark(new Vector3(-0.14f, 1f, 0f));
            Assert.IsTrue(plan.AllMarked);
            Assert.IsFalse(plan.AllOnTarget);

            plan.Mark(new Vector3(-0.13f, 1f, 0f)); // still off: no new deviation
            plan.Mark(new Vector3(-0.105f, 1f, 0f));
            Assert.IsTrue(plan.AllOnTarget);
            Assert.AreEqual(1, Count(EventCodes.PortOffTarget));
        }

        [Test]
        public void A_stray_mark_far_from_every_site_is_ignored()
        {
            var plan = Plan();
            Assert.IsNull(plan.Mark(new Vector3(0.6f, 1f, 0.4f)));
            plan.SkipUnmarked();
            Assert.AreEqual(3, Count(EventCodes.PortMarkingSkipped));
        }

        // ───────────── trocar entry ─────────────

        /// <summary>Pushes at a steady speed to a depth, then holds there.</summary>
        static void Insert(TrocarEntry entry, float toDepth, float angle, float speed, float holdSeconds)
        {
            const float dt = 1f / 72f;
            for (float d = 0f; d < toDepth && !entry.Finished; d += speed * dt) entry.Tick(d, angle, dt);
            for (float t = 0f; t < holdSeconds && !entry.Finished; t += dt) entry.Tick(toDepth, angle, dt);
        }

        [Test]
        public void Steady_entry_on_axis_is_placed()
        {
            var entry = new TrocarEntry("Camera", new TrocarLimits(), Log);
            Insert(entry, 0.045f, 5f, 0.03f, 1.2f);
            Assert.AreEqual(TrocarPhase.Placed, entry.Phase);
            Assert.AreEqual(1, Count(EventCodes.TrocarPlaced));
            Assert.IsFalse(events.Any(e => e.cls != EventClass.OnProtocol));
        }

        [Test]
        public void Off_axis_entry_is_flagged_and_not_placed_until_corrected()
        {
            var entry = new TrocarEntry("Camera", new TrocarLimits(), Log);
            Insert(entry, 0.045f, 30f, 0.03f, 1.5f);
            Assert.AreEqual(TrocarPhase.Entering, entry.Phase);
            Assert.IsTrue(entry.AngleWarning);
            Assert.AreEqual(1, Count(EventCodes.TrocarAngle));

            for (int i = 0; i < 90; i++) entry.Tick(0.045f, 5f, 1f / 72f); // straightened up, held in place
            Assert.AreEqual(TrocarPhase.Placed, entry.Phase);
            Assert.IsFalse(entry.AngleWarning);
        }

        [Test]
        public void Shoving_the_trocar_is_excess_force()
        {
            var entry = new TrocarEntry("Camera", new TrocarLimits(), Log);
            Insert(entry, 0.045f, 5f, 0.4f, 1.2f);
            Assert.AreEqual(1, Count(EventCodes.TrocarForce));
            Assert.AreEqual(TrocarPhase.Placed, entry.Phase); // placed, but the deviation stands
        }

        [Test]
        public void Too_deep_is_an_unsafe_entry()
        {
            var entry = new TrocarEntry("Camera", new TrocarLimits(), Log);
            Insert(entry, 0.08f, 5f, 0.05f, 0f);
            Assert.AreEqual(TrocarPhase.Unsafe, entry.Phase);
            Assert.AreEqual(1, Count(EventCodes.TrocarUnsafeEntry));
            Assert.AreEqual(0, Count(EventCodes.TrocarPlaced));
        }

        // ───────────── drift ─────────────

        [Test]
        public void Drift_needs_the_threshold_and_logs_the_correction()
        {
            var drift = new DriftMonitor(1f, Log);
            for (int i = 0; i < 8; i++) drift.Tick("Grasper", false, 0.1f);
            Assert.IsNull(drift.Drifting);

            drift.Tick("Grasper", true, 0.1f); // back in view in time: no drift
            for (int i = 0; i < 12; i++) drift.Tick("Grasper", false, 0.1f);
            Assert.AreEqual("Grasper", drift.Drifting);
            Assert.AreEqual(1, drift.DriftCount);

            drift.Tick("Grasper", true, 0.1f);
            Assert.IsNull(drift.Drifting);
            Assert.AreEqual(1, Count(EventCodes.Drift));
            Assert.AreEqual(1, Count(EventCodes.DriftCorrected));
        }

        [Test]
        public void Instruments_drift_independently()
        {
            var drift = new DriftMonitor(1f, Log);
            for (int i = 0; i < 12; i++)
            {
                drift.Tick("Grasper", false, 0.1f);
                drift.Tick("Scissors", false, 0.1f);
            }
            Assert.AreEqual(2, drift.DriftCount);
            drift.Tick("Scissors", true, 0.1f);
            Assert.AreEqual("Grasper", drift.Drifting);
        }

        // ───────────── swab count ─────────────

        [Test]
        public void Count_completes_when_every_swab_is_in()
        {
            var count = new SwabCount(5, Log);
            for (int i = 0; i < 4; i++) count.Count(false, 0f);
            Assert.IsFalse(count.Complete);
            Assert.AreEqual(1, count.Missing);

            count.Count(true, 42f);
            Assert.IsTrue(count.Complete);
            Assert.AreEqual(1, Count(EventCodes.SwabFound));
            Assert.AreEqual(1, Count(EventCodes.CountComplete));

            count.CloseWithMismatch(); // nothing to report once the count is right
            Assert.AreEqual(0, Count(EventCodes.CountMismatchUnresolved));
        }

        [Test]
        public void Closing_with_a_swab_missing_is_a_single_deviation()
        {
            var count = new SwabCount(5, Log);
            for (int i = 0; i < 4; i++) count.Count(false, 0f);
            count.CloseWithMismatch();
            count.CloseWithMismatch();
            Assert.IsTrue(count.ClosedWithMismatch);
            Assert.AreEqual(1, Count(EventCodes.CountMismatchUnresolved));
            Assert.AreEqual(EventClass.Deviation, events.Last().cls);
        }
    }
}
