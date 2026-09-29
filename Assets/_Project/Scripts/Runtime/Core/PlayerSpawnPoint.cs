using UnityEngine;

namespace SurgicalFoundations.Core
{
    /// <summary>Where the learner stands when a scene or stage loads. Forward (blue axis) is the facing direction.</summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] int priority;
        public int Priority => priority;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.36f, 0.88f, 0.78f, 0.9f);
            var p = transform.position;
            Gizmos.DrawWireSphere(p + Vector3.up * 0.02f, 0.3f);
            Gizmos.DrawLine(p + Vector3.up * 0.02f, p + Vector3.up * 0.02f + transform.forward * 0.6f);
            Gizmos.DrawWireCube(p + Vector3.up * 0.85f, new Vector3(0.45f, 1.7f, 0.3f));
        }
    }
}
