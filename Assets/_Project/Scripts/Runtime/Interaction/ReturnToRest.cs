using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SurgicalFoundations.Interaction
{
    /// <summary>
    /// A hand-held instrument (needle holder, scalpel) goes back to its place on the Mayo stand when it is let go,
    /// instead of hanging in the air where it was released: the scrub nurse takes it back.
    /// </summary>
    public class ReturnToRest : MonoBehaviour
    {
        XRGrabInteractable grab;
        Vector3 restPosition;
        Quaternion restRotation;
        bool pending;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            restPosition = transform.position;
            restRotation = transform.rotation;
        }

        void OnEnable() { if (grab != null) grab.selectExited.AddListener(OnReleased); }
        void OnDisable() { if (grab != null) grab.selectExited.RemoveListener(OnReleased); }

        void OnReleased(SelectExitEventArgs _) => pending = true;

        void LateUpdate()
        {
            if (!pending) return;
            pending = false;
            // After the grab has finished letting go, so its own drop handling doesn't move the instrument again.
            if (grab == null || !grab.isSelected) transform.SetPositionAndRotation(restPosition, restRotation);
        }
    }
}
