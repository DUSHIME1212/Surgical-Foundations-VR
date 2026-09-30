using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SurgicalFoundations.Backend;
using SurgicalFoundations.Contracts;
using SurgicalFoundations.Interaction;
using UnityEngine;
using UnityEngine.Networking;

namespace SurgicalFoundations.Replay
{
    public enum ReplayCameraMode { Learner, Laparoscope, Free }

    /// <summary>
    /// What the dashboard hands the viewer. In the WebGL build it arrives as JSON through
    /// <c>unityInstance.SendMessage("ReplayPlayer", "LoadSession", json)</c>, so the pre-signed replay URL and the event
    /// list never appear in a page URL. Desktop builds take <c>-replay &lt;url|path&gt; [-events &lt;path&gt;] [-title "…"]</c>.
    /// </summary>
    [Serializable]
    public class ReplayViewerRequest
    {
        /// <summary>Pre-signed download URL (GET /replays/{id} → downloadUrl) or, on desktop, a local file path.</summary>
        public string replayUrl;
        /// <summary>Shown above the timeline, e.g. "Ada Lovelace · 30 Sep 2026 · Assessment".</summary>
        public string title;
        /// <summary>Protocol events from GET /sessions/{id}/events, for timeline markers and captions.</summary>
        public List<ProtocolEvent> events = new List<ProtocolEvent>();
    }

    /// <summary>
    /// Plays a session replay (FR-22): moves the learner ghost's head and hands, re-creates each recorded instrument,
    /// and follows the learner's eyes, the laparoscope, or a free orbit camera. Seekable to any moment; poses are
    /// interpolated between the ≥ 30 Hz samples.
    /// </summary>
    public class ReplayPlayer : MonoBehaviour
    {
        public static ReplayPlayer Instance { get; private set; }

        [Header("Ghost")]
        [SerializeField] Transform head;
        [SerializeField] Transform leftHand;
        [SerializeField] Transform rightHand;
        [Tooltip("From the recorded controller/pinch pose back to the wrist of the hand mesh (hand space).")]
        [SerializeField] Vector3 handWristOffset = new Vector3(0f, 0f, -0.08f);
        [Header("Instruments")]
        [SerializeField] GameObject[] instrumentPrefabs;
        [SerializeField] Transform instrumentRoot;
        [Header("Camera")]
        [SerializeField] Camera viewCamera;
        [SerializeField] ReplayOrbitCamera orbit;
        [SerializeField] float learnerFov = 80f;
        [SerializeField] float laparoscopeFov = 70f;
        [SerializeField] float freeFov = 55f;
        [Header("Editor testing")]
        [SerializeField, Tooltip("Editor only: a local .sfr file to load on Play.")] string editorReplayPath;

        readonly Dictionary<ushort, Binding> bindings = new Dictionary<ushort, Binding>();
        readonly List<ProtocolEvent> events = new List<ProtocolEvent>();
        Renderer[] headRenderers;
        ushort? laparoscopeTrack;

        class Binding
        {
            public ReplayFile.Track track;
            public Transform target;
            public InstrumentJaws jaws;
            public bool hideWhenAbsent;
        }

        public event Action Loaded;
        public event Action<string> LoadFailed;

        public ReplayFile File { get; private set; }
        public string Title { get; private set; } = "Session replay";
        public IReadOnlyList<ProtocolEvent> Events => events;
        public float Time { get; private set; }
        public float Duration => File?.DurationSeconds ?? 0f;
        public bool Playing { get; private set; }
        public float Speed { get; set; } = 1f;
        public ReplayCameraMode CameraMode { get; private set; } = ReplayCameraMode.Free;
        public bool HasLaparoscope => laparoscopeTrack != null;

        void Awake()
        {
            Instance = this;
            if (head != null) headRenderers = head.GetComponentsInChildren<Renderer>(true);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            var args = Environment.GetCommandLineArgs();
            var replay = Arg(args, "-replay");
            if (replay != null)
            {
                var request = new ReplayViewerRequest { replayUrl = replay, title = Arg(args, "-title") };
                var eventsPath = Arg(args, "-events");
                if (eventsPath != null && System.IO.File.Exists(eventsPath))
                    request.events = ApiJson.Deserialize<List<ProtocolEvent>>(System.IO.File.ReadAllText(eventsPath));
                StartCoroutine(Load(request));
            }
#if UNITY_EDITOR
            else if (!string.IsNullOrEmpty(editorReplayPath))
                StartCoroutine(Load(new ReplayViewerRequest { replayUrl = editorReplayPath, title = Path.GetFileName(editorReplayPath) }));
#endif
            SetCamera(ReplayCameraMode.Free);
        }

        /// <summary>WebGL entry point (JavaScript: unityInstance.SendMessage("ReplayPlayer", "LoadSession", json)).</summary>
        public void LoadSession(string json)
        {
            ReplayViewerRequest request;
            try { request = ApiJson.Deserialize<ReplayViewerRequest>(json); }
            catch (Exception e) { Fail("Invalid replay request: " + e.Message); return; }
            StopAllCoroutines();
            StartCoroutine(Load(request));
        }

        IEnumerator Load(ReplayViewerRequest request)
        {
            Playing = false;
            byte[] bytes = null;
            if (request?.replayUrl == null) { Fail("No replay given."); yield break; }

            if (request.replayUrl.StartsWith("http://") || request.replayUrl.StartsWith("https://"))
            {
                using var www = UnityWebRequest.Get(request.replayUrl);
                yield return www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Fail(www.responseCode == 403 ? "The replay link has expired. Reopen it from the dashboard." : "Couldn't download the replay: " + www.error);
                    yield break;
                }
                bytes = www.downloadHandler.data;
            }
            else
            {
                try { bytes = System.IO.File.ReadAllBytes(request.replayUrl); }
                catch (Exception e) { Fail("Couldn't open the replay file: " + e.Message); yield break; }
            }

            try { Bind(ReplayFile.Load(bytes)); }
            catch (Exception e) { Fail("This file isn't a readable replay: " + e.Message); yield break; }

            Title = string.IsNullOrEmpty(request.title) ? "Session replay" : request.title;
            events.Clear();
            if (request.events != null) events.AddRange(request.events.Where(e => e != null).OrderBy(e => e.sessionTime));
            Seek(0f);
            Loaded?.Invoke();
        }

        void Bind(ReplayFile file)
        {
            foreach (var b in bindings.Values)
                if (b.track.kind == TrackKind.Instrument && b.target != null) Destroy(b.target.gameObject);
            bindings.Clear();
            laparoscopeTrack = null;
            File = file;

            foreach (var track in file.Tracks.Values)
            {
                switch (track.kind)
                {
                    case TrackKind.Head:
                        if (head != null) bindings[track.id] = new Binding { track = track, target = head };
                        break;
                    case TrackKind.LeftHand:
                        if (leftHand != null) bindings[track.id] = new Binding { track = track, target = leftHand };
                        break;
                    case TrackKind.RightHand:
                        if (rightHand != null) bindings[track.id] = new Binding { track = track, target = rightHand };
                        break;
                    case TrackKind.LaparoscopeCamera:
                        laparoscopeTrack ??= track.id;
                        break;
                    case TrackKind.Instrument:
                        var instance = SpawnInstrument(track.name);
                        if (instance != null)
                            bindings[track.id] = new Binding
                            {
                                track = track, target = instance, hideWhenAbsent = true,
                                jaws = instance.GetComponentInChildren<InstrumentJaws>(true)
                            };
                        break;
                }
            }
        }

        /// <summary>A display-only copy of the instrument: no grabbing, physics or sounds.</summary>
        Transform SpawnInstrument(string name)
        {
            var prefab = instrumentPrefabs?.FirstOrDefault(p => p != null && p.name == name);
            if (prefab == null) return null;
            var go = Instantiate(prefab, instrumentRoot);
            go.name = name;
            foreach (var body in go.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.detectCollisions = false; }
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true))
                if (!(behaviour is InstrumentJaws)) behaviour.enabled = false;
            go.SetActive(false);
            return go.transform;
        }

        void Update()
        {
            if (File == null) return;
            if (Playing)
            {
                Time += UnityEngine.Time.unscaledDeltaTime * Speed;
                if (Time >= Duration) { Time = Duration; Playing = false; }
                Apply();
            }
            UpdateCamera();
        }

        public void TogglePlay()
        {
            if (File == null) return;
            if (!Playing && Time >= Duration - 0.01f) Time = 0f;
            Playing = !Playing;
        }

        public void Seek(float seconds)
        {
            if (File == null) return;
            Time = Mathf.Clamp(seconds, 0f, Duration);
            Apply();
            UpdateCamera();
        }

        public void Skip(float seconds) => Seek(Time + seconds);

        public void SetCamera(ReplayCameraMode mode)
        {
            if (mode == ReplayCameraMode.Laparoscope && !HasLaparoscope) mode = ReplayCameraMode.Learner;
            CameraMode = mode;
            if (orbit != null) orbit.enabled = mode == ReplayCameraMode.Free;
            if (viewCamera != null)
                viewCamera.fieldOfView = mode == ReplayCameraMode.Learner ? learnerFov : mode == ReplayCameraMode.Laparoscope ? laparoscopeFov : freeFov;
            // Seen from the learner's eyes, the ghost head would fill the view.
            if (headRenderers != null) foreach (var r in headRenderers) r.enabled = mode != ReplayCameraMode.Learner;
            UpdateCamera();
        }

        /// <summary>The event just before the current time (within a few seconds), for the caption under the timeline.</summary>
        public ProtocolEvent CurrentEvent(float window = 3f)
        {
            ProtocolEvent current = null;
            foreach (var e in events)
            {
                if (e.sessionTime > Time) break;
                if (Time - e.sessionTime <= window) current = e;
            }
            return current;
        }

        void Apply()
        {
            foreach (var b in bindings.Values)
            {
                if (b.target == null) continue;
                var present = File.TrySample(b.track.id, Time, out var pose);
                if (b.hideWhenAbsent && b.target.gameObject.activeSelf != present) b.target.gameObject.SetActive(present);
                if (!present) continue;

                var position = pose.position;
                if (b.track.kind == TrackKind.LeftHand || b.track.kind == TrackKind.RightHand)
                    position += pose.rotation * handWristOffset;
                b.target.SetPositionAndRotation(position, pose.rotation);
                if (b.jaws != null) b.jaws.SetOpenness(pose.scalar);
            }
        }

        void UpdateCamera()
        {
            if (viewCamera == null || File == null || CameraMode == ReplayCameraMode.Free) return;
            ushort? track = CameraMode == ReplayCameraMode.Laparoscope ? laparoscopeTrack
                : File.Tracks.Values.FirstOrDefault(t => t.kind == TrackKind.Head)?.id;
            if (track == null || !File.TrySample(track.Value, Time, out var pose)) return;
            viewCamera.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        void Fail(string message)
        {
            Debug.LogWarning("[ReplayPlayer] " + message);
            LoadFailed?.Invoke(message);
        }

        static string Arg(string[] args, string name)
        {
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
