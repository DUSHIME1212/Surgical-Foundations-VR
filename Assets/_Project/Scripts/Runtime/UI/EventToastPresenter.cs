using System.Collections;
using SurgicalFoundations.Core;
using SurgicalFoundations.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SurgicalFoundations.UI
{
    /// <summary>
    /// "ON PROTOCOL · Nails cleaned 00:48" toast. Slides in, coloured by class (FR-16), Guided mode only.
    /// </summary>
    public class EventToastPresenter : MonoBehaviour
    {
        [SerializeField] UITheme theme;
        [SerializeField] CanvasGroup group;
        [SerializeField] Image outline;
        [SerializeField] Image chip;
        [SerializeField] TMP_Text chipLabel;
        [SerializeField] TMP_Text message;
        [SerializeField] TMP_Text time;
        [SerializeField] float holdSeconds = 3.5f;

        RectTransform rect;
        Vector2 restPos;
        Coroutine running;

        void Awake()
        {
            rect = (RectTransform)group.transform;
            restPos = rect.anchoredPosition;
            group.alpha = 0f;
        }

        IEnumerator Start()
        {
            // The logger lives in 00_Bootstrap, which loads after this scene when a scene is opened directly.
            while (EventLogger.Instance == null) yield return null;
            EventLogger.Instance.EventLogged += OnEvent;
        }

        void OnDestroy()
        {
            if (EventLogger.Instance != null) EventLogger.Instance.EventLogged -= OnEvent;
        }

        void OnEvent(ProtocolEvent e)
        {
            if (e.eventClass == EventClass.Info) return;
            var s = SessionManager.Instance;
            if (s != null && s.Settings.mode != TrainingMode.Guided) return;

            Color c = e.eventClass switch
            {
                EventClass.OnProtocol => theme.accent,
                EventClass.Delayed => theme.warning,
                _ => theme.danger
            };
            chip.color = c;
            outline.color = c;
            chipLabel.text = e.eventClass switch
            {
                EventClass.OnProtocol => "ON PROTOCOL",
                EventClass.Delayed => "DELAYED",
                _ => "DEVIATION"
            };
            message.text = e.message;
            time.text = $"<mspace=0.62em>{SessionManager.FormatClock(e.sessionTime)}</mspace>";
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            const float slide = 0.22f;
            for (float t = 0; t < slide; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / slide, 3f);
                group.alpha = k;
                rect.anchoredPosition = restPos + new Vector2(0f, 40f * (1f - k));
                yield return null;
            }
            group.alpha = 1f;
            rect.anchoredPosition = restPos;
            yield return new WaitForSecondsRealtime(holdSeconds);
            for (float t = 0; t < slide; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / slide;
                yield return null;
            }
            group.alpha = 0f;
        }
    }
}
