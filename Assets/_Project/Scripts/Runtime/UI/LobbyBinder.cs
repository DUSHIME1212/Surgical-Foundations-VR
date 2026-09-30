using System;
using System.Linq;
using SurgicalFoundations.Backend;
using SurgicalFoundations.Contracts;
using SurgicalFoundations.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurgicalFoundations.UI
{
    /// <summary>Screen 02: the learner's name, their next assignment, recent attempts and whether results are synced.</summary>
    public class LobbyBinder : MonoBehaviour
    {
        [SerializeField] UITheme theme;
        [SerializeField] TMP_Text welcome;
        [SerializeField] TMP_Text assignmentLine;
        [SerializeField] TMP_Text[] attemptKeys;
        [SerializeField] TMP_Text[] attemptValues;
        [SerializeField] TMP_Text attemptsFooter;
        [SerializeField] TMP_Text syncStatus;
        [SerializeField] Image syncDot;
        [SerializeField] TMP_Text accountButtonLabel;

        static BackendServices Services => BackendServices.Instance;

        void OnEnable()
        {
            if (Services != null)
            {
                Services.Auth.Changed += Refresh;
                Services.Queue.Changed += Refresh;
                Services.Learner.Changed += Refresh;
                Services.RefreshSoon();
            }
            if (SessionManager.Instance != null) SessionManager.Instance.SettingsChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (Services != null)
            {
                Services.Auth.Changed -= Refresh;
                Services.Queue.Changed -= Refresh;
                Services.Learner.Changed -= Refresh;
            }
            if (SessionManager.Instance != null) SessionManager.Instance.SettingsChanged -= Refresh;
        }

        void Refresh()
        {
            if (!isActiveAndEnabled) return;
            var s = Services;
            var guest = s == null || s.Auth.IsGuest || !s.Auth.HasAccount;

            Set(welcome, guest ? "Training as a guest · results aren't saved" : $"Welcome back, {s.Auth.DisplayName}");
            Set(accountButtonLabel, guest ? "Sign in" : "Sign out");
            Set(assignmentLine, AssignmentText(s, guest));

            var attempts = guest ? Array.Empty<SessionSummary>() : s.Learner.Attempts.sessions.ToArray();
            for (int i = 0; i < (attemptKeys?.Length ?? 0); i++)
            {
                var a = i < attempts.Length ? attempts[i] : null;
                Set(attemptKeys[i], a == null ? (i == 0 ? "No attempts yet" : "") : $"{Local(a.startedAtUnixMs):d MMM HH:mm} · {a.mode}");
                if (attemptValues != null && i < attemptValues.Length)
                    Set(attemptValues[i], a == null ? "" : a.status == SessionStatus.Completed ? $"{a.overallScore:0}" : "unfinished");
            }
            Set(attemptsFooter, guest ? "Sign in to keep a history of your attempts" : "Your most recent attempts, newest first");
            SetSync(s, guest);
        }

        string AssignmentText(BackendServices s, bool guest)
        {
            const string stages = "Prep → Access → Operate → Close";
            if (guest) return "Free practice · " + stages;
            var mode = SessionManager.Instance != null ? SessionManager.Instance.Settings.mode : TrainingMode.Guided;
            var next = s.Learner.NextAssignment(mode, s.Settings.scenarioId);
            if (next != null)
            {
                var due = next.dueAtUnixMs > 0 ? $" · due {Local(next.dueAtUnixMs):d MMM}" : "";
                return $"Assignment: {next.title} · {next.cohortName}{due} · this run counts toward it";
            }
            var other = s.Learner.NextAssignmentAnyMode(s.Settings.scenarioId);
            if (other != null) return $"Assignment \"{other.title}\" needs {other.mode} mode · this run is free practice";
            return "Free practice · " + stages;
        }

        void SetSync(BackendServices s, bool guest)
        {
            string text; bool ok;
            if (s == null || guest) { text = "Guest session · nothing is uploaded"; ok = false; }
            else
            {
                var pending = s.Queue.PendingFor(s.Auth.UserId);
                var reachable = !s.Queue.Offline;
                if (pending == 0) { text = "All results synced"; ok = true; }
                else if (!reachable) { text = $"Offline · {pending} upload{(pending == 1 ? "" : "s")} waiting · will sync when you reconnect"; ok = false; }
                else { text = s.Queue.Uploading ? $"Syncing {pending} upload{(pending == 1 ? "" : "s")}…" : $"{pending} upload{(pending == 1 ? "" : "s")} waiting to sync"; ok = true; }
            }
            Set(syncStatus, text);
            if (syncDot != null && theme != null) syncDot.color = ok ? theme.accent : theme.warning;
        }

        /// <summary>Sign out (or, for a guest, go sign in) and return to the sign-in screen.</summary>
        public void SignOutOrIn()
        {
            if (Services != null)
            {
                if (Services.Auth.IsGuest) Services.Auth.LeaveGuestMode();
                else Services.Auth.SignOut();
            }
            var sequence = GetComponentInParent<StepSequence>();
            if (sequence != null) sequence.GoTo(0);
        }

        static DateTime Local(long unixMs) => DateTimeOffset.FromUnixTimeMilliseconds(unixMs).LocalDateTime;

        static void Set(TMP_Text t, string value) { if (t != null) t.text = value; }
    }
}
