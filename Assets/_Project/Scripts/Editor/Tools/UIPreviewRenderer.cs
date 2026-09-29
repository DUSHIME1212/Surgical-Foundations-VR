using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurgicalFoundations.EditorTools
{
    /// <summary>Renders every UI screen prefab to PNG (Docs/UI Previews) for design review against Design.pdf.</summary>
    public static class UIPreviewRenderer
    {
        [MenuItem(SFPaths.MenuRoot + "Tools/Render UI Previews", priority = 40)]
        public static void RenderAll() => Render(Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/UI Previews")));

        public static void Render(string outDir)
        {
            Directory.CreateDirectory(outDir);
            var scene = EditorSceneManager.NewPreviewScene();
            var folders = new[] { SFPaths.UIScreens, SFPaths.UIHud };
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", folders))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                foreach (var cg in inst.GetComponentsInChildren<CanvasGroup>(true)) cg.alpha = 1f;
                foreach (var rt in inst.GetComponentsInChildren<RectTransform>(true)) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                Canvas.ForceUpdateCanvases();

                var bounds = new Bounds(inst.transform.position, Vector3.zero);
                foreach (var rt in inst.GetComponentsInChildren<RectTransform>())
                {
                    var c = new Vector3[4];
                    rt.GetWorldCorners(c);
                    foreach (var p in c) bounds.Encapsulate(p);
                }

                var camGo = new GameObject("PreviewCamera");
                SceneManager.MoveGameObjectToScene(camGo, scene);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = scene;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.043f, 0.082f, 0.09f);
                cam.fieldOfView = 50f;
                float fit = Mathf.Max(bounds.size.x / 1.6f, bounds.size.y) * 0.62f / Mathf.Tan(25f * Mathf.Deg2Rad);
                cam.transform.position = bounds.center - Vector3.forward * Mathf.Max(0.6f, fit + 0.4f);
                cam.transform.LookAt(bounds.center);

                const int w = 1600, h = 1000;
                var target = new RenderTexture(w, h, 24) { antiAliasing = 4 };
                cam.targetTexture = target;
                cam.Render();
                RenderTexture.active = target;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(outDir, prefab.name + ".png"), tex.EncodeToPNG());
                RenderTexture.active = null;
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(inst);
                target.Release();
                count++;
            }
            EditorSceneManager.ClosePreviewScene(scene);
            Debug.Log($"[Surgical Foundations] Rendered {count} UI previews to {outDir}");
        }
    }
}
