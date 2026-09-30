using SurgicalFoundations.Scenario;
using UnityEngine;
using UnityEngine.UI;
using SurgicalFoundations.Contracts;

namespace SurgicalFoundations.UI
{
    /// <summary>
    /// Advances the enclosing <see cref="StepSequence"/> when clicked, optionally logging a protocol event first.
    /// Lets screen prefabs drive the stage flow without scene-specific wiring.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class StepAdvanceButton : MonoBehaviour
    {
        public enum LogKind { None, OnProtocol, Delayed, Deviation }

        [SerializeField, Tooltip("-1 = next step. Any index ≥ step count ends the stage.")] int goToIndex = -1;
        [SerializeField] LogKind log = LogKind.None;
        [SerializeField] string message;

        void Awake() => GetComponent<Button>().onClick.AddListener(OnClick);

        void OnClick()
        {
            if (log != LogKind.None && EventLogger.Instance != null)
            {
                var cls = log == LogKind.OnProtocol ? EventClass.OnProtocol : log == LogKind.Delayed ? EventClass.Delayed : EventClass.Deviation;
                EventLogger.Instance.Log(cls, "step." + name, message);
            }

            var seq = GetComponentInParent<StepSequence>();
            if (seq == null) seq = FindAnyObjectByType<StepSequence>();
            if (seq == null) return;
            if (goToIndex < 0) seq.Next();
            else seq.GoTo(goToIndex);
        }
    }
}
