using SurgicalFoundations.Detection;
using TMPro;
using UnityEngine;

namespace SurgicalFoundations.UI
{
    /// <summary>Screen 14: the closing swab count against the opening count; closure stays blocked until they match (FR-15).</summary>
    public class CountPanelBinder : MonoBehaviour
    {
        [SerializeField] UITheme theme;
        [SerializeField] TMP_Text swabsOpening;
        [SerializeField] TMP_Text swabsNow;
        [SerializeField] TMP_Text swabsStatus;
        [SerializeField] GameObject blockedCard;
        [SerializeField] TMP_Text blockedTitle;

        void Update()
        {
            var count = (StageController.Current as CloseController)?.Count;
            if (count == null) return;
            if (swabsOpening != null) swabsOpening.text = count.Opening.ToString();
            if (swabsNow != null)
            {
                swabsNow.text = count.Counted.ToString();
                if (theme != null) swabsNow.color = count.Complete ? theme.textPrimary : theme.warning;
            }
            if (swabsStatus != null)
            {
                swabsStatus.text = count.Complete ? "<sprite name=\"check\" tint=1> Match" : $"{count.Missing} to count";
                if (theme != null) swabsStatus.color = count.Complete ? theme.accent : theme.warning;
            }
            if (blockedCard != null && blockedCard.activeSelf == count.Complete) blockedCard.SetActive(!count.Complete);
            if (blockedTitle != null && !count.Complete)
                blockedTitle.text = $"Closure blocked: {count.Missing} swab{(count.Missing == 1 ? "" : "s")} unaccounted for";
        }
    }
}
