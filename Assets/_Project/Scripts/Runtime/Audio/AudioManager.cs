using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurgicalFoundations.Audio
{
    /// <summary>
    /// Central audio service in 00_Bootstrap: pooled one-shots (2D and spatial), looping emitters, per-category volume,
    /// ambience ducking under voice prompts, and subtitle notifications for voice lines.
    /// UI, feedback and voice keep playing while paused; world audio pauses with AudioListener.pause.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] SoundBank bank;
        [SerializeField, Range(4, 48)] int poolSize = 24;

        [Header("Mix (linear)")]
        [SerializeField, Range(0f, 1f)] float master = 1f;
        [SerializeField, Range(0f, 1f)] float ui = 0.8f;
        [SerializeField, Range(0f, 1f)] float feedback = 0.9f;
        [SerializeField, Range(0f, 1f)] float sfx = 1f;
        [SerializeField, Range(0f, 1f)] float ambience = 0.7f;
        [SerializeField, Range(0f, 1f)] float voice = 1f;

        [Header("Ducking")]
        [SerializeField, Range(0f, 1f)] float ambienceDuckLevel = 0.45f;
        [SerializeField] float duckFadeTime = 0.25f;

        readonly List<AudioSource> pool = new List<AudioSource>();
        readonly List<LoopHandle> loops = new List<LoopHandle>();
        int nextSource;
        float duck = 1f;
        int activeVoiceLines;
        AudioSource voiceSource;

        /// <summary>Raised when a voice line with a subtitle starts (text, duration).</summary>
        public event Action<string, float> SubtitleRequested;

        public SoundBank Bank => bank;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            for (int i = 0; i < poolSize; i++) pool.Add(CreateSource($"OneShot_{i:00}"));
            voiceSource = CreateSource("Voice");
            voiceSource.ignoreListenerPause = true;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        AudioSource CreateSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.dopplerLevel = 0f;
            return src;
        }

        void Update()
        {
            float target = activeVoiceLines > 0 ? ambienceDuckLevel : 1f;
            duck = Mathf.MoveTowards(duck, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, duckFadeTime));
            for (int i = loops.Count - 1; i >= 0; i--)
            {
                var h = loops[i];
                if (h.source == null) { loops.RemoveAt(i); continue; }
                h.Tick(Time.unscaledDeltaTime);
                h.source.volume = h.def.volume * h.fade * CategoryVolume(h.def.category);
                if (h.stopping && h.fade <= 0f) { Destroy(h.source.gameObject); loops.RemoveAt(i); }
            }
        }

        public float CategoryVolume(AudioCategory c)
        {
            float v = c switch
            {
                AudioCategory.UI => ui,
                AudioCategory.Feedback => feedback,
                AudioCategory.SFX => sfx,
                AudioCategory.Ambience => ambience * duck,
                AudioCategory.Voice => voice,
                _ => 1f
            };
            return v * master;
        }

        public void SetCategoryVolume(AudioCategory c, float value)
        {
            value = Mathf.Clamp01(value);
            switch (c)
            {
                case AudioCategory.UI: ui = value; break;
                case AudioCategory.Feedback: feedback = value; break;
                case AudioCategory.SFX: sfx = value; break;
                case AudioCategory.Ambience: ambience = value; break;
                case AudioCategory.Voice: voice = value; break;
            }
        }

        /// <summary>2D one-shot (UI, feedback) or, if the sound is spatial, played at the listener.</summary>
        public AudioSource Play(SoundId id) => PlayInternal(id, null, false);

        /// <summary>Spatial one-shot at a world position.</summary>
        public AudioSource PlayAt(SoundId id, Vector3 position) => PlayInternal(id, position, true);

        /// <summary>
        /// A spoken line with no recording yet (the scrub nurse's reactions): shown as a subtitle for as long as it
        /// would take to say. Swap for a recorded clip per line when the voice-over is produced.
        /// </summary>
        public void Say(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            var words = line.Split(' ').Length;
            SubtitleRequested?.Invoke(line, Mathf.Clamp(words * 0.38f, 1.8f, 6f));
        }

        AudioSource PlayInternal(SoundId id, Vector3? position, bool forceSpatial)
        {
            var def = bank != null ? bank.Get(id) : null;
            var clip = def?.PickClip();
            if (clip == null) return null;
            if (def.category == AudioCategory.Voice) return PlayVoice(def, clip);

            var src = NextSource();
            src.transform.position = position ?? transform.position;
            src.clip = clip;
            src.loop = false;
            src.volume = def.volume * CategoryVolume(def.category);
            src.pitch = 1f + UnityEngine.Random.Range(-def.pitchVariance, def.pitchVariance);
            src.spatialBlend = position.HasValue ? Mathf.Max(def.spatialBlend, forceSpatial ? 1f : 0f) : 0f;
            src.minDistance = def.minDistance;
            src.maxDistance = def.maxDistance;
            src.ignoreListenerPause = def.category == AudioCategory.UI || def.category == AudioCategory.Feedback;
            src.Play();
            return src;
        }

        AudioSource PlayVoice(SoundDefinition def, AudioClip clip)
        {
            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.volume = def.volume * CategoryVolume(AudioCategory.Voice);
            voiceSource.spatialBlend = 0f;
            voiceSource.Play();
            if (!string.IsNullOrEmpty(def.subtitle)) SubtitleRequested?.Invoke(def.subtitle, clip.length + 0.6f);
            StartCoroutine(TrackVoice(clip.length));
            return voiceSource;
        }

        IEnumerator TrackVoice(float length)
        {
            activeVoiceLines++;
            yield return new WaitForSecondsRealtime(length);
            activeVoiceLines = Mathf.Max(0, activeVoiceLines - 1);
        }

        AudioSource NextSource()
        {
            // Prefer a free source; otherwise steal round-robin.
            for (int i = 0; i < pool.Count; i++)
            {
                var s = pool[(nextSource + i) % pool.Count];
                if (!s.isPlaying) { nextSource = (nextSource + i + 1) % pool.Count; return s; }
            }
            var steal = pool[nextSource];
            nextSource = (nextSource + 1) % pool.Count;
            return steal;
        }

        /// <summary>Starts a looping sound attached to a transform (or 2D if null). Stop via the handle.</summary>
        public LoopHandle PlayLoop(SoundId id, Transform attachTo, float fadeIn = 1f)
        {
            var def = bank != null ? bank.Get(id) : null;
            var clip = def?.PickClip();
            if (clip == null) return null;

            var src = CreateSource($"Loop_{id}");
            if (attachTo != null)
            {
                src.transform.SetParent(attachTo, false);
                src.spatialBlend = def.spatialBlend;
            }
            src.clip = clip;
            src.loop = true;
            src.minDistance = def.minDistance;
            src.maxDistance = def.maxDistance;
            src.volume = 0f;
            src.time = UnityEngine.Random.Range(0f, clip.length); // de-correlate identical loops
            src.Play();

            var handle = new LoopHandle(src, def, fadeIn);
            loops.Add(handle);
            return handle;
        }

        public class LoopHandle
        {
            internal readonly AudioSource source;
            internal readonly SoundDefinition def;
            internal float fade;
            internal bool stopping;
            float fadeTime;

            internal LoopHandle(AudioSource s, SoundDefinition d, float fadeIn)
            {
                source = s; def = d; fadeTime = Mathf.Max(0.01f, fadeIn);
            }

            internal void Tick(float dt) => fade = Mathf.Clamp01(fade + (stopping ? -dt : dt) / fadeTime);

            public void Stop(float fadeOut = 0.6f)
            {
                fadeTime = Mathf.Max(0.01f, fadeOut);
                stopping = true;
            }
        }
    }
}
