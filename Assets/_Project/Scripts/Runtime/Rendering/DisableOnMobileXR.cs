using UnityEngine;

namespace SurgicalFoundations.Rendering
{
    /// <summary>
    /// Turns off expensive, purely atmospheric effects (e.g. full-screen transparent laminar flow) on standalone
    /// headsets to protect the 72 fps budget (NFR-01). Tick <see cref="keepOnMobile"/> if profiling shows headroom.
    /// </summary>
    public class DisableOnMobileXR : MonoBehaviour
    {
        [SerializeField] bool keepOnMobile;

        void Awake()
        {
            if (Application.platform == RuntimePlatform.Android && !keepOnMobile) gameObject.SetActive(false);
        }
    }
}
