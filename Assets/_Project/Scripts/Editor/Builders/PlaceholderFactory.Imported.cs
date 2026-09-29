using System.Collections.Generic;
using System.Linq;
using SurgicalFoundations.Placeholders;
using UnityEditor;
using UnityEngine;

namespace SurgicalFoundations.EditorTools
{
    /// <summary>
    /// Real models from Assets/_Project/Imports placed under a prefab's "Visual" (same contract as the placeholders),
    /// fitted to real-world size, turned to face +Z and, for rigged characters, posed out of the T-pose.
    /// </summary>
    public static partial class PlaceholderFactory
    {
        public enum Fit { Height, Width, None }

        /// <summary>Instantiates <paramref name="modelPath"/> under b.Visual. Returns the model instance or null if missing.</summary>
        static GameObject TryModel(PB b, string modelPath, Fit fit, float size, bool faceForward = false)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) return null;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.transform.SetParent(b.Visual, false);
            ImportedAssets.Remap(inst, ImportedAssets.MaterialMap(modelPath));
            foreach (var c in inst.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var a in inst.GetComponentsInChildren<Animator>(true)) a.enabled = false; // no clips yet; keep the authored pose

            if (faceForward) FaceForward(inst);

            var bounds = WorldBounds(inst);
            float scale = fit switch
            {
                Fit.Height => size / Mathf.Max(bounds.size.y, 1e-4f),
                Fit.Width => size / Mathf.Max(bounds.size.x, 1e-4f),
                _ => 1f
            };
            inst.transform.localScale *= scale;
            bounds = WorldBounds(inst);
            // Centre on X/Z and stand on the floor (y = 0 of the prefab).
            inst.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) + b.Root.transform.position;
            return inst;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs.Length > 0 ? rs[0].bounds : new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static Transform Bone(Transform root, string side, bool fore) =>
            root.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.EndsWith(side + (fore ? "ForeArm" : "Arm")));

        /// <summary>Uses the T-pose arm line to find the character's facing and turns it to +Z.</summary>
        static void FaceForward(GameObject inst)
        {
            var l = Bone(inst.transform, "Left", false);
            var r = Bone(inst.transform, "Right", false);
            if (l == null || r == null) return;
            var across = Vector3.ProjectOnPlane(r.position - l.position, Vector3.up);
            if (across.sqrMagnitude < 1e-6f) return;
            var forward = Vector3.Cross(across.normalized, Vector3.up);
            inst.transform.rotation = Quaternion.FromToRotation(forward, Vector3.forward) * inst.transform.rotation;
        }

        static void Aim(Transform bone, Vector3 worldDir)
        {
            if (bone == null || bone.childCount == 0) return;
            var child = bone.GetChild(0);
            var current = (child.position - bone.position).normalized;
            bone.rotation = Quaternion.FromToRotation(current, worldDir.normalized) * bone.rotation;
        }

        /// <summary>Re-poses a T-posed humanoid: upper arms then forearms aimed along world directions (character faces +Z).</summary>
        static void PoseArms(GameObject inst, Vector3 upperL, Vector3 upperR, Vector3 foreL, Vector3 foreR)
        {
            var t = inst.transform;
            Aim(Bone(t, "Left", false), upperL);
            Aim(Bone(t, "Right", false), upperR);
            Aim(Bone(t, "Left", true), foreL);
            Aim(Bone(t, "Right", true), foreR);
        }

        // Character facing +Z: its left side is −X.
        static readonly Vector3[] SterileHands = { new Vector3(-0.22f, -1f, 0.35f), new Vector3(0.22f, -1f, 0.35f), new Vector3(0.4f, 0.35f, 1f), new Vector3(-0.4f, 0.35f, 1f) };
        static readonly Vector3[] RelaxedArms = { new Vector3(-0.16f, -1f, 0.04f), new Vector3(0.16f, -1f, 0.04f), new Vector3(-0.04f, -1f, 0.32f), new Vector3(0.04f, -1f, 0.32f) };

        /// <summary>Character from an imported rig; falls back to false so the caller builds the primitive mannequin.</summary>
        static bool ImportedCharacter(PB b, string model, float height, Vector3[] pose)
        {
            var inst = TryModel(b, model, Fit.Height, height, faceForward: true);
            if (inst == null) return false;
            PoseArms(inst, pose[0], pose[1], pose[2], pose[3]);
            return true;
        }

        // ───────────── new props from the imports (not in the asset list; background dressing) ─────────────

        static void ImportedProps()
        {
            if (ImportedAssets.Exists(ImportedAssets.HeartLung))
            {
                var b = new PB("EQ_HeartLungMachine");
                TryModel(b, ImportedAssets.HeartLung, Fit.None, 0f);
                b.FitColliderFromRenderers();
                b.Info("EQ-X1", "Heart-lung machine", "Equipment", AssetRelease.MVP, 17000, "Imported model (background dressing). Not in the asset list; 17k tris.").isPlaceholder = false;
                b.MakeStatic(false);
                b.Save(Folder("Equipment"));
            }
            if (ImportedAssets.Exists(ImportedAssets.Microscope))
            {
                var b = new PB("EQ_Microscope");
                TryModel(b, ImportedAssets.Microscope, Fit.None, 0f);
                b.FitColliderFromRenderers();
                b.Info("EQ-X2", "Microscope", "Equipment", AssetRelease.MVP, 11600, "Imported model (skills-lab dressing).").isPlaceholder = false;
                b.Save(Folder("Equipment"));
            }
            if (ImportedAssets.Exists(ImportedAssets.Monitor))
            {
                var b = new PB("EQ_PatientMonitor");
                b.Cyl("Stand_Base", new Vector3(0, 0.02f, 0), 0.5f, 0.04f, "Polymer_Grey");
                b.Cyl("Stand_Pole", new Vector3(0, 0.7f, 0), 0.04f, 1.3f, "Stainless_Satin");
                var head = new GameObject("MonitorHead").transform;
                head.SetParent(b.Visual, false);
                var tmp = new PB("tmp");
                var inst = TryModel(tmp, ImportedAssets.Monitor, Fit.Width, 0.5f);
                if (inst != null)
                {
                    inst.transform.SetParent(head, true);
                    head.localPosition = new Vector3(0, 1.3f, 0);
                }
                Object.DestroyImmediate(tmp.Root);
                var screen = b.Quad("Vitals_Screen", new Vector3(0, 1.3f + WorldBounds(head.gameObject).extents.y, -0.03f), new Vector2(0.44f, 0.27f), "Screen_Vitals");
                screen.gameObject.SetActive(inst != null);
                b.FitColliderFromRenderers();
                b.Info("EQ-X3", "Patient monitor on stand", "Equipment", AssetRelease.MVP, 400, "Imported monitor model + procedural vitals screen (SF/UI/Vitals Monitor).").isPlaceholder = false;
                b.Save(Folder("Equipment"));
            }
            if (ImportedAssets.Exists(ImportedAssets.SurgeryTools))
            {
                var b = new PB("PROP_OpenSurgerySet");
                TryModel(b, ImportedAssets.SurgeryTools, Fit.None, 0f);
                b.FitColliderFromRenderers();
                b.Info("PROP-X1", "Open surgery instrument set", "TrainingProp", AssetRelease.MVP, 164590,
                    "Imported. ⚠ 164k tris — too heavy for the theatre on Quest (400k scene budget). Used as skills-lab display only; decimate before wider use.").isPlaceholder = false;
                b.Save(Folder("TrainingProps"));
            }
        }
    }
}
