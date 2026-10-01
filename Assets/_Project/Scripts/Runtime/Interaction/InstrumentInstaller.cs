using System.Collections.Generic;
using SurgicalFoundations.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SurgicalFoundations.Interaction
{
    /// <summary>
    /// Turns the props of the Operate and Close scenes into working ports and instruments when the scene loads, so the
    /// baked scenes need no rebuild: every <c>Trocar_*</c> becomes an <see cref="InstrumentPort"/> and every instrument
    /// with a shaft (Pivot_Tip + Pivot_Port) becomes a <see cref="LapInstrument"/>.
    /// </summary>
    public static class InstrumentInstaller
    {
        // Free places on the Mayo stand, beside the instruments the scene builder lays out there.
        static readonly Vector3 FirstRestSlot = new Vector3(0.6f, 1.085f, -0.82f);
        static readonly Quaternion RestRotation = Quaternion.Euler(0, 0, 90);
        const float RestSlotSpacing = 0.1f;

        static readonly Dictionary<Scene, int> slotsUsed = new Dictionary<Scene, int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            slotsUsed.Clear();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        static void OnSceneUnloaded(Scene scene) => slotsUsed.Remove(scene);

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneIds.StageOperate && scene.name != SceneIds.StageClose) return;

            foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Trocar_"))
                {
                    if (t.GetComponent<InstrumentPort>() == null) t.gameObject.AddComponent<InstrumentPort>();
                    // While operating, a port is part of the patient, not something to pick up.
                    var grab = t.GetComponent<XRGrabInteractable>();
                    if (grab != null && scene.name == SceneIds.StageOperate) grab.enabled = false;
                }
                else if (t.name.StartsWith("INST_") && !t.name.Contains("Trocar") && t.GetComponent<LapInstrument>() == null
                         && HasChild(t, "Pivot_Tip") && HasChild(t, "Pivot_Port") && t.GetComponent<XRGrabInteractable>() != null)
                {
                    t.gameObject.AddComponent<LapInstrument>();
                }
                else if (t.name.StartsWith("INST_") && !t.name.Contains("Trocar") && !t.name.Contains("Swab")
                         && t.GetComponent<LapInstrument>() == null && t.GetComponent<ReturnToRest>() == null
                         && t.GetComponent<XRGrabInteractable>() != null)
                {
                    // Hand-held instruments (needle holder) are handed back to the Mayo stand when let go.
                    t.gameObject.AddComponent<ReturnToRest>();
                }
            }
        }

        /// <summary>A place on the Mayo stand for an instrument that started the stage inside the patient.</summary>
        public static void NextRestSlot(Scene scene, out Vector3 position, out Quaternion rotation)
        {
            slotsUsed.TryGetValue(scene, out var used);
            slotsUsed[scene] = used + 1;
            position = FirstRestSlot + Vector3.left * (RestSlotSpacing * used);
            rotation = RestRotation;
        }

        static bool HasChild(Transform root, string childName)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == childName) return true;
            return false;
        }
    }
}
