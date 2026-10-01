using SurgicalFoundations.Detection;
using UnityEngine;

namespace SurgicalFoundations.UI
{
    /// <summary>Screen 15: each port from "in place" to "removed" to "closed".</summary>
    public class ClosurePanelBinder : MonoBehaviour
    {
        [SerializeField] ChecklistRow[] rows;

        void Update()
        {
            var close = StageController.Current as CloseController;
            if (close == null || close.Removal == null || rows == null) return;
            for (int i = 0; i < rows.Length && i < close.Removal.Count; i++)
            {
                if (rows[i] == null) continue;
                if (close.Closure.IsClosed(i)) rows[i].Set(StepState.Done, "closed");
                else if (close.Removal.IsRemoved(i)) rows[i].Set(StepState.Active, "out · stitch the site");
                else rows[i].Set(StepState.Pending, "in place");
            }
        }
    }
}
