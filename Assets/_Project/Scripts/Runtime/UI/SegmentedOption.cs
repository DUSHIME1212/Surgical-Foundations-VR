using SurgicalFoundations.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurgicalFoundations.UI
{
    /// <summary>
    /// One option in a segmented control (Guided / Assessment, Seated / Standing, Controllers / Hands, text size).
    /// Selected = mint outline, soft mint fill, mint label; unselected = neutral outline, primary label.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class SegmentedOption : MonoBehaviour
    {
        [SerializeField] UITheme theme;
        [SerializeField] Image fill;
        [SerializeField] Image outline;
        [SerializeField] TMP_Text[] labels;
        [SerializeField] bool tintLabels = true;

        Toggle toggle;

        void Awake()
        {
            toggle = GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(OnChanged);
        }

        void OnEnable() => Refresh();

        void OnChanged(bool on)
        {
            if (on) AudioManager.Instance?.Play(SoundId.UI_Toggle);
            Refresh();
        }

        public void Refresh()
        {
            if (theme == null || toggle == null) return;
            bool on = toggle.isOn;
            if (fill != null) fill.color = on ? theme.accentSoft : theme.card;
            if (outline != null) outline.color = on ? theme.accent : theme.cardBorder;
            // Only the title label is tinted; a description line (if any) keeps its secondary colour.
            if (tintLabels && labels != null && labels.Length > 0 && labels[0] != null)
                labels[0].color = on ? theme.accent : theme.textPrimary;
        }
    }
}
