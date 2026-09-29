using TMPro;
using UnityEngine;

namespace SurgicalFoundations.UI
{
    /// <summary>
    /// Design tokens sampled from the headset UI flow prototype (Design.pdf): dark teal glass panels, mint accent,
    /// coral for deviations, amber for warnings. The UI builder and runtime widgets both read from this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Surgical Foundations/UI/Theme", fileName = "UITheme")]
    public class UITheme : ScriptableObject
    {
        [Header("Surfaces")]
        public Color backdrop = Hex("0B1517");
        public Color panel = Hex("0F1D20", 0.97f);
        public Color panelBorder = Hex("2A4145");
        public Color card = Hex("0B1719");
        public Color cardBorder = Hex("24393C");
        public Color track = Hex("1F3236");
        public Color divider = Hex("1E3034");

        [Header("Accent")]
        public Color accent = Hex("5CE0C8");
        public Color accentSoft = Hex("123430");
        public Color accentChip = Hex("1D3A36");
        public Color onAccent = Hex("0A1A1A");

        [Header("Status")]
        public Color danger = Hex("FF8A79");
        public Color dangerSurface = Hex("211111", 0.97f);
        public Color dangerInset = Hex("190D0D");
        public Color warning = Hex("F5B54B");
        public Color warningSurface = Hex("251E0C", 0.97f);

        [Header("Text")]
        public Color textPrimary = Hex("E8F0EF");
        public Color textSecondary = Hex("A7B8B7");
        public Color textMuted = Hex("7C9191");

        [Header("Type")]
        [Tooltip("Swap for Inter / Instrument Sans SDF when the font is licensed and imported.")]
        public TMP_FontAsset font;
        [Tooltip("Optional true monospace (e.g. JetBrains Mono SDF). If empty, <mspace> is used for tabular digits.")]
        public TMP_FontAsset monoFont;

        [Header("Motion")]
        public float panelTransition = 0.2f;

        public static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            c.a = alpha;
            return c;
        }
    }
}
