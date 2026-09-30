using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SurgicalFoundations.Contracts;
using SurgicalFoundations.Replay;
using UnityEngine;

namespace SurgicalFoundations.Tests
{
    public class ReplayFormatTests
    {
        string dir;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "sf-replay-tests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        static Vector3 HeadAt(float t) => new Vector3(Mathf.Sin(t), 1.6f + 0.05f * t, Mathf.Cos(t) * 0.5f);
        static Quaternion HeadRotAt(float t) => Quaternion.Euler(10f * t, 45f * t, -5f * t);

        ReplayFileInfo Record(float seconds, float rate, bool gapForInstrument = false)
        {
            var writer = new ReplayWriter(Path.Combine(dir, "s.part"), "session-1", rate, 1_790_000_000_000);
            writer.WriteTrack(0, TrackKind.Head, "Head");
            writer.WriteTrack(1, TrackKind.Instrument, "INST_AtraumaticGrasper");
            writer.WriteStage(0f, ScenarioStage.Prep);
            var samples = new List<TrackSample>();
            for (int i = 0; i <= Mathf.RoundToInt(seconds * rate); i++)
            {
                var t = i / rate;
                if (Mathf.Approximately(t, 1f)) writer.WriteStage(t, ScenarioStage.Access);
                samples.Clear();
                samples.Add(new TrackSample { track = 0, position = HeadAt(t), rotation = HeadRotAt(t) });
                var hidden = gapForInstrument && t > 0.5f && t < 1.5f;
                if (!hidden)
                    samples.Add(new TrackSample
                    {
                        track = 1, flags = PoseFlags.HasScalar, position = new Vector3(0.1f * t, 1f, 0.4f),
                        rotation = Quaternion.Euler(0, 90f * t, 0), scalar = (byte)(t / seconds * 255f)
                    });
                writer.WriteFrame(t, samples);
            }
            return writer.Finish(Path.Combine(dir, "s" + ReplayFormat.FileExtension))();
        }

        [Test]
        public void Round_trip_keeps_header_tracks_stages_and_frames()
        {
            var info = Record(2f, 30f);
            Assert.AreEqual(61, info.frameCount);
            Assert.AreEqual(64, info.sha256.Length);
            Assert.IsFalse(File.Exists(Path.Combine(dir, "s.part")), "the uncompressed part file is removed");

            var file = ReplayFile.Load(File.ReadAllBytes(info.path));
            Assert.IsTrue(file.Complete);
            Assert.AreEqual("session-1", file.SessionId);
            Assert.AreEqual(30f, file.SampleRateHz);
            Assert.AreEqual(1_790_000_000_000, file.StartedAtUnixMs);
            Assert.AreEqual(2f, file.DurationSeconds, 1e-4f);
            Assert.AreEqual(61, file.FrameCount);
            Assert.AreEqual(TrackKind.Instrument, file.Tracks[1].kind);
            Assert.AreEqual("INST_AtraumaticGrasper", file.Tracks[1].name);
            CollectionAssert.AreEqual(new[] { ScenarioStage.Prep, ScenarioStage.Access }, file.Stages.ConvertAll(s => s.stage));
        }

        [Test]
        public void Sampling_interpolates_between_frames_within_tolerance()
        {
            var file = ReplayFile.Load(File.ReadAllBytes(Record(2f, 30f).path));
            foreach (var t in new[] { 0f, 0.37f, 1.01f, 1.99f })
            {
                Assert.IsTrue(file.TrySample(0, t, out var pose), $"head at {t}");
                Assert.Less(Vector3.Distance(HeadAt(t), pose.position), 0.002f, $"position at {t}");
                Assert.Less(Quaternion.Angle(HeadRotAt(t), pose.rotation), 0.6f, $"rotation at {t}");
            }
            Assert.IsTrue(file.TrySample(1, 1f, out var instrument));
            Assert.AreEqual(0.5f, instrument.scalar, 0.01f, "jaw openness");
        }

        [Test]
        public void Rotation_packing_is_accurate_even_near_180_degrees()
        {
            foreach (var q in new[] { Quaternion.identity, Quaternion.Euler(0, 179.9f, 0), Quaternion.Euler(170, -60, 33), new Quaternion(0.5f, -0.5f, 0.5f, -0.5f) })
            {
                using var ms = new MemoryStream();
                using (var w = new BinaryWriter(ms, System.Text.Encoding.UTF8, true)) ReplayFormat.WriteRotation(w, q);
                ms.Position = 0;
                using var r = new BinaryReader(ms);
                Assert.Less(Quaternion.Angle(q, ReplayFormat.ReadRotation(r)), 0.6f, q.eulerAngles.ToString());
            }
        }

        [Test]
        public void An_absent_object_is_not_interpolated_across_the_gap()
        {
            var file = ReplayFile.Load(File.ReadAllBytes(Record(2f, 30f, gapForInstrument: true).path));
            Assert.IsFalse(file.TrySample(1, 1.0f, out _), "instrument was put away between 0.5 s and 1.5 s");
            Assert.IsTrue(file.TrySample(1, 1.8f, out _));
        }

        [Test]
        public void Other_files_are_rejected()
        {
            using var ms = new MemoryStream();
            using (var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress, true))
                gz.Write(new byte[] { (byte)'N', (byte)'O', (byte)'P', (byte)'E', 1, 0 }, 0, 6);
            Assert.Throws<InvalidDataException>(() => ReplayFile.Load(ms.ToArray()));
        }
    }
}
