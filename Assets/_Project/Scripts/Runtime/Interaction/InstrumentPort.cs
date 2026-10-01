using System.Collections.Generic;
using UnityEngine;

namespace SurgicalFoundations.Interaction
{
    /// <summary>
    /// A placed port (trocar) that an instrument can be passed through. Added at runtime to every <c>Trocar_*</c> in a
    /// stage scene. Trocars are built long-axis +Z with the cannula at +Z, standing 9 cm proud of the skin.
    /// </summary>
    public class InstrumentPort : MonoBehaviour
    {
        const float SkinDistance = 0.09f;
        const float ValveDistance = 0.03f;

        static readonly List<InstrumentPort> all = new List<InstrumentPort>();
        public static IReadOnlyList<InstrumentPort> All => all;

        /// <summary>The instrument currently through this port; one at a time.</summary>
        public LapInstrument Occupant { get; set; }

        /// <summary>The fulcrum: where the port passes through the abdominal wall.</summary>
        public Vector3 Fulcrum => transform.position + transform.forward * SkinDistance;
        /// <summary>The valve at the top, where an instrument is introduced.</summary>
        public Vector3 Entry => transform.position - transform.forward * ValveDistance;
        public Vector3 Axis => transform.forward;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);
    }
}
