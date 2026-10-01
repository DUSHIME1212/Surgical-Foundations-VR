using SurgicalFoundations.Detection;
using UnityEngine;
using UnityEngine.UI;

namespace SurgicalFoundations.UI
{
    /// <summary>
    /// The camera-assistant chips under the monitor. The six command chips ask the assistant to move the scope; the
    /// last chip switches between the assistant holding it and the learner holding it. Command chips go dim while the
    /// learner has the scope, because there is nobody to ask.
    /// </summary>
    public class CameraAssistantBar : MonoBehaviour
    {
        // Order matches CameraCommand: left, right, up, down, zoom in, zoom out.
        [SerializeField] Button[] commands;
        [SerializeField] Button holdMyself;      // shown while the assistant holds the scope
        [SerializeField] Button holdingMyself;   // shown (selected) while the learner holds it

        void Awake()
        {
            for (int i = 0; i < commands.Length; i++)
            {
                var command = (CameraCommand)i;
                if (commands[i] != null) commands[i].onClick.AddListener(() => CameraAssistant.Instance?.Command(command));
            }
            if (holdMyself != null) holdMyself.onClick.AddListener(() => CameraAssistant.Instance?.SetSelfHold(true));
            if (holdingMyself != null) holdingMyself.onClick.AddListener(() => CameraAssistant.Instance?.SetSelfHold(false));
        }

        void Update()
        {
            var assistant = CameraAssistant.Instance;
            var ready = assistant != null && assistant.Ready;
            var self = ready && assistant.SelfHold;
            foreach (var chip in commands)
                if (chip != null && chip.interactable != (ready && !self)) chip.interactable = ready && !self;
            if (holdMyself != null && holdMyself.gameObject.activeSelf == self) holdMyself.gameObject.SetActive(!self);
            if (holdingMyself != null && holdingMyself.gameObject.activeSelf != self) holdingMyself.gameObject.SetActive(self);
        }
    }
}
