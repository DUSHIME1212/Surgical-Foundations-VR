using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurgicalFoundations.Audio
{
    [Serializable]
    public class SoundDefinition
    {
        public SoundId id;
        public AudioCategory category;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0f, 0.3f)] public float pitchVariance;
        public bool loop;
        [Tooltip("0 = 2D (UI, voice), 1 = fully spatialised world sound.")]
        [Range(0f, 1f)] public float spatialBlend;
        public float minDistance = 0.5f;
        public float maxDistance = 12f;
        [Tooltip("Subtitle shown while this voice line plays (US-ACS-04).")]
        [TextArea] public string subtitle;

        public AudioClip PickClip() =>
            clips == null || clips.Length == 0 ? null : clips[UnityEngine.Random.Range(0, clips.Length)];
    }

    /// <summary>Data-driven sound table. Designers tune volume, spatialisation and variation here, not in code.</summary>
    [CreateAssetMenu(menuName = "Surgical Foundations/Audio/Sound Bank", fileName = "SoundBank")]
    public class SoundBank : ScriptableObject
    {
        public List<SoundDefinition> sounds = new List<SoundDefinition>();

        Dictionary<SoundId, SoundDefinition> lookup;

        public SoundDefinition Get(SoundId id)
        {
            if (lookup == null || lookup.Count != sounds.Count)
            {
                lookup = new Dictionary<SoundId, SoundDefinition>(sounds.Count);
                foreach (var s in sounds) lookup[s.id] = s;
            }
            return lookup.TryGetValue(id, out var def) ? def : null;
        }

        void OnValidate() => lookup = null;
    }
}
