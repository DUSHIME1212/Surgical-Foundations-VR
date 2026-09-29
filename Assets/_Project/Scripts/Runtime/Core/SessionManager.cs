using System;
using UnityEngine;

namespace SurgicalFoundations.Core
{
    public enum TrainingMode { Guided, Assessment }
    public enum Posture { Standing, Seated }
    public enum InputMode { Controllers, Hands }

    [Serializable]
    public class SessionSettings
    {
        public TrainingMode mode = TrainingMode.Guided;
        public Posture posture = Posture.Standing;
        public InputMode input = InputMode.Controllers;
        public bool subtitles = true;
        [Range(0.85f, 1.3f)] public float textScale = 1f;
        public bool dominantHandRight = true;
        public float tableHeightCm = 92f;
    }

    /// <summary>
    /// Lives in 00_Bootstrap. Owns the learner's settings, the session clock and the pause state (US-ACS-03).
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        [SerializeField] SessionSettings settings = new SessionSettings();

        public SessionSettings Settings => settings;
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public int Seed { get; private set; }

        public event Action<bool> PauseChanged;
        public event Action SettingsChanged;
        public event Action SessionStarted;
        public event Action SessionEnded;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (IsRunning && !IsPaused)
                ElapsedSeconds += Time.unscaledDeltaTime;
        }

        public void BeginSession(int? seed = null)
        {
            Seed = seed ?? Environment.TickCount;
            ElapsedSeconds = 0f;
            IsRunning = true;
            SetPaused(false);
            SessionStarted?.Invoke();
        }

        public void EndSession()
        {
            IsRunning = false;
            SessionEnded?.Invoke();
        }

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            PauseChanged?.Invoke(paused);
        }

        public void TogglePause() => SetPaused(!IsPaused);

        public void SetMode(TrainingMode mode) { settings.mode = mode; SettingsChanged?.Invoke(); }
        public void SetPosture(Posture posture) { settings.posture = posture; SettingsChanged?.Invoke(); }
        public void SetInput(InputMode input) { settings.input = input; SettingsChanged?.Invoke(); }
        public void SetSubtitles(bool on) { settings.subtitles = on; SettingsChanged?.Invoke(); }
        public void SetTextScale(float scale) { settings.textScale = scale; SettingsChanged?.Invoke(); }

        public static string FormatClock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{s / 60:00}:{s % 60:00}";
        }
    }
}
