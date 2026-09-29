using UnityEngine;

namespace SurgicalFoundations.Placeholders
{
    public enum AssetRelease { MVP, R2, R3 }

    /// <summary>
    /// Marks a primitive-built stand-in for a production model from the asset list. Gameplay components, colliders
    /// and pivots live on the root; everything visual lives under the "Visual" child. To swap in the real model,
    /// replace the children of "Visual" (keep pivot names such as Pivot_Tip / Pivot_Port / Jaw_*), then untick
    /// <see cref="isPlaceholder"/>. "Surgical Foundations ▸ Placeholders ▸ Report" lists what is still grey-boxed.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlaceholderInfo : MonoBehaviour
    {
        public string assetId;
        public string displayName;
        public string category;
        public AssetRelease release = AssetRelease.MVP;
        [Tooltip("LOD0 triangle budget from the production asset list (Quest 3, 72 fps).")]
        public int triangleBudget;
        [TextArea(2, 5)] public string buildNotes;
        public bool isPlaceholder = true;

        public Transform Visual => transform.Find("Visual");

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!isPlaceholder) return;
            var bounds = new Bounds(transform.position, Vector3.zero);
            foreach (var r in GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
            Gizmos.color = new Color(0.96f, 0.71f, 0.29f, 0.8f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            UnityEditor.Handles.Label(bounds.max, $"{displayName}\n{release} · {triangleBudget / 1000f:0.#}k tris · placeholder");
        }
#endif
    }
}
