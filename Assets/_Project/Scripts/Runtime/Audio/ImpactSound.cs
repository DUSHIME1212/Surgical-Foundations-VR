using UnityEngine;

namespace SurgicalFoundations.Audio
{
    /// <summary>Spatial collision sound scaled by impact speed (peg rings, swabs, instruments set down).</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ImpactSound : MonoBehaviour
    {
        [SerializeField] SoundId sound = SoundId.ENV_RingDrop;
        [SerializeField] float minSpeed = 0.25f;
        [SerializeField] float cooldown = 0.08f;

        float lastTime;

        void OnCollisionEnter(Collision c)
        {
            if (c.relativeVelocity.magnitude < minSpeed || Time.time - lastTime < cooldown) return;
            lastTime = Time.time;
            AudioManager.Instance?.PlayAt(sound, c.GetContact(0).point);
        }
    }
}
