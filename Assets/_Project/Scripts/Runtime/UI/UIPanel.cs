using System.Collections;
using SurgicalFoundations.Audio;
using UnityEngine;

namespace SurgicalFoundations.UI
{
    /// <summary>Panel open/close: scale 96→100 % + fade over 150–250 ms (asset list, "Panel open and close").</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanel : MonoBehaviour
    {
        [SerializeField] float duration = 0.2f;
        [SerializeField] bool animateOnEnable = true;
        [SerializeField] bool playSounds = true;

        CanvasGroup group;
        Coroutine running;
        Vector3 baseScale; // world-space canvases carry their metres-per-pixel scale here (0.001)

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            baseScale = transform.localScale;
        }

        void OnEnable()
        {
            if (!animateOnEnable) return;
            if (playSounds) AudioManager.Instance?.Play(SoundId.UI_PanelOpen);
            Animate(true, null);
        }

        public void Show()
        {
            if (gameObject.activeSelf) return;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (!gameObject.activeInHierarchy) return;
            if (playSounds) AudioManager.Instance?.Play(SoundId.UI_PanelClose);
            Animate(false, () => gameObject.SetActive(false));
        }

        void Animate(bool show, System.Action done)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Run(show, done));
        }

        IEnumerator Run(bool show, System.Action done)
        {
            float from = show ? 0f : 1f, to = show ? 1f : 0f;
            group.interactable = show;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = EaseOutCubic(t / duration);
                float v = Mathf.Lerp(from, to, k);
                group.alpha = v;
                transform.localScale = baseScale * Mathf.Lerp(0.96f, 1f, v);
                yield return null;
            }
            group.alpha = to;
            transform.localScale = baseScale;
            running = null;
            done?.Invoke();
        }

        static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - Mathf.Clamp01(x), 3f);
    }
}
