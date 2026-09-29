using System;
using System.Collections.Generic;
using SurgicalFoundations.Audio;
using SurgicalFoundations.Core;
using UnityEngine;

namespace SurgicalFoundations.Scenario
{
    /// <summary>
    /// Session-wide protocol event log (lives in 00_Bootstrap). Pose recording at >= 30 Hz and the sync queue
    /// plug in here later; for now it records events, plays the matching feedback sound and notifies the UI.
    /// </summary>
    public class EventLogger : MonoBehaviour
    {
        public static EventLogger Instance { get; private set; }

        readonly List<ProtocolEvent> events = new List<ProtocolEvent>();
        public IReadOnlyList<ProtocolEvent> Events => events;
        public ScenarioStage CurrentStage { get; set; } = ScenarioStage.Prep;

        public event Action<ProtocolEvent> EventLogged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public ProtocolEvent Log(EventClass cls, string code, string message)
        {
            var t = SessionManager.Instance != null ? SessionManager.Instance.ElapsedSeconds : Time.time;
            var e = ProtocolEvent.Create(t, CurrentStage, cls, code, message);
            events.Add(e);

            var guided = SessionManager.Instance == null || SessionManager.Instance.Settings.mode == TrainingMode.Guided;
            if (guided && AudioManager.Instance != null)
            {
                switch (cls)
                {
                    case EventClass.OnProtocol: AudioManager.Instance.Play(SoundId.FB_OnProtocol); break;
                    case EventClass.Delayed: AudioManager.Instance.Play(SoundId.FB_Delayed); break;
                    case EventClass.Deviation: AudioManager.Instance.Play(SoundId.FB_Deviation); break;
                }
            }

            EventLogged?.Invoke(e);
            return e;
        }

        public void Clear() => events.Clear();
    }
}
