using SurgicalFoundations.Core;
using TMPro;
using UnityEngine;

namespace SurgicalFoundations.UI
{
    /// <summary>Feeds the session clock and the GUIDED / ASSESSMENT badge in the HUD.</summary>
    public class SessionHudBinder : MonoBehaviour
    {
        [SerializeField] TMP_Text clock;
        [SerializeField] TMP_Text modeBadge;

        float nextRefresh;

        void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.25f;
            var s = SessionManager.Instance;
            if (s == null) return;
            if (clock != null) clock.text = $"<mspace=0.62em>{SessionManager.FormatClock(s.ElapsedSeconds)}</mspace>";
            if (modeBadge != null) modeBadge.text = s.Settings.mode == TrainingMode.Guided ? "GUIDED" : "ASSESSMENT";
        }

        public void TogglePause() => SessionManager.Instance?.TogglePause();
    }
}
