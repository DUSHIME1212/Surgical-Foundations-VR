using SurgicalFoundations.Detection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurgicalFoundations.UI
{
    /// <summary>A checklist row whose state changes at runtime: pending ring, highlighted active row, filled tick, or skipped.</summary>
    public class ChecklistRow : MonoBehaviour
    {
        [SerializeField] UITheme theme;
        [SerializeField] Image background;
        [SerializeField] Image ring;
        [SerializeField] Image dot;
        [SerializeField] TMP_Text label;
        [SerializeField] TMP_Text trailing;

        StepState shown = (StepState)(-1);

        public void Set(StepState state, string trailingText = "")
        {
            if (trailing != null) trailing.text = trailingText ?? "";
            if (state == shown) return;
            shown = state;
            if (background != null) background.enabled = state == StepState.Active;
            if (ring != null)
            {
                ring.enabled = state != StepState.Done;
                if (theme != null) ring.color = state == StepState.Active ? theme.accent : state == StepState.Skipped ? theme.warning : theme.textMuted;
            }
            if (dot != null) dot.gameObject.SetActive(state == StepState.Done);
            if (theme == null) return;
            if (label != null) label.color = state == StepState.Skipped ? theme.warning : state == StepState.Done ? theme.textSecondary : theme.textPrimary;
            if (trailing != null) trailing.color = state == StepState.Skipped ? theme.warning : state == StepState.Active ? theme.accent : theme.textSecondary;
        }

        public void SetLabel(string text) { if (label != null) label.text = text; }
    }
}
