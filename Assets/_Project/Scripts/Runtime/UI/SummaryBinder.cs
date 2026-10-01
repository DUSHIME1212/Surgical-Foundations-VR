using System.Linq;
using SurgicalFoundations.Backend;
using SurgicalFoundations.Contracts;
using SurgicalFoundations.Core;
using SurgicalFoundations.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurgicalFoundations.UI
{
    /// <summary>
    /// Screen 16 (FR-20): fills the summary with the on-device result — overall and stage scores, pass/fail, the top
    /// three issues — and says whether it has reached the server (and the LMS) yet.
    /// </summary>
    public class SummaryBinder : MonoBehaviour
    {
        [SerializeField] UITheme theme;
        [SerializeField] UIMotion motion;
        [SerializeField] TMP_Text eyebrow;
        [SerializeField] TMP_Text passMark;
        [SerializeField] TMP_Text chipLabel;
        [SerializeField] Image chipFill;
        [SerializeField] TMP_Text[] stageValues;
        [SerializeField] GameObject[] issueCards;
        [SerializeField] TMP_Text[] issueTimes;
        [SerializeField] TMP_Text[] issueTitles;
        [SerializeField] TMP_Text[] issueMetas;
        [SerializeField] TMP_Text retryLabel;
        [SerializeField] TMP_Text syncStatus;
        [SerializeField] Image syncDot;

        static readonly ScenarioStage[] Stages = { ScenarioStage.Prep, ScenarioStage.Access, ScenarioStage.Operate, ScenarioStage.Close };
        ScenarioStage weakest = ScenarioStage.Access;

        static BackendServices Services => BackendServices.Instance;

        void OnEnable()
        {
            if (Services != null) Services.Queue.Changed += RefreshSync;
            Apply();
        }

        void OnDisable()
        {
            if (Services != null) Services.Queue.Changed -= RefreshSync;
        }

        void Apply()
        {
            var result = Services?.Recorder.LastResult;
            if (result == null) return; // editor preview: keep the prototype values
            var r = result.request;
            var mode = SessionManager.Instance != null ? SessionManager.Instance.Settings.mode : TrainingMode.Guided;

            // Short enough for one line of the spaced, upper-case eyebrow style.
            Set(eyebrow, $"{mode} mode · {SessionManager.FormatClock(r.durationSeconds)}");
            Set(passMark, result.criticalFailure
                ? $"Overall · a critical step was missed · pass mark {result.passThreshold:0}"
                : $"Overall · pass mark {result.passThreshold:0}");
            Set(chipLabel, r.passed ? "Pass" : "Not yet");
            if (chipFill != null && theme != null) chipFill.color = r.passed ? theme.accent : theme.warning;

            var bars = new float[Stages.Length];
            for (int i = 0; i < Stages.Length; i++)
            {
                var stage = r.stageScores.FirstOrDefault(s => s.stage == Stages[i]);
                bars[i] = stage != null ? stage.score / 100f : 0f;
                if (stageValues != null && i < stageValues.Length)
                    Set(stageValues[i], Mono(stage != null ? $"{stage.score:0}" : "—"));
            }
            if (motion != null) motion.SetValues(Mathf.RoundToInt(r.overallScore), bars);

            var played = r.stageScores.Where(s => s.stage != ScenarioStage.Summary).ToList();
            if (played.Count > 0) weakest = played.OrderBy(s => s.score).First().stage;
            Set(retryLabel, $"Retry {weakest}"); // "Retry Operate stage" is wider than the button

            for (int i = 0; i < (issueCards?.Length ?? 0); i++)
            {
                var issue = i < result.issues.Count ? result.issues[i] : null;
                if (issueCards[i] != null) issueCards[i].SetActive(issue != null || i == 0);
                if (issue == null)
                {
                    if (i == 0) { SetAt(issueTimes, i, ""); SetAt(issueTitles, i, "No protocol issues — well done"); SetAt(issueMetas, i, ""); }
                    continue;
                }
                SetAt(issueTimes, i, Mono(SessionManager.FormatClock(issue.evt.sessionTime)));
                SetAt(issueTitles, i, string.IsNullOrEmpty(issue.evt.message) ? issue.evt.code : issue.evt.message);
                SetAt(issueMetas, i, $"{Label(issue.evt.eventClass)} · {issue.evt.stage} · −{issue.penalty:0} points");
                if (issueMetas != null && i < issueMetas.Length && issueMetas[i] != null && theme != null)
                    issueMetas[i].color = issue.evt.eventClass == EventClass.Deviation ? theme.danger : theme.warning;
            }
            RefreshSync();
        }

        void RefreshSync()
        {
            if (!isActiveAndEnabled || Services == null) return;
            var s = Services;
            string text; bool ok;
            if (!s.Recorder.LastResultRecorded) { text = "Guest session · this result isn't saved"; ok = false; }
            else
            {
                var pending = s.Queue.PendingFor(s.Auth.UserId);
                var lms = s.Auth.FromLmsLaunch ? " and your LMS" : "";
                if (pending == 0) { text = $"Result saved to your account{lms}"; ok = true; }
                else if (s.Queue.Offline) { text = $"Offline · result queued, will sync to your account{lms} automatically"; ok = false; }
                else { text = "Uploading result…"; ok = true; }
            }
            Set(syncStatus, text);
            if (syncDot != null && theme != null) syncDot.color = ok ? theme.accent : theme.warning;
        }

        public void RetryWeakestStage() => ScenarioDirector.Instance?.RetryStage((int)weakest);

        static string Label(EventClass c) => c == EventClass.Deviation ? "Deviation" : c == EventClass.Delayed ? "Delayed" : c.ToString();

        // The builder styles numbers with this monospacing; setting .text replaces its tags, so re-apply them.
        static string Mono(string s) => $"<mspace=0.62em>{s}</mspace>";

        static void Set(TMP_Text t, string value) { if (t != null) t.text = value; }
        static void SetAt(TMP_Text[] ts, int i, string value) { if (ts != null && i < ts.Length) Set(ts[i], value); }
    }
}
