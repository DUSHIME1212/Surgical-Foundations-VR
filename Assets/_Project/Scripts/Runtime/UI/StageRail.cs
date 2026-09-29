using SurgicalFoundations.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurgicalFoundations.UI
{
    /// <summary>PREP · ACCESS · OPERATE · CLOSE pill rail. Done = ✓ + mint text, current = mint pill, next = muted.</summary>
    public class StageRail : MonoBehaviour
    {
        [SerializeField] UITheme theme;
        [SerializeField] Image[] pills = new Image[4];
        [SerializeField] TMP_Text[] labels = new TMP_Text[4];

        static readonly string[] Names = { "PREP", "ACCESS", "OPERATE", "CLOSE" };
        ScenarioDirector director;

        void OnEnable()
        {
            director = ScenarioDirector.Instance;
            if (director != null)
            {
                director.StageChanged += Show;
                Show(director.Stage);
            }
        }

        void OnDisable()
        {
            if (director != null) director.StageChanged -= Show;
        }

        public void Show(ScenarioStage stage)
        {
            int current = (int)stage;
            for (int i = 0; i < Names.Length; i++)
            {
                bool done = i < current, now = i == current;
                if (pills[i] != null)
                {
                    pills[i].enabled = now;
                    pills[i].color = theme.accent;
                }
                if (labels[i] != null)
                {
                    // Sprite tick: the default font has no U+2713 (sprite asset assigned by the UI builder).
                    labels[i].text = done ? "<sprite name=\"check\" tint=1> " + Names[i] : Names[i];
                    labels[i].color = now ? theme.onAccent : done ? theme.accent : theme.textSecondary;
                }
            }
        }
    }
}
