using System.Collections;
using SurgicalFoundations.Core;
using SurgicalFoundations.Scenario;
using UnityEngine;
using SurgicalFoundations.Contracts;

namespace SurgicalFoundations.UI
{
    /// <summary>
    /// Coral edge vignette that flashes around the learner's view on a deviation or contamination event
    /// (asset list: "Deviation / contamination edge vignette"). Guided mode only. Uses SF/FX/View Vignette.
    /// </summary>
    public class DeviationVignette : MonoBehaviour
    {
        [SerializeField] Renderer sphere;
        [SerializeField] float peak = 0.45f;
        [SerializeField] float rise = 0.12f;
        [SerializeField] float hold = 0.5f;
        [SerializeField] float fall = 0.9f;

        MaterialPropertyBlock mpb;
        static readonly int EdgeOpacity = Shader.PropertyToID("_EdgeOpacity");
        Coroutine running;

        IEnumerator Start()
        {
            mpb = new MaterialPropertyBlock();
            Set(0f);
            sphere.enabled = false;
            while (EventLogger.Instance == null) yield return null;
            EventLogger.Instance.EventLogged += OnEvent;
        }

        void OnDestroy()
        {
            if (EventLogger.Instance != null) EventLogger.Instance.EventLogged -= OnEvent;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null && sphere.enabled) sphere.transform.position = cam.transform.position;
        }

        void OnEvent(ProtocolEvent e)
        {
            if (e.eventClass != EventClass.Deviation) return;
            var s = SessionManager.Instance;
            if (s != null && s.Settings.mode != TrainingMode.Guided) return;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Flash());
        }

        IEnumerator Flash()
        {
            sphere.enabled = true;
            for (float t = 0; t < rise; t += Time.unscaledDeltaTime) { Set(peak * t / rise); yield return null; }
            Set(peak);
            yield return new WaitForSecondsRealtime(hold);
            for (float t = 0; t < fall; t += Time.unscaledDeltaTime) { Set(peak * (1 - t / fall)); yield return null; }
            Set(0f);
            sphere.enabled = false;
        }

        void Set(float v)
        {
            sphere.GetPropertyBlock(mpb);
            mpb.SetFloat(EdgeOpacity, v);
            sphere.SetPropertyBlock(mpb);
        }
    }
}
