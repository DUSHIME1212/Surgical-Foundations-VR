using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SurgicalFoundations.Core
{
    /// <summary>
    /// Fade to black for every scene change (NFR-05: nothing may move the player's camera, so we cut under a fade).
    /// Uses a screen-space-camera canvas a few centimetres in front of the eyes so it covers both eyes in XR.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] float defaultDuration = 0.35f;
        [SerializeField] Color color = new Color(0.02f, 0.04f, 0.05f, 1f);

        Canvas canvas;
        Image image;
        Camera boundCamera;

        public bool IsOpaque => image != null && image.color.a >= 0.999f;

        void Awake()
        {
            var go = new GameObject("FadeCanvas", typeof(Canvas), typeof(Image));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.sortingOrder = short.MaxValue;
            canvas.planeDistance = 0.05f;
            image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = new Color(color.r, color.g, color.b, 0f);
            canvas.enabled = false;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != boundCamera)
            {
                boundCamera = cam;
                canvas.worldCamera = cam;
            }
        }

        public void SetOpaque()
        {
            canvas.worldCamera = Camera.main;
            canvas.enabled = true;
            image.color = new Color(color.r, color.g, color.b, 1f);
        }

        public Coroutine FadeOut(float duration = -1f) => StartCoroutine(FadeRoutine(1f, duration < 0 ? defaultDuration : duration));
        public Coroutine FadeIn(float duration = -1f) => StartCoroutine(FadeRoutine(0f, duration < 0 ? defaultDuration : duration));

        IEnumerator FadeRoutine(float target, float duration)
        {
            canvas.worldCamera = Camera.main;
            canvas.enabled = true;
            float start = image.color.a;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                image.color = new Color(color.r, color.g, color.b, Mathf.Lerp(start, target, k));
                yield return null;
            }
            image.color = new Color(color.r, color.g, color.b, target);
            if (target <= 0f) canvas.enabled = false;
        }
    }
}
