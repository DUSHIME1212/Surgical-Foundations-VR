using System.Collections;
using SurgicalFoundations.Audio;
using SurgicalFoundations.Scenario;
using UnityEngine;

namespace SurgicalFoundations.UI
{
    /// <summary>
    /// Logs a protocol event (and optional cue sound) when a step panel appears — stands in for the detection logic
    /// that will raise these events for real (e.g. contamination on the back-table edge, instrument drift).
    /// </summary>
    public class LogEventOnEnable : MonoBehaviour
    {
        [SerializeField] EventClass eventClass = EventClass.Deviation;
        [SerializeField] string code = "demo";
        [SerializeField] string message;
        [SerializeField] SoundId cue = SoundId.None;
        [SerializeField] float delay = 0.6f;

        void OnEnable() => StartCoroutine(Run());

        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(delay);
            if (cue != SoundId.None) AudioManager.Instance?.Play(cue);
            EventLogger.Instance?.Log(eventClass, code, message);
        }
    }
}
