using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SurgicalFoundations.Contracts;
using SurgicalFoundations.Detection;

namespace SurgicalFoundations.Tests
{
    public class TaskLogicTests
    {
        readonly List<(EventClass cls, string code, string message)> events = new List<(EventClass, string, string)>();

        [SetUp]
        public void Clear() => events.Clear();

        void Log(EventClass cls, string code, string message) => events.Add((cls, code, message));
        int Count(string code) => events.Count(e => e.code == code);

        // ───────────── peg transfer ─────────────

        [Test]
        public void Peg_transfer_completes_when_every_ring_is_across()
        {
            var pegs = new PegTransfer(3, Log);
            pegs.Placed();
            pegs.Dropped();
            pegs.Placed();
            Assert.IsFalse(pegs.Complete);
            pegs.Placed();
            Assert.IsTrue(pegs.Complete);
            Assert.AreEqual(1, pegs.Drops);
            Assert.AreEqual(3, Count(EventCodes.PegTransferred));
            Assert.AreEqual(EventClass.Delayed, events.Single(e => e.code == EventCodes.PegDropped).cls);

            pegs.Placed(); // nothing left to place
            Assert.AreEqual(3, pegs.Transferred);
        }

        // ───────────── dissection ─────────────

        [Test]
        public void Each_dissection_point_opens_once()
        {
            var dissect = new Dissection(3, 0.25f, Log);
            Assert.IsTrue(dissect.Open(1));
            Assert.IsFalse(dissect.Open(1));
            Assert.AreEqual(1, dissect.Opened);
            dissect.Open(0);
            dissect.Open(2);
            Assert.IsTrue(dissect.Complete);
            Assert.AreEqual(3, Count(EventCodes.DissectionOpened));
        }

        [Test]
        public void A_fast_movement_in_tissue_is_one_excess_force_event()
        {
            var dissect = new Dissection(3, 0.25f, Log);
            for (int i = 0; i < 30; i++) dissect.Contact(true, 0.6f, 0.02f); // 0.6 s of one rough movement
            Assert.AreEqual(1, dissect.ForceEvents);

            for (int i = 0; i < 100; i++) dissect.Contact(true, 0.1f, 0.02f); // gentle: nothing, and the cooldown runs out
            Assert.IsTrue(dissect.Contact(true, 0.6f, 0.02f));
            Assert.AreEqual(2, Count(EventCodes.TissueForce));
        }

        [Test]
        public void Moving_fast_outside_the_tissue_is_not_force()
        {
            var dissect = new Dissection(3, 0.25f, Log);
            Assert.IsFalse(dissect.Contact(false, 2f, 0.02f));
            Assert.AreEqual(0, events.Count);
        }

        // ───────────── clip and cut ─────────────

        [Test]
        public void Clip_both_sites_then_cut_is_on_protocol()
        {
            var clip = new ClipAndCut(2, Log);
            Assert.IsTrue(clip.PlaceClip(0));
            Assert.IsFalse(clip.PlaceClip(0));
            clip.PlaceClip(1);
            Assert.IsTrue(clip.Cut(true));
            Assert.IsTrue(clip.Complete);
            Assert.IsFalse(clip.CutWithoutClips);
            Assert.AreEqual(1, Count(EventCodes.CutDone));
            Assert.IsFalse(events.Any(e => e.cls == EventClass.Deviation));
        }

        [Test]
        public void Cutting_before_clipping_ends_the_task_with_a_deviation()
        {
            var clip = new ClipAndCut(2, Log);
            clip.PlaceClip(0);
            Assert.IsTrue(clip.Cut(true));
            Assert.IsTrue(clip.Complete);
            Assert.IsTrue(clip.CutWithoutClips);
            Assert.AreEqual(EventClass.Deviation, events.Single(e => e.code == EventCodes.CutUnclipped).cls);

            Assert.IsFalse(clip.PlaceClip(1)); // too late
        }

        [Test]
        public void Cut_in_the_wrong_place_and_stray_clip_are_logged_without_ending_the_task()
        {
            var clip = new ClipAndCut(2, Log);
            clip.ClipMisplaced();
            Assert.IsFalse(clip.Cut(false));
            Assert.IsFalse(clip.Complete);
            Assert.AreEqual(1, clip.Misplaced);
            Assert.AreEqual(1, Count(EventCodes.ClipMisplaced));
            Assert.AreEqual(1, Count(EventCodes.CutWrongPlace));
        }

        // ───────────── port removal ─────────────

        static readonly string[] Ports = { "Right working", "Left working", "Camera" };

        [Test]
        public void Ports_out_under_vision_camera_last_is_clean()
        {
            var removal = new PortRemoval(Ports, 2, Log);
            removal.Remove(0, true);
            removal.Remove(1, true);
            removal.Remove(2, false); // the camera port can't watch itself
            Assert.IsTrue(removal.Complete);
            Assert.AreEqual(3, Count(EventCodes.PortRemoved));
            Assert.IsFalse(events.Any(e => e.cls == EventClass.Deviation));
        }

        [Test]
        public void Blind_removal_and_camera_first_are_deviations()
        {
            var removal = new PortRemoval(Ports, 2, Log);
            removal.Remove(2, true);
            removal.Remove(0, false);
            Assert.AreEqual(1, Count(EventCodes.PortRemovedOutOfOrder));
            Assert.AreEqual(1, Count(EventCodes.PortRemovedBlind));
            Assert.IsFalse(removal.Remove(0, true)); // already out

            removal.SkipRemaining();
            Assert.AreEqual(1, Count(EventCodes.PortLeftIn));
            StringAssert.Contains("Left working", events.Last().message);
        }

        // ───────────── port-site closure ─────────────

        [Test]
        public void A_stitch_needs_a_bite_on_each_side()
        {
            var closure = new PortClosure(Ports, Log);
            Assert.IsFalse(closure.Bite(0, 0));
            Assert.IsFalse(closure.Bite(0, 0)); // same side again
            Assert.IsFalse(closure.IsClosed(0));
            Assert.IsTrue(closure.Bite(0, 1));
            Assert.IsTrue(closure.IsClosed(0));
            Assert.AreEqual(1, Count(EventCodes.SiteClosed));

            closure.SkipRemaining();
            Assert.AreEqual(2, Count(EventCodes.SiteNotClosed));
            Assert.IsFalse(closure.Complete);
        }
    }
}
