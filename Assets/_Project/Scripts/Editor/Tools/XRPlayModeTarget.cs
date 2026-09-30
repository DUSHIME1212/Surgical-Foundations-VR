using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;

namespace SurgicalFoundations.EditorTools
{
    /// <summary>
    /// Chooses what drives the XR rig when you press Play in the editor:
    ///  • Simulator: the XRI "XR Interaction Simulator" (mouse + keyboard, simulated controllers and XR Hands);
    ///    OpenXR is not started, so a Link/SteamVR runtime can't take over.
    ///  • Headset: OpenXR starts on the PC (Quest Link / Air Link) and the simulator stays out of the way.
    /// Only the Standalone (PC/editor) XR settings change; the Android build is untouched.
    /// </summary>
    public static class XRPlayModeTarget
    {
        const string SimulatorMenu = SFPaths.MenuRoot + "Play Mode/XR Simulator (no headset)";
        const string HeadsetMenu = SFPaths.MenuRoot + "Play Mode/Headset via Link";
        public const string SimulatorPrefab = "Assets/Samples/XR Interaction Toolkit/3.6.0/XR Interaction Simulator/XR Interaction Simulator.prefab";

        [MenuItem(SimulatorMenu, priority = 20)]
        public static void UseSimulator() => Set(true);

        [MenuItem(HeadsetMenu, priority = 21)]
        public static void UseHeadset() => Set(false);

        [MenuItem(SimulatorMenu, true)]
        static bool ValidateSimulator()
        {
            Menu.SetChecked(SimulatorMenu, SimulatorOn);
            Menu.SetChecked(HeadsetMenu, !SimulatorOn);
            return !EditorApplication.isPlaying;
        }

        [MenuItem(HeadsetMenu, true)]
        static bool ValidateHeadset() => ValidateSimulator();

        /// <summary>XRI's XRDeviceSimulatorSettings (Project Settings ▸ XR Plug-in Management ▸ XR Interaction Toolkit); the type is internal.</summary>
        static Object SimulatorSettings()
        {
            var guid = AssetDatabase.FindAssets("t:XRDeviceSimulatorSettings");
            return guid.Length > 0 ? AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid[0])) : null;
        }

        static bool SimulatorOn
        {
            get
            {
                var s = SimulatorSettings();
                return s != null && new SerializedObject(s).FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue;
            }
        }

        static void Set(bool simulator)
        {
            var sim = SimulatorSettings();
            if (sim == null)
            {
                Debug.LogError("[Surgical Foundations] XRDeviceSimulatorSettings not found. Open Project Settings ▸ XR Plug-in Management ▸ XR Interaction Toolkit once to create it.");
                return;
            }
            var so = new SerializedObject(sim);
            so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = simulator;
            so.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue = true;
            so.FindProperty("m_UseClassic").boolValue = false;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefab);
            if (simulator && prefab == null)
                Debug.LogWarning("[Surgical Foundations] XR Interaction Simulator sample not found. Import it from Package Manager ▸ XR Interaction Toolkit ▸ Samples.");
            if (prefab != null) so.FindProperty("m_SimulatorPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sim);

            var pc = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (pc != null)
            {
                pc.InitManagerOnStart = !simulator;
                EditorUtility.SetDirty(pc);
            }
            AssetDatabase.SaveAssets();
            Debug.Log(simulator
                ? "[Surgical Foundations] Play Mode: XR Interaction Simulator. Press Play; the simulator's on-screen panel lists the mouse and keyboard controls."
                : "[Surgical Foundations] Play Mode: headset via Quest Link / Air Link (OpenXR on PC).");
        }
    }
}
