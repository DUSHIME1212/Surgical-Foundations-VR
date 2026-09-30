using System.Linq;
using SurgicalFoundations.Core;
using SurgicalFoundations.Replay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurgicalFoundations.EditorTools
{
    /// <summary>
    /// Connects 90_ReplayViewer's ghost, instruments and camera to a <see cref="ReplayPlayer"/>. Used by the scene
    /// builder, and on its own (menu) to update the existing scene in place without losing its baked lighting.
    /// </summary>
    public static class ReplayViewerWiring
    {
        public static readonly string ScenePath = $"{SFPaths.Scenes}/Tools/{SceneIds.ReplayViewer}.unity";

        [MenuItem(SFPaths.MenuRoot + "Tools/Wire Replay Viewer (keeps lighting)", priority = 120)]
        public static void WireInPlace()
        {
            var existing = SceneManager.GetSceneByPath(ScenePath);
            var wasOpen = existing.IsValid() && existing.isLoaded;
            var scene = wasOpen ? existing : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            Apply(scene);
            EditorSceneManager.SaveScene(scene);
            if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("[Surgical Foundations] Replay viewer wired: " + ScenePath);
        }

        /// <summary>Idempotent: safe to run on a scene that is already wired.</summary>
        public static void Apply(Scene scene)
        {
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
            Transform Named(string name) => all.FirstOrDefault(t => t.name == name);

            var ghost = Named("LearnerGhost");
            var camera = Named("ReplayCamera_Free")?.GetComponent<Camera>();
            if (ghost == null || camera == null)
            {
                Debug.LogError("[ReplayViewerWiring] 90_ReplayViewer is missing LearnerGhost or ReplayCamera_Free; rebuild it with Build ▸ 4 · Scenes.");
                return;
            }

            // The object name is the WebGL bridge target: unityInstance.SendMessage("ReplayPlayer", "LoadSession", json).
            var player = all.Select(t => t.GetComponent<ReplayPlayer>()).FirstOrDefault(p => p != null);
            if (player == null)
            {
                var go = new GameObject("ReplayPlayer");
                SceneManager.MoveGameObjectToScene(go, scene);
                player = go.AddComponent<ReplayPlayer>();
            }
            player.gameObject.name = "ReplayPlayer";

            var instruments = player.transform.Find("Instruments");
            if (instruments == null)
            {
                instruments = new GameObject("Instruments").transform;
                instruments.SetParent(player.transform, false);
            }

            var orbit = camera.GetComponent<ReplayOrbitCamera>();
            if (orbit == null) orbit = camera.gameObject.AddComponent<ReplayOrbitCamera>();
            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { SFPaths.Prefabs + "/Instruments" })
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(p => p != null && p.name.StartsWith("INST_"))
                .Cast<Object>()
                .ToArray();

            var ghostParts = ghost.GetComponentsInChildren<Transform>(true);
            Ser.Set(player, "head", ghostParts.FirstOrDefault(t => t.name == "Head"));
            Ser.Set(player, "leftHand", ghostParts.FirstOrDefault(t => t.name == "Hand_L"));
            Ser.Set(player, "rightHand", ghostParts.FirstOrDefault(t => t.name == "Hand_R"));
            Ser.SetArray(player, "instrumentPrefabs", prefabs);
            Ser.Set(player, "instrumentRoot", instruments);
            Ser.Set(player, "viewCamera", camera);
            Ser.Set(player, "orbit", orbit);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
