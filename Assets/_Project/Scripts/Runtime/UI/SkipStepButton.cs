using SurgicalFoundations.Detection;
using UnityEngine;
using UnityEngine.UI;

namespace SurgicalFoundations.UI
{
    /// <summary>
    /// "Skip step" on a stage panel: moves on without the step being detected as done. The stage controller logs
    /// whatever was left undone as deviations, so skipping is never a way around the assessment.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SkipStepButton : MonoBehaviour
    {
        void Awake() => GetComponent<Button>().onClick.AddListener(() => StageController.Current?.SkipStep());
    }
}
