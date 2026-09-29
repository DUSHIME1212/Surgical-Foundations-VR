using UnityEngine;

namespace SurgicalFoundations.Rendering
{
    /// <summary>Gentle procedural bob/sway/spin for demo props (ghost-hand demos, gallery pedestals). Local space.</summary>
    public class SimpleMotion : MonoBehaviour
    {
        [SerializeField] Vector3 bobAmplitude = new Vector3(0f, 0.02f, 0f);
        [SerializeField] float bobSpeed = 0.8f;
        [SerializeField] Vector3 swayDegrees = new Vector3(0f, 0f, 6f);
        [SerializeField] float swaySpeed = 0.6f;
        [SerializeField] float spinDegreesPerSecond;

        Vector3 basePos;
        Quaternion baseRot;

        public void Configure(Vector3 bob, float bobHz, Vector3 sway, float swayHz, float spin)
        {
            bobAmplitude = bob; bobSpeed = bobHz; swayDegrees = sway; swaySpeed = swayHz; spinDegreesPerSecond = spin;
        }

        void OnEnable()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
        }

        void Update()
        {
            float t = Time.time;
            transform.localPosition = basePos + bobAmplitude * Mathf.Sin(t * bobSpeed * Mathf.PI * 2f);
            var sway = swayDegrees * Mathf.Sin(t * swaySpeed * Mathf.PI * 2f);
            transform.localRotation = baseRot * Quaternion.Euler(sway) * Quaternion.Euler(0f, t * spinDegreesPerSecond, 0f);
        }
    }
}
