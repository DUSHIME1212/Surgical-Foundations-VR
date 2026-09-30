using UnityEngine;

namespace SurgicalFoundations.Backend
{
    /// <summary>
    /// Where the API lives and how the headset talks to it. Create one with
    /// Surgical Foundations ▸ Backend ▸ Create Settings (saved to Resources so it ships in every build).
    /// </summary>
    [CreateAssetMenu(menuName = "Surgical Foundations/Backend Settings", fileName = "BackendSettings")]
    public class BackendSettings : ScriptableObject
    {
        public const string ResourceName = "BackendSettings";

        [Tooltip("API base URL. With a USB-connected Quest run `adb reverse tcp:5010 tcp:5010` and keep http://localhost:5010; " +
                 "production builds must use https://.")]
        public string apiBaseUrl = "http://localhost:5010";

        [Tooltip("Off = never contact the server (demo builds). Everything else still works; nothing is saved remotely.")]
        public bool online = true;

        public string scenarioId = "lap-foundations";

        [Tooltip("Seconds before a request counts as offline.")]
        public int requestTimeoutSeconds = 15;

        [Tooltip("Events are uploaded in batches of at most this many (the API accepts up to 500).")]
        public int eventBatchSize = 100;

        [Tooltip("Buffered events are queued for upload at least this often, so a crash loses little.")]
        public float eventFlushSeconds = 10f;

        [Tooltip("Record head, hands and instruments for the session replay (signed-in learners only).")]
        public bool recordReplays = true;

        [Tooltip("Replay samples per second. 30 is the minimum the requirements allow (FR-21).")]
        [Min(30f)] public float replaySampleRateHz = 30f;

        [Tooltip("How often the lobby data and scoring config are refreshed while online.")]
        public float refreshSeconds = 60f;

        public static BackendSettings Load()
        {
            var s = Resources.Load<BackendSettings>(ResourceName);
            return s != null ? s : CreateInstance<BackendSettings>();
        }
    }
}
