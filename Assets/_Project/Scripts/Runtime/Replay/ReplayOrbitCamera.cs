using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SurgicalFoundations.Replay
{
    /// <summary>
    /// Free camera for the replay viewer (desktop and WebGL): right- or left-drag to orbit around the operating table,
    /// middle-drag to pan, scroll to zoom. Ignores the mouse while it is over the controls panel.
    /// </summary>
    public class ReplayOrbitCamera : MonoBehaviour
    {
        [SerializeField] Vector3 target = new Vector3(0f, 1.1f, 0f);
        [SerializeField] float distance = 3.2f;
        [SerializeField] float minDistance = 0.4f;
        [SerializeField] float maxDistance = 8f;
        [SerializeField] float orbitSpeed = 0.25f;
        [SerializeField] float panSpeed = 0.0025f;

        float yaw, pitch;

        void OnEnable()
        {
            // Continue from wherever the camera is now (e.g. after the learner view), looking at the table.
            var offset = transform.position - target;
            if (offset.sqrMagnitude < 0.01f) offset = new Vector3(-1.9f, 1f, -2.4f);
            distance = Mathf.Clamp(offset.magnitude, minDistance, maxDistance);
            var dir = offset.normalized;
            pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            ApplyPose();
        }

        void LateUpdate()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            var overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            var delta = mouse.delta.ReadValue();

            if (!overUi && (mouse.leftButton.isPressed || mouse.rightButton.isPressed))
            {
                yaw += delta.x * orbitSpeed;
                pitch = Mathf.Clamp(pitch - delta.y * orbitSpeed, -10f, 85f);
            }
            if (!overUi && mouse.middleButton.isPressed)
                target -= (transform.right * delta.x + transform.up * delta.y) * panSpeed * distance;
            var scroll = mouse.scroll.ReadValue().y;
            if (!overUi && Mathf.Abs(scroll) > 0.01f)
                distance = Mathf.Clamp(distance * (1f - Mathf.Sign(scroll) * 0.1f), minDistance, maxDistance);

            ApplyPose();
        }

        void ApplyPose()
        {
            var rotation = Quaternion.Euler(pitch, yaw + 180f, 0f);
            transform.SetPositionAndRotation(target - rotation * Vector3.forward * distance, rotation);
        }
    }
}
