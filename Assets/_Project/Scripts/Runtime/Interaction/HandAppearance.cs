using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace SurgicalFoundations.Interaction
{
    public enum HandLook { Bare, Gloved, Contaminated }

    /// <summary>
    /// Dresses the learner's tracked XR Hands meshes (from the XRI "XR Origin Hands" rig) for the current moment in
    /// the scenario: bare skin while scrubbing, latex gloves after gowning, a contaminated glove after a sterility
    /// break (FR-07). Lives on the XR Origin in 00_Bootstrap; <see cref="HandLookCue"/> components on step panels
    /// request looks as the scenario moves on.
    /// </summary>
    public class HandAppearance : MonoBehaviour
    {
        public static HandAppearance Instance { get; private set; }

        [SerializeField] Material bare;
        [SerializeField] Material gloved;
        [SerializeField] Material contaminated;
        [SerializeField] HandLook left = HandLook.Bare;
        [SerializeField] HandLook right = HandLook.Bare;

        readonly List<SkinnedMeshRenderer> renderers = new List<SkinnedMeshRenderer>();
        float nextScan;
        int lastCount = -1;

        public HandLook Left => left;
        public HandLook Right => right;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Set(HandLook leftLook, HandLook rightLook)
        {
            left = leftLook;
            right = rightLook;
            Apply();
        }

        void Update()
        {
            // Hand meshes can appear late (tracking acquired, modality switch), so re-check a couple of times a second.
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 0.5f;
            var root = GetComponentInParent<XROrigin>() != null ? GetComponentInParent<XROrigin>().transform : transform;
            root.GetComponentsInChildren(true, renderers);
            if (renderers.Count != lastCount) Apply();
        }

        void Apply()
        {
            var root = GetComponentInParent<XROrigin>() != null ? GetComponentInParent<XROrigin>().transform : transform;
            root.GetComponentsInChildren(true, renderers);
            lastCount = renderers.Count;
            foreach (var r in renderers)
            {
                // Only the hand meshes (LeftHand / RightHand), not the pinch and poke highlight visuals next to them.
                if (r.name.IndexOf("Hand", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                var side = SideOf(r.transform);
                if (side == 0) continue;
                var mat = For(side < 0 ? left : right);
                if (mat == null) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
        }

        Material For(HandLook look) => look switch
        {
            HandLook.Gloved => gloved,
            HandLook.Contaminated => contaminated,
            _ => bare
        };

        /// <summary>−1 left hand, +1 right hand, 0 not a hand (controller models etc.).</summary>
        static int SideOf(Transform t)
        {
            bool isHand = false;
            for (var p = t; p != null; p = p.parent)
            {
                var n = p.name;
                if (n.IndexOf("Controller", System.StringComparison.OrdinalIgnoreCase) >= 0) return 0;
                if (n.IndexOf("Hand", System.StringComparison.OrdinalIgnoreCase) >= 0) isHand = true;
                if (!isHand) continue;
                if (n.IndexOf("Left", System.StringComparison.OrdinalIgnoreCase) >= 0) return -1;
                if (n.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0) return 1;
            }
            return 0;
        }
    }
}
