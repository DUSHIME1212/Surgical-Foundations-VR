using System.Collections;
using UnityEngine;

namespace SurgicalFoundations.Audio
{
    /// <summary>Plays a looping sound from this object while enabled (room tone, machine hum, tap water).</summary>
    public class AmbientEmitter : MonoBehaviour
    {
        [SerializeField] SoundId sound = SoundId.AMB_OR_RoomTone;
        [SerializeField] bool spatial = true;
        [SerializeField] float fadeIn = 1.5f;
        [SerializeField] float fadeOut = 0.8f;

        AudioManager.LoopHandle handle;

        public SoundId Sound { get => sound; set => sound = value; }

        IEnumerator Start()
        {
            // AudioManager lives in the bootstrap scene, which may finish loading after this one.
            while (AudioManager.Instance == null) yield return null;
            if (isActiveAndEnabled && handle == null) Begin();
        }

        void OnEnable()
        {
            if (AudioManager.Instance != null && handle == null) Begin();
        }

        void OnDisable()
        {
            handle?.Stop(fadeOut);
            handle = null;
        }

        void Begin() => handle = AudioManager.Instance.PlayLoop(sound, spatial ? transform : null, fadeIn);
    }
}
