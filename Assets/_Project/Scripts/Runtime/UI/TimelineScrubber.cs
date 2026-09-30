using UnityEngine;
using UnityEngine.EventSystems;

namespace SurgicalFoundations.UI
{
    /// <summary>Click or drag anywhere on the timeline to seek.</summary>
    public class TimelineScrubber : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField] ReplayControlsBinder controls;

        public void OnPointerDown(PointerEventData e) => Scrub(e);
        public void OnDrag(PointerEventData e) => Scrub(e);

        void Scrub(PointerEventData e)
        {
            var rt = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var local)) return;
            var x = Mathf.InverseLerp(rt.rect.xMin, rt.rect.xMax, local.x);
            if (controls != null) controls.ScrubTo(x);
        }
    }
}
