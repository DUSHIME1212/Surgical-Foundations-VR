using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SurgicalFoundations.Contracts;
using SurgicalFoundations.Core;
using SurgicalFoundations.Interaction;
using SurgicalFoundations.Lighting;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurgicalFoundations.Replay
{
    /// <summary>
    /// Records the learner's head, hands, instruments and laparoscope at a fixed rate (≥ 30 Hz, FR-21) on the session
    /// clock, so poses line up with protocol events. Recording is world-space: teleports and table moves replay as
    /// they happened. Sampling pauses with the session.
    /// </summary>
    public class PoseRecorder
    {
        class Tracked
        {
            public ushort id;
            public TrackKind kind;
            public Transform primary;   // controller, instrument root, camera
            public Transform secondary; // hand-tracking pose, used when its object is active
            public InstrumentJaws jaws;
        }

        readonly string directory;
        readonly float sampleRateHz;
        readonly List<Tracked> tracked = new List<Tracked>();
        readonly Dictionary<Transform, Tracked> byTransform = new Dictionary<Transform, Tracked>();
        readonly List<TrackSample> samples = new List<TrackSample>(16);
        ReplayWriter writer;
        string sessionId;
        ushort nextTrackId;
        float nextSampleAt;
        bool rescan;

        public PoseRecorder(string directory, float sampleRateHz)
        {
            this.directory = directory;
            this.sampleRateHz = Mathf.Max(30f, sampleRateHz);
            Directory.CreateDirectory(directory);
            // A ".part" left behind means the app died mid-session; that session was never completed, so drop it.
            foreach (var stale in Directory.GetFiles(directory, "*.part"))
                try { File.Delete(stale); } catch (IOException) { }
            SceneManager.sceneLoaded += (_, __) => rescan = true;
            SceneManager.sceneUnloaded += _ => rescan = true;
        }

        public bool IsRecording => writer != null;
        public float SampleRateHz => sampleRateHz;

        public void Begin(string newSessionId, long startedAtUnixMs)
        {
            Abort();
            sessionId = newSessionId;
            tracked.Clear();
            byTransform.Clear();
            nextTrackId = 0;
            nextSampleAt = 0f;
            rescan = true;
            try { writer = new ReplayWriter(PathFor(newSessionId) + ".part", newSessionId, sampleRateHz, startedAtUnixMs); }
            catch (IOException e) { Debug.LogWarning("[PoseRecorder] Can't record this session: " + e.Message); writer = null; }
        }

        public void MarkStage(ScenarioStage stage)
        {
            if (writer == null) return;
            writer.WriteStage(Clock, stage);
            rescan = true;
        }

        /// <summary>Called from LateUpdate, after tracking and animation have moved everything this frame.</summary>
        public void Tick()
        {
            if (writer == null) return;
            var sm = SessionManager.Instance;
            if (sm == null || !sm.IsRunning || sm.IsPaused) return;
            var now = sm.ElapsedSeconds;
            if (now < nextSampleAt) return;
            // Fixed rate; if a frame hitch skipped samples, continue from now rather than bursting to catch up.
            nextSampleAt = Mathf.Max(nextSampleAt + 1f / sampleRateHz, now + 0.5f / sampleRateHz);

            if (rescan) Rescan();
            samples.Clear();
            foreach (var t in tracked) Sample(t);
            try { writer.WriteFrame(now, samples); }
            catch (IOException e) { Debug.LogWarning("[PoseRecorder] Stopped recording: " + e.Message); Abort(); }
        }

        /// <summary>Stops recording and compresses the file off the main thread. Null if nothing was recorded.</summary>
        public Task<ReplayFileInfo> FinishAsync()
        {
            if (writer == null) return Task.FromResult<ReplayFileInfo>(null);
            var w = writer;
            writer = null;
            if (w.FrameCount == 0) { w.Abort(); return Task.FromResult<ReplayFileInfo>(null); }
            var compress = w.Finish(PathFor(sessionId) + ReplayFormat.FileExtension);
            return Task.Run(compress);
        }

        public void Abort()
        {
            writer?.Abort();
            writer = null;
        }

        void Sample(Tracked t)
        {
            var source = t.primary;
            var flags = PoseFlags.None;
            if (t.secondary != null && t.secondary.gameObject.activeInHierarchy)
            {
                source = t.secondary;
                flags |= PoseFlags.HandTracking;
            }
            if (source == null || !source.gameObject.activeInHierarchy) return;

            byte scalar = 0;
            if (t.jaws != null)
            {
                flags |= PoseFlags.HasScalar;
                scalar = (byte)Mathf.RoundToInt(t.jaws.Openness * 255f);
            }
            source.GetPositionAndRotation(out var position, out var rotation);
            samples.Add(new TrackSample { track = t.id, flags = flags, position = position, rotation = rotation, scalar = scalar });
        }

        void Rescan()
        {
            rescan = false;
            // Instruments and the scope come and go with stage scenes; forget the destroyed ones.
            tracked.RemoveAll(t => t.primary == null);
            var gone = new List<Transform>();
            foreach (var key in byTransform.Keys) if (key == null) gone.Add(key);
            foreach (var key in gone) byTransform.Remove(key);

            var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            if (origin != null)
            {
                if (origin.Camera != null) Add(origin.Camera.transform, TrackKind.Head, "Head");
                var offset = origin.CameraFloorOffsetObject != null ? origin.CameraFloorOffsetObject.transform : origin.transform;
                AddHand(offset, TrackKind.LeftHand, "Left");
                AddHand(offset, TrackKind.RightHand, "Right");
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var tip in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (tip.name != "Pivot_Tip") continue;
                        var instrument = InstrumentRoot(tip);
                        if (instrument != null) Add(instrument, TrackKind.Instrument, CleanName(instrument.name), instrument.GetComponentInChildren<InstrumentJaws>(true));
                    }
                    foreach (var scope in root.GetComponentsInChildren<LaparoscopeFeed>(true))
                        Add(scope.transform, TrackKind.LaparoscopeCamera, "Laparoscope");
                }
            }
        }

        void AddHand(Transform offset, TrackKind kind, string side)
        {
            var controller = offset.Find(side + " Controller");
            var handPose = offset.Find(side + " Hand/Pinch Grab Pose");
            if (controller == null && handPose == null) return;
            var key = controller != null ? controller : handPose;
            if (byTransform.ContainsKey(key)) return;
            var t = Add(key, kind, side + " hand");
            if (t != null) t.secondary = controller != null ? handPose : null;
        }

        Tracked Add(Transform target, TrackKind kind, string name, InstrumentJaws jaws = null)
        {
            if (byTransform.TryGetValue(target, out var existing)) return existing;
            if (writer == null || nextTrackId == ushort.MaxValue) return null;
            var t = new Tracked { id = nextTrackId++, kind = kind, primary = target, jaws = jaws };
            byTransform[target] = t;
            tracked.Add(t);
            writer.WriteTrack(t.id, kind, name);
            return t;
        }

        /// <summary>The instrument prefab root ("INST_…") above a tip pivot.</summary>
        static Transform InstrumentRoot(Transform tip)
        {
            for (var t = tip.parent; t != null; t = t.parent)
                if (t.name.StartsWith("INST_", StringComparison.Ordinal)) return t;
            return null;
        }

        static string CleanName(string name) => name.Replace("(Clone)", "").Trim();

        string PathFor(string id) => Path.Combine(directory, id);

        static float Clock => SessionManager.Instance != null ? SessionManager.Instance.ElapsedSeconds : 0f;
    }
}
