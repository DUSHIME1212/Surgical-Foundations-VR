using SurgicalFoundations.Audio;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SurgicalFoundations.Interaction
{
    /// <summary>Plays a spatial sound when an interactable is picked up and, optionally, when released.</summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class GrabSound : MonoBehaviour
    {
        [SerializeField] SoundId onGrab = SoundId.INST_PickupMetal;
        [SerializeField] SoundId onRelease = SoundId.None;

        XRBaseInteractable interactable;

        void Awake() => interactable = GetComponent<XRBaseInteractable>();

        void OnEnable()
        {
            interactable.selectEntered.AddListener(Grabbed);
            interactable.selectExited.AddListener(Released);
        }

        void OnDisable()
        {
            interactable.selectEntered.RemoveListener(Grabbed);
            interactable.selectExited.RemoveListener(Released);
        }

        void Grabbed(SelectEnterEventArgs _)
        {
            if (onGrab != SoundId.None) AudioManager.Instance?.PlayAt(onGrab, transform.position);
        }

        void Released(SelectExitEventArgs _)
        {
            if (onRelease != SoundId.None) AudioManager.Instance?.PlayAt(onRelease, transform.position);
        }
    }
}
