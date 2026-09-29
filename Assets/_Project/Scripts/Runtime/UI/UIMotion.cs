using System.Collections;
using SurgicalFoundations.Audio;
using TMPro;
using UnityEngine;

namespace SurgicalFoundations.UI
{
    /// <summary>Summary score count-up and stage bar fill (FR-20). Plays when the panel is shown.</summary>
    public class UIMotion : MonoBehaviour
    {
        [System.Serializable]
        public class Bar
        {
            public RectTransform fill;
            [Range(0f, 1f)] public float value = 0.7f;
        }

        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] int score = 82;
        [SerializeField] float duration = 1.2f;
        [SerializeField] Bar[] bars;

        void OnEnable() => StartCoroutine(Run());

        IEnumerator Run()
        {
            foreach (var b in bars) if (b.fill != null) b.fill.anchorMax = new Vector2(0f, 1f);
            if (scoreLabel != null) scoreLabel.text = "0";
            yield return new WaitForSecondsRealtime(0.25f);

            int lastShown = -1;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / duration, 3f);
                int shown = Mathf.RoundToInt(score * k);
                if (scoreLabel != null && shown != lastShown)
                {
                    scoreLabel.text = shown.ToString();
                    if (shown % 4 == 0) AudioManager.Instance?.Play(SoundId.FB_CountTick);
                    lastShown = shown;
                }
                foreach (var b in bars) if (b.fill != null) b.fill.anchorMax = new Vector2(b.value * k, 1f);
                yield return null;
            }
            if (scoreLabel != null) scoreLabel.text = score.ToString();
            foreach (var b in bars) if (b.fill != null) b.fill.anchorMax = new Vector2(b.value, 1f);
        }
    }
}
