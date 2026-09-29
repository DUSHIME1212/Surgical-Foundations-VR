using SurgicalFoundations.Audio;
using UnityEngine;

namespace SurgicalFoundations.Lighting
{
    /// <summary>
    /// One surgical light head: a mixed spot light plus an emissive lens. Intensity eases in like a real LED head
    /// and the lens emission tracks it so bloom-free reflections on instruments still read as "on".
    /// </summary>
    public class SurgicalLight : MonoBehaviour
    {
        [SerializeField] Light spot;
        [SerializeField] Renderer lens;
        [SerializeField] Color lensColor = new Color(1f, 0.97f, 0.92f);
        [SerializeField] float lensEmission = 6f;
        [SerializeField, Range(0f, 1f)] float level = 1f;
        [SerializeField] float easeTime = 0.35f;

        float current;
        float baseIntensity;
        MaterialPropertyBlock mpb;
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public float Level { get => level; set => level = Mathf.Clamp01(value); }

        void Awake()
        {
            if (spot == null) spot = GetComponentInChildren<Light>();
            baseIntensity = spot != null ? spot.intensity : 1f;
            current = level;
            mpb = new MaterialPropertyBlock();
            Apply();
        }

        public void Toggle()
        {
            level = level > 0.5f ? 0f : 1f;
            AudioManager.Instance?.PlayAt(SoundId.ENV_LightSwitch, transform.position);
        }

        void Update()
        {
            if (Mathf.Approximately(current, level)) return;
            current = Mathf.MoveTowards(current, level, Time.deltaTime / Mathf.Max(0.01f, easeTime));
            Apply();
        }

        void Apply()
        {
            if (spot != null)
            {
                spot.intensity = baseIntensity * current;
                spot.enabled = current > 0.001f;
            }
            if (lens != null)
            {
                lens.GetPropertyBlock(mpb);
                mpb.SetColor(EmissionColor, lensColor * (lensEmission * current));
                lens.SetPropertyBlock(mpb);
            }
        }
    }
}
