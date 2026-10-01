using SurgicalFoundations.Contracts;
using SurgicalFoundations.Core;
using SurgicalFoundations.Detection;
using TMPro;
using UnityEngine;

namespace SurgicalFoundations.UI
{
    /// <summary>Screen 08: each port site, measured against its landmark. Distances and on/off target show in Guided mode only.</summary>
    public class PortSitesPanelBinder : MonoBehaviour
    {
        [SerializeField] UITheme theme;
        [SerializeField] TMP_Text[] status;
        [SerializeField] TMP_Text[] detail;

        void Update()
        {
            var plan = (StageController.Current as AccessController)?.Plan;
            if (plan == null || status == null) return;
            var guided = SessionManager.Instance == null || SessionManager.Instance.Settings.mode == TrainingMode.Guided;
            for (int i = 0; i < status.Length && i < plan.Sites.Count; i++)
            {
                var site = plan.Sites[i];
                if (status[i] != null)
                {
                    status[i].text = !site.marked ? "Pending" : !guided ? "Marked" : site.onTarget ? "On target" : "Off target";
                    if (theme != null) status[i].color = !site.marked ? theme.textSecondary : !guided || site.onTarget ? theme.accent : theme.warning;
                }
                if (detail != null && i < detail.Length && detail[i] != null)
                    detail[i].text = !site.marked || !guided ? ""
                        : $"<mspace=0.62em>{site.errorMm:0}</mspace> mm from target" + (site.onTarget ? "" : " · point again to re-mark");
            }
        }
    }
}
