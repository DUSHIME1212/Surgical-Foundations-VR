using SurgicalFoundations.Core;
using SurgicalFoundations.Detection;
using UnityEngine;

namespace SurgicalFoundations.UI
{
    /// <summary>Screen 10: the three steps of managing the port-site bleed, with the time still to hold or watch.</summary>
    public class BleedPanelBinder : MonoBehaviour
    {
        [SerializeField] ChecklistRow[] rows;

        void Update()
        {
            var bleed = (StageController.Current as AccessController)?.Bleed;
            if (bleed == null || rows == null) return;
            for (int i = 0; i < rows.Length && i < 3; i++)
            {
                if (rows[i] == null) continue;
                var state = bleed.State((BleedPhase)i);
                var trailing = state == StepState.Skipped ? "skipped"
                    : state == StepState.Active && bleed.Remaining > 0f ? SessionManager.FormatClock(Mathf.Ceil(bleed.Remaining)) : "";
                rows[i].Set(state, trailing);
            }
        }
    }
}
