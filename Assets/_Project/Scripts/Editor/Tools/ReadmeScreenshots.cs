using System;
using System.IO;
using System.Linq;
using SurgicalFoundations.Core;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SurgicalFoundations.EditorTools
{
    /// <summary>
    /// Renders the README screenshots (Docs/Images) from fixed viewpoints. Bake lighting first (Build ▸ 5).
    /// The rooms are captured in play mode: scenes load through SceneLoader exactly like the game (stages on top of
    /// 10_OR_Base), and URP only fills its reflection-probe data during real frames, so steel renders black if you
    /// render these scenes from an editor script. The model line-up needs no scene and is rendered in edit mode.
    /// Progress survives the play-mode domain reload through SessionState.
    /// </summary>
    [InitializeOnLoad]
    public static class ReadmeScreenshots
    {
        const int Width = 1600, Height = 900;
        const string KeyOut = "SF.Readme.OutDir", KeyShot = "SF.Readme.Shot", KeyPhase = "SF.Readme.Phase",
            KeyStart = "SF.Readme.PrevStartScene";
        const int SettleFrames = 45; // after the fade-in: let probes, the scope feed and UI animations settle
        const int PanelFrames = 40;  // after a shot's setup: UIPanels fade in when activated

        class Shot
        {
            public string file;
            public Action<SceneLoader> load;
            public Vector3 eye, target;
            public float fov = 70f;
            public Action setup;
        }

        static readonly Shot[] PlayShots =
        {
            new Shot
            {
                file = "access-stage.png", load = l => l.LoadStage(SceneIds.StageAccess),
                eye = new Vector3(0.25f, 1.64f, -1.0f), target = new Vector3(-0.4f, 1.28f, 0.55f), fov = 80f,
                setup = () => ShowStep("UI_08_PortSites", "Step_TrocarEntry"),
            },
            new Shot
            {
                file = "theatre-overview.png", load = l => l.LoadTheatreOnly(),
                eye = new Vector3(3.15f, 2.35f, -3.15f), target = new Vector3(-0.9f, 0.75f, 0.5f), fov = 82f,
            },
            new Shot
            {
                file = "prep-scrub.png", load = l => l.LoadStage(SceneIds.StagePrep),
                eye = new Vector3(-1.45f, 1.72f, -4.2f), target = new Vector3(-2.15f, 1.05f, -5.35f), fov = 72f,
            },
            new Shot
            {
                file = "skills-lab.png", load = l => l.LoadFrontend(SceneIds.SkillsLab),
                eye = new Vector3(-2.45f, 1.9f, -2.45f), target = new Vector3(1.4f, 0.7f, 0.9f), fov = 78f,
            },
            new Shot
            {
                file = "lobby.png", load = l => l.LoadFrontend(SceneIds.Lobby),
                eye = new Vector3(3.8f, 1.8f, -1.4f), target = new Vector3(-0.5f, 0.9f, -1.4f), fov = 70f,
            },
        };

        static ReadmeScreenshots()
        {
            EditorApplication.update += Tick;
        }

        [MenuItem(SFPaths.MenuRoot + "Tools/Render README Screenshots", priority = 42)]
        public static void RenderAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            RenderTo(Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Images")));
        }

        /// <summary>No prompts: open scenes must already be saved. Finishes asynchronously (enters and leaves play mode).</summary>
        public static void RenderTo(string outDir)
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Surgical Foundations] Stop play mode before rendering README screenshots."); return; }
            Directory.CreateDirectory(outDir);
            var reopen = SceneManager.GetActiveScene().path;

            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            ModelLineup();
            Render(Path.Combine(outDir, "imported-models.png"), new Vector3(4.4f, 3.1f, -7.4f), new Vector3(4.4f, 0.55f, 0.1f), 38f,
                1800, 800, new Color(0.2f, 0.23f, 0.25f));
            if (!string.IsNullOrEmpty(reopen)) EditorSceneManager.OpenScene(reopen, OpenSceneMode.Single);

            SessionState.SetString(KeyOut, outDir);
            SessionState.SetInt(KeyShot, 0);
            SessionState.SetInt(KeyPhase, 0);
            SessionState.SetString(KeyStart, EditorSceneManager.playModeStartScene != null ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) : "");
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>($"{SFPaths.Scenes}/Core/{SceneIds.Bootstrap}.unity");
            EditorApplication.EnterPlaymode();
        }

        public static bool IsRunning => !string.IsNullOrEmpty(SessionState.GetString(KeyOut, ""));

        // Phase 0: request the load · 1..Settle: frames after SceneLoader reports idle · Settle: shot setup ·
        // then PanelFrames more for UI fades · render.
        static void Tick()
        {
            if (!IsRunning || !EditorApplication.isPlaying) return;
            var loader = SceneLoader.Instance;
            // Let AppBootstrap's own first load (the lobby) start and finish before taking over.
            if (loader == null || loader.IsBusy || Time.frameCount < 30) return;

            int i = SessionState.GetInt(KeyShot, 0), phase = SessionState.GetInt(KeyPhase, 0);
            if (i >= PlayShots.Length) { Finish(); return; }
            var s = PlayShots[i];
            if (phase == 0)
            {
                s.load(loader);
                SessionState.SetInt(KeyPhase, 1);
                return;
            }
            if (phase == SettleFrames) s.setup?.Invoke();
            if (phase < SettleFrames + PanelFrames) { SessionState.SetInt(KeyPhase, phase + 1); return; }

            var rig = Object.FindAnyObjectByType<XROrigin>();
            var hidden = rig != null ? rig.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray() : new Renderer[0];
            foreach (var r in hidden) r.enabled = false; // controllers/hands of the idle rig would float in the shot
            Render(Path.Combine(SessionState.GetString(KeyOut, ""), s.file), s.eye, s.target, s.fov, Width, Height, null);
            foreach (var r in hidden) r.enabled = true;
            SessionState.SetInt(KeyShot, i + 1);
            SessionState.SetInt(KeyPhase, 0);
        }

        static void Finish()
        {
            var outDir = SessionState.GetString(KeyOut, "");
            var start = SessionState.GetString(KeyStart, "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(start) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(start);
            SessionState.EraseString(KeyOut);
            SessionState.EraseInt(KeyShot);
            SessionState.EraseInt(KeyPhase);
            SessionState.EraseString(KeyStart);
            EditorApplication.ExitPlaymode();
            Debug.Log($"[Surgical Foundations] README screenshots rendered to {outDir}");
        }

        static void Render(string file, Vector3 eye, Vector3 target, float fov, int width, int height, Color? background)
        {
            var go = new GameObject("ReadmeCamera");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = eye;
            cam.transform.LookAt(target);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 60f;
            if (background.HasValue)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = background.Value;
            }
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.allowXRRendering = false;

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(file, tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        // ───────────── shot setups ─────────────

        /// <summary>Switches the stage's StepSequence from its first step to another one.</summary>
        static void ShowStep(string hide, string show)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.name == hide) t.gameObject.SetActive(false);
                if (t.name == show) t.gameObject.SetActive(true);
            }
        }

        /// <summary>Imported and textured models on a neutral floor: big pieces behind, characters and small kit in front.</summary>
        static void ModelLineup()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.localScale = new Vector3(3f, 1f, 2f);
            floor.transform.position = new Vector3(4.6f, 0f, 2f);
            var fm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            fm.SetColor("_BaseColor", new Color(0.42f, 0.45f, 0.47f));
            fm.SetFloat("_Smoothness", 0.3f);
            floor.GetComponent<Renderer>().sharedMaterial = fm;
            var sun = Object.FindAnyObjectByType<Light>();
            // Key light from the camera side (camera looks along +Z), so the fronts are lit.
            if (sun != null) { sun.transform.rotation = Quaternion.Euler(40f, 150f, 0f); sun.intensity = 1.25f; sun.shadows = LightShadows.Soft; }
            RenderSettings.ambientIntensity = 1.1f;

            void Put(string prefab, float x, float z, float yaw, float y = 0f)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{SFPaths.Prefabs}/{prefab}.prefab");
                if (p == null) return;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(p);
                go.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0f, yaw, 0f));
            }
            // Prefab fronts face +Z, so 180° turns them to the camera (−Z).
            Put("Equipment/EQ_ORTable", 1.3f, 1.2f, 0f);
            Put("Anatomy/ANA_PatientBody", 1.3f, 1.2f, 0f, 0.92f);
            Put("Equipment/EQ_ScrubSink", 3.9f, 1.5f, 180f);
            Put("Equipment/EQ_HospitalBed", 5.9f, 1.2f, 180f);
            Put("Equipment/EQ_OxygenCylinder", 7.4f, 1.3f, 180f);
            Put("Equipment/EQ_HeartLungMachine", 8.8f, 1.3f, 200f);
            Put("Characters/CHR_ScrubNurse", 0.1f, -1.2f, 180f);
            Put("Characters/CHR_Anaesthetist", 0.9f, -1.2f, 180f);
            Put("Equipment/EQ_MedicalCart", 2.2f, -1.2f, 180f);
            Put("Equipment/EQ_IVStand", 3.4f, -1.3f, 180f);
            Put("Equipment/EQ_PatientMonitor", 4.5f, -1.3f, 180f);
            Put("Equipment/EQ_BedsideCabinet", 5.5f, -1.3f, 180f);
            Put("Environment/FURN_VisitorChair", 6.4f, -1.3f, 200f);
            Put("Equipment/EQ_SupplyCabinet", 7.6f, -1.1f, 180f);
        }
    }
}
