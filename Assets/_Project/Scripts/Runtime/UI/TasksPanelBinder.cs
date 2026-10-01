using SurgicalFoundations.Contracts;
using SurgicalFoundations.Core;
using SurgicalFoundations.Detection;
using TMPro;
using UnityEngine;

namespace SurgicalFoundations.UI
{
    /// <summary>Screen 11: the Operate task list with live progress, and the current task's metrics (Guided mode only).</summary>
    public class TasksPanelBinder : MonoBehaviour
    {
        [SerializeField] ChecklistRow[] rows;
        [SerializeField] TMP_Text time;
        [SerializeField] TMP_Text path;
        [SerializeField] TMP_Text errors;

        void Update()
        {
            var tasks = (StageController.Current as OperateController)?.Tasks;
            if (tasks == null) return;

            if (rows != null)
                for (int i = 0; i < rows.Length && i < 3; i++)
                {
                    if (rows[i] == null) continue;
                    var task = (OperateTask)i;
                    var state = tasks.State(task);
                    rows[i].Set(state, state == StepState.Skipped ? "skipped" : state == StepState.Pending ? "" : tasks.Progress(task));
                }

            // Live numbers coach; in Assessment they would be a hint, so they wait for the summary.
            var guided = SessionManager.Instance == null || SessionManager.Instance.Settings.mode == TrainingMode.Guided;
            var m = tasks.Metrics;
            if (time != null) time.text = guided ? SessionManager.FormatClock(m.seconds) : "—";
            if (path != null) path.text = guided ? $"{m.pathMetres:0.00} m" : "—";
            if (errors != null) errors.text = guided ? m.errors.ToString() : "—";
        }
    }
}
