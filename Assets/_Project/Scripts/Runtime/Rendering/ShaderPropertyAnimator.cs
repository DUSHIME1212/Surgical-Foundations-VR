using UnityEngine;

namespace SurgicalFoundations.Rendering
{
    /// <summary>
    /// Animates one float material property through a MaterialPropertyBlock (no material instances, SRP-batcher safe
    /// for the renderer's other properties). Drives e.g. blood _Spread, _Dissolve, glove _Contamination.
    /// </summary>
    public class ShaderPropertyAnimator : MonoBehaviour
    {
        public enum Mode { Once, Loop, PingPong }

        [SerializeField] string property = "_Spread";
        [SerializeField] float from;
        [SerializeField] float to = 1f;
        [SerializeField] float duration = 4f;
        [SerializeField] float delay;
        [SerializeField] Mode mode = Mode.Once;
        [SerializeField] AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        Renderer target;
        MaterialPropertyBlock mpb;
        int id;
        float startTime;

        public void Configure(string prop, float fromValue, float toValue, float seconds, Mode playMode, float startDelay = 0f)
        {
            property = prop; from = fromValue; to = toValue; duration = seconds; mode = playMode; delay = startDelay;
        }

        void OnEnable()
        {
            target = GetComponent<Renderer>();
            mpb ??= new MaterialPropertyBlock();
            id = Shader.PropertyToID(property);
            startTime = Time.time + delay;
            Apply(from);
        }

        void Update()
        {
            if (target == null || duration <= 0f) return;
            float t = (Time.time - startTime) / duration;
            if (t < 0f) return;
            t = mode switch
            {
                Mode.Loop => Mathf.Repeat(t, 1f),
                Mode.PingPong => Mathf.PingPong(t, 1f),
                _ => Mathf.Clamp01(t)
            };
            Apply(Mathf.LerpUnclamped(from, to, curve.Evaluate(t)));
        }

        void Apply(float value)
        {
            if (target == null) return;
            target.GetPropertyBlock(mpb);
            mpb.SetFloat(id, value);
            target.SetPropertyBlock(mpb);
        }
    }
}
