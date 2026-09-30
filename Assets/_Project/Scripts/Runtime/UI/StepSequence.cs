using SurgicalFoundations.Audio;
using SurgicalFoundations.Scenario;
using UnityEngine;
using SurgicalFoundations.Contracts;

namespace SurgicalFoundations.UI
{
    /// <summary>
    /// Steps through the panels of one stage (e.g. Prep: scrub → sterility → tray & drape). The last step hands
    /// over to the ScenarioDirector. Detection logic will call <see cref="Next"/> instead of the buttons later.
    /// </summary>
    public class StepSequence : MonoBehaviour
    {
        [SerializeField] GameObject[] steps;
        [SerializeField] SoundId[] voiceOnStep;
        [SerializeField, Tooltip("Optional: where the learner stands for each step. Null = stay put.")] Transform[] stepSpawns;
        [SerializeField] bool advanceScenarioAtEnd = true;

        int index = -1;

        void Start() => Show(0);

        public void Next() => GoTo(index + 1);

        /// <summary>Jump to a step (branches). Past the last step ends the stage.</summary>
        public void GoTo(int i)
        {
            if (i < steps.Length) Show(i);
            else if (advanceScenarioAtEnd) ScenarioDirector.Instance?.Next();
        }

        public void Show(int i)
        {
            int previous = index;
            index = Mathf.Clamp(i, 0, steps.Length - 1);
            // Step 0's spot is the stage spawn point (placed by SceneLoader); later steps may move the learner.
            if (previous >= 0 && stepSpawns != null && index < stepSpawns.Length && stepSpawns[index] != null)
                Core.SceneLoader.Instance?.TeleportTo(stepSpawns[index]);
            for (int s = 0; s < steps.Length; s++)
                if (steps[s] != null) steps[s].SetActive(s == index);
            if (voiceOnStep != null && index < voiceOnStep.Length && voiceOnStep[index] != SoundId.None)
                AudioManager.Instance?.Play(voiceOnStep[index]);
        }

        public void LogOnProtocol(string message) => EventLogger.Instance?.Log(EventClass.OnProtocol, "demo.onprotocol", message);
        public void LogDelayed(string message) => EventLogger.Instance?.Log(EventClass.Delayed, "demo.delayed", message);
        public void LogDeviation(string message) => EventLogger.Instance?.Log(EventClass.Deviation, "demo.deviation", message);
    }
}
