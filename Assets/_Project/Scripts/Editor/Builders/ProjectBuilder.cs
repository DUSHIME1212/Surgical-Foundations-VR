using System.Linq;
using SurgicalFoundations.Core;
using SurgicalFoundations.Placeholders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurgicalFoundations.EditorTools
{
    /// <summary>One-click rebuild of all generated content, lighting bakes and a placeholder report.</summary>
    public static class ProjectBuilder
    {
        [MenuItem(SFPaths.MenuRoot + "Build Everything", priority = -100)]
        public static void BuildEverything()
        {
            LightingRig.ConfigureRendering();
            PlaceholderFactory.BuildAll();
            SoundBankBuilder.Build();
            UIScreensBuilder.BuildAll();
            SceneBuilder.BuildAll();
            Debug.Log("[Surgical Foundations] Build complete. Next: Build ▸ 5 · Bake Lighting (takes a few minutes).");
        }

        static readonly (string folder, string scene)[] Baked =
        {
            ("Frontend", SceneIds.Lobby), ("Frontend", SceneIds.SkillsLab), ("Theatre", SceneIds.OperatingTheatre), ("Tools", SceneIds.ReplayViewer),
        };

        [MenuItem(SFPaths.MenuRoot + "Build/5 · Bake Lighting (all rooms)", priority = 5)]
        public static void BakeAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (var (folder, scene) in Baked) Bake($"{SFPaths.Scenes}/{folder}/{scene}.unity");
        }

        [MenuItem(SFPaths.MenuRoot + "Build/5a · Bake Lighting (theatre only)", priority = 6)]
        public static void BakeTheatre() => Bake($"{SFPaths.Scenes}/Theatre/{SceneIds.OperatingTheatre}.unity");

        public static void Bake(string scenePath)
        {
            var s = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Lightmapping.Bake(); // synchronous: lightmaps, light probes, reflection probes
            EditorSceneManager.SaveScene(s);
            Debug.Log($"[Surgical Foundations] Baked {scenePath}");
        }

        [MenuItem(SFPaths.MenuRoot + "Tools/Placeholder Report", priority = 41)]
        public static void PlaceholderReport()
        {
            var infos = AssetDatabase.FindAssets("t:Prefab", new[] { SFPaths.Prefabs })
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                .Select(p => p.GetComponent<PlaceholderInfo>())
                .Where(i => i != null)
                .OrderBy(i => i.assetId)
                .ToList();
            var open = infos.Where(i => i.isPlaceholder).ToList();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Placeholders still grey-boxed: {open.Count} of {infos.Count}");
            foreach (var i in open) sb.AppendLine($"  {i.assetId,-8} {i.displayName,-32} {i.release,-4} {i.triangleBudget / 1000f,5:0.#}k tris  {i.category}");
            Debug.Log(sb.ToString());
        }
    }
}
