using SurgicalFoundations.Audio;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SurgicalFoundations.Interaction
{
    /// <summary>
    /// Opens and closes an instrument's jaws or blades. Jaws are open at rest and close while the instrument is
    /// activated (trigger). Replace with analogue trigger value 0–1 once the input layer lands (asset list: "Jaw open/close").
    /// </summary>
    public class InstrumentJaws : MonoBehaviour
    {
        [SerializeField] Transform upperJaw;
        [SerializeField] Transform lowerJaw;
        [SerializeField] float openAngle = 28f;
        [SerializeField] float speed = 10f;
        [SerializeField] SoundId closeSound = SoundId.INST_JawClose;
        [SerializeField] SoundId openSound = SoundId.INST_JawOpen;

        XRGrabInteractable grab;
        Quaternion upperRest, lowerRest;
        float openness = 1f;
        float target = 1f;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            if (upperJaw == null) upperJaw = FindDeep(transform, "Jaw_Upper");
            if (lowerJaw == null) lowerJaw = FindDeep(transform, "Jaw_Lower");
            if (upperJaw != null) upperRest = upperJaw.localRotation;
            if (lowerJaw != null) lowerRest = lowerJaw.localRotation;
        }

        void OnEnable()
        {
            if (grab == null) return;
            grab.activated.AddListener(OnActivated);
            grab.deactivated.AddListener(OnDeactivated);
            grab.selectEntered.AddListener(OnGrabbed);
        }

        void OnDisable()
        {
            if (grab == null) return;
            grab.activated.RemoveListener(OnActivated);
            grab.deactivated.RemoveListener(OnDeactivated);
            grab.selectEntered.RemoveListener(OnGrabbed);
        }

        void OnGrabbed(SelectEnterEventArgs _) => AudioManager.Instance?.PlayAt(SoundId.INST_PickupMetal, transform.position);

        void OnActivated(ActivateEventArgs _)
        {
            target = 0f;
            AudioManager.Instance?.PlayAt(closeSound, transform.position);
        }

        void OnDeactivated(DeactivateEventArgs _)
        {
            target = 1f;
            AudioManager.Instance?.PlayAt(openSound, transform.position);
        }

        public void SetOpenness(float value) => target = Mathf.Clamp01(value);

        /// <summary>0 = closed, 1 = open. Recorded in session replays.</summary>
        public float Openness => openness;

        void Update()
        {
            openness = Mathf.MoveTowards(openness, target, speed * Time.deltaTime);
            float a = openAngle * 0.5f * openness;
            if (upperJaw != null) upperJaw.localRotation = upperRest * Quaternion.Euler(-a, 0f, 0f);
            if (lowerJaw != null) lowerJaw.localRotation = lowerRest * Quaternion.Euler(a, 0f, 0f);
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}
