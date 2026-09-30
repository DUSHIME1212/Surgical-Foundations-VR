using System;
using System.Collections.Generic;
using SurgicalFoundations.Contracts;
using SurgicalFoundations.Core;
using SurgicalFoundations.Replay;
using SurgicalFoundations.Scenario;
using UnityEngine;

namespace SurgicalFoundations.Backend
{
    /// <summary>
    /// Turns what happens in the headset into API records: a session when the scenario starts (FR-04), batched protocol
    /// events as they happen (FR-25), and the on-device score when the summary opens (FR-17). Everything goes through
    /// the <see cref="SyncQueue"/>, so none of it needs a connection at the time.
    /// </summary>
    public class SessionRecorder
    {
        readonly AuthService auth;
        readonly SyncQueue queue;
        readonly ScoringConfigStore scoring;
        readonly LearnerData learner;
        readonly BackendSettings settings;
        readonly PoseRecorder pose;

        readonly List<ProtocolEvent> sessionEvents = new List<ProtocolEvent>();
        readonly List<ProtocolEvent> unsent = new List<ProtocolEvent>();
        readonly Dictionary<ScenarioStage, float> stageSeconds = new Dictionary<ScenarioStage, float>();
        ScenarioStage? currentStage;
        float stageEnteredAt;
        float lastFlushTime;
        bool completed;
        string ownerUserId;
        ScoringConfig sessionConfig;

        public SessionRecorder(AuthService auth, SyncQueue queue, ScoringConfigStore scoring, LearnerData learner, BackendSettings settings,
            PoseRecorder pose)
        {
            this.pose = pose;
            this.auth = auth;
            this.queue = queue;
            this.scoring = scoring;
            this.learner = learner;
            this.settings = settings;
        }

        public event Action ResultReady;

        public string SessionId { get; private set; }
        /// <summary>False for guests: the session is scored for the summary but never stored.</summary>
        public bool Recording { get; private set; }
        public AssignmentSummary Assignment { get; private set; }
        public ScoringEngine.Result LastResult { get; private set; }
        public bool LastResultRecorded { get; private set; }

        public void Attach()
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.SessionStarted += Begin;
                SessionManager.Instance.SessionEnded += Flush;
            }
            if (EventLogger.Instance != null) EventLogger.Instance.EventLogged += OnEvent;
            ScenarioDirector.StageEntered += OnStage;
        }

        public void Detach()
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.SessionStarted -= Begin;
                SessionManager.Instance.SessionEnded -= Flush;
            }
            if (EventLogger.Instance != null) EventLogger.Instance.EventLogged -= OnEvent;
            ScenarioDirector.StageEntered -= OnStage;
        }

        void Begin()
        {
            Flush();
            var sm = SessionManager.Instance;
            SessionId = Guid.NewGuid().ToString();
            sessionEvents.Clear();
            unsent.Clear();
            stageSeconds.Clear();
            currentStage = null;
            completed = false;
            LastResult = null;
            LastResultRecorded = false;
            lastFlushTime = Time.unscaledTime;

            ownerUserId = auth.UserId;
            Recording = auth.HasAccount && !auth.IsGuest;
            // Pinned for the whole session: a config downloaded mid-run must not change how this run is scored.
            sessionConfig = scoring.Current;
            var mode = sm != null ? sm.Settings.mode : TrainingMode.Guided;
            Assignment = Recording ? learner.NextAssignment(mode, settings.scenarioId) : null;
            var startedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Movement data is personal: record it only for a signed-in learner whose results are kept anyway.
            if (Recording && settings.recordReplays) pose?.Begin(SessionId, startedAt);
            else pose?.Abort();
            if (!Recording) return;

            queue.Enqueue(SyncQueue.Kinds.CreateSession, ownerUserId, SessionId, new CreateSessionRequest
            {
                id = SessionId,
                scenarioId = settings.scenarioId,
                mode = mode,
                posture = sm != null ? sm.Settings.posture : Posture.Standing,
                input = sm != null ? sm.Settings.input : InputMode.Controllers,
                seed = sm != null ? sm.Seed : 0,
                scoringConfigVersion = sessionConfig.version,
                appVersion = Application.version,
                deviceId = SystemInfo.deviceUniqueIdentifier,
                startedAtUnixMs = startedAt,
                assignmentId = Assignment?.id
            });
        }

        void OnEvent(ProtocolEvent e)
        {
            if (SessionId == null) return;
            sessionEvents.Add(e);
            unsent.Add(e);
            if (unsent.Count >= settings.eventBatchSize) Flush();
        }

        void OnStage(ScenarioStage stage)
        {
            pose?.MarkStage(stage);
            var clock = SessionClock;
            if (currentStage is { } previous && previous != ScenarioStage.Summary)
                stageSeconds[previous] = (stageSeconds.TryGetValue(previous, out var s) ? s : 0f) + Mathf.Max(0f, clock - stageEnteredAt);

            if (stage == ScenarioStage.Summary)
            {
                Complete();
            }
            else if (completed)
            {
                // Retrying a stage after the summary is a new, partial attempt with the same seed.
                SessionManager.Instance?.BeginSession(SessionManager.Instance.Seed); // raises SessionStarted → Begin()
                clock = SessionClock;
            }
            currentStage = stage;
            stageEnteredAt = clock;
            Flush();
        }

        /// <summary>Called every frame by <see cref="BackendServices"/>; queues buffered events every few seconds.</summary>
        public void Tick()
        {
            if (unsent.Count > 0 && Time.unscaledTime - lastFlushTime >= settings.eventFlushSeconds) Flush();
        }

        public void Flush()
        {
            lastFlushTime = Time.unscaledTime;
            if (unsent.Count == 0) return;
            if (Recording && SessionId != null)
                queue.Enqueue(SyncQueue.Kinds.Events, ownerUserId, SessionId, new EventBatch { events = new List<ProtocolEvent>(unsent) });
            unsent.Clear();
        }

        void Complete()
        {
            if (completed || SessionId == null) return;
            completed = true;
            Flush();

            LastResult = ScoringEngine.Score(sessionConfig ?? scoring.Current, sessionEvents, stageSeconds, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            LastResultRecorded = Recording;
            if (Recording)
            {
                queue.Enqueue(SyncQueue.Kinds.CompleteSession, ownerUserId, SessionId, LastResult.request);
                FinishReplay(SessionId, ownerUserId);
            }
            ResultReady?.Invoke();
        }

        /// <summary>Compresses the pose recording off the main thread, then queues it behind the result (FR-21).</summary>
        async void FinishReplay(string sessionId, string owner)
        {
            if (pose == null) return;
            try
            {
                var file = await pose.FinishAsync();
                if (file == null) return;
                queue.Enqueue(SyncQueue.Kinds.Replay, owner, sessionId, new CreateReplayRequest
                {
                    id = Guid.NewGuid().ToString(),
                    sessionId = sessionId,
                    contentType = ReplayFormat.ContentType,
                    sizeBytes = file.sizeBytes,
                    sha256 = file.sha256,
                    manifest = new ReplayManifest
                    {
                        formatVersion = ReplayFormat.Version,
                        sampleRateHz = file.sampleRateHz,
                        durationSeconds = file.durationSeconds,
                        sampleCount = file.frameCount
                    }
                }, file.path);
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        static float SessionClock => SessionManager.Instance != null ? SessionManager.Instance.ElapsedSeconds : Time.unscaledTime;
    }
}
