using SurgicalFoundations.Contracts;
using SurgicalFoundations.Core;
using SurgicalFoundations.Detection;
using TMPro;
using UnityEngine;

namespace SurgicalFoundations.UI
{
    /// <summary>
    /// Screen 06: gloving instructions, the sterile state of each glove, and (only after a real break) the recovery
    /// card naming what was touched (FR-07). In Assessment mode the card stays hidden; the event is still logged.
    /// </summary>
    public class SterilityPanelBinder : MonoBehaviour
    {
        [SerializeField] UITheme theme;
        [SerializeField] GameObject glovingPanel;
        [SerializeField] GameObject deviationPanel;
        [SerializeField] TMP_Text deviationText;
        [SerializeField] TMP_Text leftGlove;
        [SerializeField] TMP_Text rightGlove;

        void Update()
        {
            var state = SterilityMonitor.Instance != null ? SterilityMonitor.Instance.State : null;
            if (state == null) return;
            var guided = SessionManager.Instance == null || SessionManager.Instance.Settings.mode == TrainingMode.Guided;
            var showCard = state.AnyContaminated && guided;
            if (deviationPanel != null && deviationPanel.activeSelf != showCard) deviationPanel.SetActive(showCard);
            if (glovingPanel != null && glovingPanel.activeSelf == showCard) glovingPanel.SetActive(!showCard);
            if (showCard && deviationText != null) deviationText.text = state.LastBreak + ".";
            Show(leftGlove, state.Left);
            Show(rightGlove, state.Right);
        }

        /// <summary>"Start re-glove" on the recovery card: the fallback when no glove packet can be picked up.</summary>
        public void Reglove()
        {
            var monitor = SterilityMonitor.Instance;
            if (monitor != null && monitor.State.AnyContaminated) monitor.State.Glove();
        }

        void Show(TMP_Text text, HandSterility s)
        {
            if (text == null) return;
            text.text = s == HandSterility.Gloved ? "Sterile" : s == HandSterility.Contaminated ? "<b>Contaminated</b>"
                : s == HandSterility.Scrubbed ? "Scrubbed, not gloved" : "Not scrubbed";
            if (theme != null) text.color = s == HandSterility.Gloved ? theme.accent : s == HandSterility.Contaminated ? theme.danger : theme.textSecondary;
        }
    }
}
