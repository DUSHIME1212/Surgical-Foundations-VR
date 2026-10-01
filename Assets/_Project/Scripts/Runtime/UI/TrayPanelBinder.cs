using SurgicalFoundations.Detection;
using TMPro;
using UnityEngine;

namespace SurgicalFoundations.UI
{
    /// <summary>Screen 07: the opening count line by line, and whether the field is ready to be confirmed.</summary>
    public class TrayPanelBinder : MonoBehaviour
    {
        [SerializeField] ChecklistRow[] rows;
        [SerializeField] TMP_Text total;
        [SerializeField] TMP_Text status;

        void Update()
        {
            var tray = (StageController.Current as PrepController)?.Tray;
            if (tray == null || rows == null) return;

            var firstOpen = true;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null) continue;
                // The scene decides what is on the table; a line with nothing to count is not shown.
                var used = i < tray.Lines;
                if (rows[i].gameObject.activeSelf != used) rows[i].gameObject.SetActive(used);
                if (!used) continue;

                rows[i].SetLabel(tray.Name(i));
                var done = tray.LineDone(i);
                rows[i].Set(done ? StepState.Done : firstOpen ? StepState.Active : StepState.Pending, $"{tray.Confirmed(i)}/{tray.Quantity(i)}");
                if (!done) firstOpen = false;
            }

            if (total != null) total.text = $"{tray.TotalConfirmed} / {tray.TotalItems}";
            if (status != null)
                status.text = tray.Complete ? "Opening count complete"
                    : $"{tray.LinesUnchecked} line{(tray.LinesUnchecked == 1 ? "" : "s")} still to count";
        }
    }
}
