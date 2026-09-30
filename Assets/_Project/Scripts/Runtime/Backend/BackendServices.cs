using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SurgicalFoundations.Core;
using SurgicalFoundations.Replay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurgicalFoundations.Backend
{
    /// <summary>
    /// Hosts the backend services in 00_Bootstrap, which never unloads, so sign-in, the upload queue and the session
    /// recorder survive every scene and stage change (NFR-09). If an older Bootstrap scene lacks this object it is
    /// added automatically.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class BackendServices : MonoBehaviour
    {
        public static BackendServices Instance { get; private set; }

        [SerializeField, Tooltip("Leave empty to load Resources/BackendSettings (or defaults).")] BackendSettings settings;

        public BackendSettings Settings => settings;
        public ApiClient Api { get; private set; }
        public AuthService Auth { get; private set; }
        public SyncQueue Queue { get; private set; }
        public ScoringConfigStore Scoring { get; private set; }
        public LearnerData Learner { get; private set; }
        public SessionRecorder Recorder { get; private set; }
        public PoseRecorder Poses { get; private set; }

        CancellationTokenSource refreshNow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneIds.Bootstrap || Instance != null || FindAnyObjectByType<BackendServices>() != null) return;
            var go = new GameObject(nameof(BackendServices));
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<BackendServices>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (settings == null) settings = BackendSettings.Load();
            var root = Application.persistentDataPath;
            Api = new ApiClient(settings);
            Auth = new AuthService(Api);
            Queue = new SyncQueue(Api, Auth, Path.Combine(root, "sync"));
            Scoring = new ScoringConfigStore(Path.Combine(root, "scoring-config.json"));
            Learner = new LearnerData(Path.Combine(root, "learners"));
            Poses = new PoseRecorder(Path.Combine(root, "replays"), settings.replaySampleRateHz);
            Recorder = new SessionRecorder(Auth, Queue, Scoring, Learner, settings, Poses);
            Learner.UseCacheFor(Auth.UserId);
            Auth.Changed += OnAuthChanged;
        }

        void Start()
        {
            Recorder.Attach();
            Run(Queue.RunAsync(destroyCancellationToken));
            Run(RefreshLoopAsync(destroyCancellationToken));
        }

        void Update() => Recorder.Tick();

        // After tracking, animation and physics have moved everything this frame.
        void LateUpdate() => Poses.Tick();

        // Quest suspends apps when the headset comes off; queue whatever is buffered first.
        void OnApplicationPause(bool paused) { if (paused) Recorder?.Flush(); }
        void OnApplicationQuit() => Recorder?.Flush();

        void OnDestroy()
        {
            if (Instance != this) return;
            Recorder?.Flush();
            Recorder?.Detach();
            Poses?.Abort();
            if (Auth != null) Auth.Changed -= OnAuthChanged;
            Instance = null;
        }

        void OnAuthChanged()
        {
            Learner.UseCacheFor(Auth.IsGuest ? null : Auth.UserId);
            if (Auth.State == AuthState.SignedIn) RefreshSoon();
        }

        /// <summary>Wakes the refresh loop now (e.g. after sign-in or when the lobby opens).</summary>
        public void RefreshSoon() => refreshNow?.Cancel();

        async Task RefreshLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (SyncQueue.NetworkReachable && settings.online)
                {
                    await Scoring.RefreshAsync(Api, ct);
                    if (Auth.State == AuthState.SignedIn && !Auth.IsGuest) await Learner.RefreshAsync(Api, Auth, ct);
                }
                refreshNow = CancellationTokenSource.CreateLinkedTokenSource(ct);
                try { await Task.Delay(TimeSpan.FromSeconds(Mathf.Max(10f, settings.refreshSeconds)), refreshNow.Token); }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }
                finally { refreshNow.Dispose(); refreshNow = null; }
            }
        }

        static async void Run(Task task)
        {
            try { await task; }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
