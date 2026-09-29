using System.Collections;
using SurgicalFoundations.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SurgicalFoundations.UI
{
    /// <summary>Hover lift, press squash, sound and a haptic tick on the controller that pointed at it.</summary>
    public class UIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] SoundId hoverSound = SoundId.UI_Hover;
        [SerializeField] SoundId clickSound = SoundId.UI_Click;
        [SerializeField] float hoverScale = 1.02f;
        [SerializeField] float pressScale = 0.98f;
        [SerializeField] float hoverHaptic = 0.08f;
        [SerializeField] float clickHaptic = 0.3f;

        Selectable selectable;
        Vector3 baseScale;
        float targetScale = 1f;
        bool hovered;

        public SoundId ClickSound { get => clickSound; set => clickSound = value; }

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            baseScale = transform.localScale;
        }

        void OnDisable()
        {
            hovered = false;
            targetScale = 1f;
            transform.localScale = baseScale;
        }

        bool Interactable => selectable == null || selectable.IsInteractable();

        public void OnPointerEnter(PointerEventData e)
        {
            if (!Interactable) return;
            hovered = true;
            targetScale = hoverScale;
            AudioManager.Instance?.Play(hoverSound);
            Haptic(e, hoverHaptic, 0.02f);
        }

        public void OnPointerExit(PointerEventData e)
        {
            hovered = false;
            targetScale = 1f;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (Interactable) targetScale = pressScale;
        }

        public void OnPointerUp(PointerEventData e) => targetScale = hovered ? hoverScale : 1f;

        public void OnPointerClick(PointerEventData e)
        {
            if (!Interactable) { AudioManager.Instance?.Play(SoundId.UI_Denied); return; }
            AudioManager.Instance?.Play(clickSound);
            Haptic(e, clickHaptic, 0.04f);
        }

        void Update()
        {
            var s = transform.localScale.x / Mathf.Max(0.0001f, baseScale.x);
            if (Mathf.Abs(s - targetScale) < 0.0005f) return;
            s = Mathf.Lerp(s, targetScale, 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime));
            transform.localScale = baseScale * s;
        }

        static void Haptic(PointerEventData e, float amplitude, float duration)
        {
            if (amplitude <= 0f || !(e is TrackedDeviceEventData td) || td.interactor == null) return;
            var player = (td.interactor as Component)?.GetComponentInParent<HapticImpulsePlayer>();
            if (player != null) player.SendHapticImpulse(amplitude, duration);
        }
    }
}
