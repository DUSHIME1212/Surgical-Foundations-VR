using SurgicalFoundations.Core;
using SurgicalFoundations.Detection;
using UnityEngine;

namespace SurgicalFoundations.UI
{
    /// <summary>Screen 05: the six scrub steps, live. The active row counts down the time still needed.</summary>
    public class ScrubPanelBinder : MonoBehaviour
    {
        [SerializeField] ChecklistRow[] rows;

        void Update()
        {
            var scrub = (StageController.Current as PrepController)?.Scrub;
            if (scrub == null || rows == null) return;
            for (int i = 0; i < rows.Length && i < ScrubSequence.StepCount; i++)
            {
                if (rows[i] == null) continue;
                var state = scrub.State(i);
                var trailing = state == StepState.Active ? SessionManager.FormatClock(Mathf.Ceil(scrub.RemainingSeconds(i)))
                    : state == StepState.Skipped ? "skipped" : "";
                rows[i].Set(state, trailing);
            }
        }
    }
}
