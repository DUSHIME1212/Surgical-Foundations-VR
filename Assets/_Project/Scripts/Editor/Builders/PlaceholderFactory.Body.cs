using SurgicalFoundations.Audio;
using SurgicalFoundations.Placeholders;
using UnityEditor;
using UnityEngine;

namespace SurgicalFoundations.EditorTools
{
    /// <summary>PPE ×4, anatomy ×6, training props ×4, characters ×3 — and the BuildAll entry point.</summary>
    public static partial class PlaceholderFactory
    {
        [MenuItem(SFPaths.MenuRoot + "Build/1 · Placeholder Models", priority = 1)]
        public static void BuildAll()
        {
            MaterialLibrary.ClearCache();
            // No StartAssetEditing batching: materials created on the fly must be imported before prefabs reference them.
            // Environment
            OperatingTheatre(); ScrubAlcove(); Lobby(); SkillsLab();
            // Equipment
            ORTable(); SurgicalLights(); LapTower(); AnaesthesiaMachine(); ScrubSink(); BackTable(); MayoStand(); InstrumentTray(); KickBucket();

            // Instruments
            Trocar("INST_Trocar12", "INST-01", "Trocar 12 mm", 0.012f, 0.04f);
            Trocar("INST_Trocar5", "INST-02", "Trocar 5 mm", 0.0055f, 0.03f);
            Laparoscope();
            LapInstrument("INST_AtraumaticGrasper", "INST-04", "Atraumatic grasper", JawStyle.Grasper, "Separate jaw meshes, handle ratchet.");
            LapInstrument("INST_MarylandDissector", "INST-05", "Maryland dissector", JawStyle.Maryland, "Curved jaws, separate meshes.");
            LapInstrument("INST_LapScissors", "INST-06", "Laparoscopic scissors", JawStyle.Scissors, "Separate blades.");
            LapInstrument("INST_ClipApplier", "INST-07", "Clip applier", JawStyle.ClipApplier, "Jaws + loaded clip.");
            SurgicalClip(); Scalpel(); NeedleHolder(); Swab();
            // PPE
            SurgeonHands(); Gown(); GlovesPacket(); DrapeSet();
            // Anatomy
            PatientBody(); AbdominalWall(); AbdominalCavity(); DissectionTissue(); ClipCutStructure(); EpigastricVessels();
            // Training props
            PegBoard(); TransferObjects(); PortSiteMarkers(); ControllerDiagram();
            // Characters
            Mannequin("CHR_ScrubNurse", "CHR-01", "Scrub nurse / assistant", AssetRelease.MVP, 20000, "Gown_Blue", "Scrubs_Teal", true,
                "Rigged humanoid; helps with gowning; holds the camera in R2 (US-OP-03). ~12 keyframed clips.");
            Mannequin("CHR_Anaesthetist", "CHR-02", "Anaesthetist", AssetRelease.R2, 15000, "Scrubs_Green", "Scrubs_Green", false,
                "Background, idle loop only.");
            CameraAssistantHands();

            AssetDatabase.SaveAssets();
            Debug.Log("[Surgical Foundations] 41 placeholder prefabs built in " + SFPaths.Prefabs);
        }

        // ───────────────────────────── PPE ─────────────────────────────

        static void Hand(PB b, string name, float x, float side, string mat)
        {
            var h = b.Group(name, new Vector3(x, 0, 0));
            b.Box("Palm", Vector3.zero, new Vector3(0.085f, 0.028f, 0.095f), mat, default, h);
            for (int i = 0; i < 4; i++)
                b.Cap($"Finger_{i}", new Vector3(-0.03f + i * 0.02f, 0, 0.085f), 0.018f, 0.08f - Mathf.Abs(i - 1.5f) * 0.01f, mat, PB.AlongZ, h);
            b.Cap("Thumb", new Vector3(side * 0.052f, -0.004f, 0.015f), 0.02f, 0.065f, mat, new Vector3(90, side * 45f, 0), h);
            b.Cyl("Wrist", new Vector3(0, 0, -0.075f), 0.058f, 0.06f, mat, PB.AlongZ, h);
        }

        static void SurgeonHands()
        {
            var b = new PB("PPE_SurgeonHands");
            Hand(b, "Hand_L", -0.12f, 1f, "Glove_Latex");
            Hand(b, "Hand_R", 0.12f, -1f, "Glove_Latex");
            b.Info("PPE-01", "Surgeon hands (L + R)", "PPE", AssetRelease.MVP, 10000,
                "Rigged for hand tracking (XR Hands skeleton); bare, gloved and contaminated material states (M_Skin, M_Glove_Latex, M_Glove_Contaminated).");
            b.Save(Folder("PPE"));
        }

        static void Gown()
        {
            var b = new PB("PPE_SurgicalGown");
            b.Cyl("Stand_Base", new Vector3(0, 0.01f, 0), 0.45f, 0.02f, "Stainless_Satin");
            b.Cyl("Stand_Pole", new Vector3(0, 0.92f, 0), 0.025f, 1.8f, "Stainless_Satin");
            b.Cyl("Hanger", new Vector3(0, 1.78f, 0), 0.015f, 0.46f, "Stainless_Satin", PB.AlongX);
            var gown = b.Group("Gown", Vector3.zero);
            b.CapE("Body", new Vector3(0, 1.12f, 0.05f), new Vector3(0.52f, 1.3f, 0.26f), "Gown_Blue", default, gown);
            b.Cap("Sleeve_L", new Vector3(-0.32f, 1.35f, 0.05f), 0.13f, 0.62f, "Gown_Blue", new Vector3(0, 0, -12), gown);
            b.Cap("Sleeve_R", new Vector3(0.32f, 1.35f, 0.05f), 0.13f, 0.62f, "Gown_Blue", new Vector3(0, 0, 12), gown);
            b.Cyl("Cuff_L", new Vector3(-0.38f, 1.06f, 0.05f), 0.1f, 0.06f, "Polymer_White", new Vector3(0, 0, -12), gown);
            b.Cyl("Cuff_R", new Vector3(0.38f, 1.06f, 0.05f), 0.1f, 0.06f, "Polymer_White", new Vector3(0, 0, 12), gown);
            b.Collider(null, new Vector3(0, 0.95f, 0.05f), new Vector3(0.9f, 1.9f, 0.4f));
            b.Info("PPE-02", "Surgical gown", "PPE", AssetRelease.MVP, 8000, "Cloth for donning; worn version for the rest of the session.");
            b.Save(Folder("PPE"));
        }

        static void GlovesPacket()
        {
            var b = new PB("PPE_GlovesPacket");
            b.Box("Packet", Vector3.zero, new Vector3(0.28f, 0.005f, 0.2f), "Packet_Paper");
            b.Box("Packet_FlapL", new Vector3(-0.07f, 0.004f, 0), new Vector3(0.14f, 0.003f, 0.2f), "Cap_Blue");
            b.Box("Packet_FlapR", new Vector3(0.07f, 0.004f, 0), new Vector3(0.14f, 0.003f, 0.2f), "Cap_Blue");
            for (int s = -1; s <= 1; s += 2)
            {
                var g = b.Group(s < 0 ? "Glove_L" : "Glove_R", new Vector3(s * 0.065f, 0.009f, -0.01f));
                b.Box("Palm", Vector3.zero, new Vector3(0.07f, 0.006f, 0.08f), "Glove_Latex", default, g);
                for (int i = 0; i < 4; i++)
                    b.Box($"Finger_{i}", new Vector3(-0.024f + i * 0.016f, 0, 0.07f), new Vector3(0.013f, 0.005f, 0.06f), "Glove_Latex", default, g);
                b.Box("Cuff", new Vector3(0, 0, -0.07f), new Vector3(0.07f, 0.006f, 0.06f), "Glove_Latex", default, g);
            }
            b.Info("PPE-03", "Gloves + glove packet", "PPE", AssetRelease.MVP, 3000, "Packet opens (flaps); glove skinned to hand rig.");
            b.Grabbable(0.05f, Vector3.zero, Vector3.zero, grabSound: SoundId.ENV_PacketTear);
            b.Save(Folder("PPE"));
        }

        /// <summary>Fenestrated laparotomy drape. Origin = centre of the fenestration (place over the umbilicus).</summary>
        static void DrapeSet()
        {
            var b = new PB("PPE_DrapeSet");
            const float L = 2.6f, Wd = 1.7f, th = 0.004f, holeX = 0.3f, holeZ = 0.25f;
            b.Box("Drape_Head", new Vector3(-(L / 2 + holeX / 2) / 2 - 0.0f, 0, 0), new Vector3(L / 2 - holeX / 2, th, Wd), "Drape_Teal");
            b.Box("Drape_Foot", new Vector3((L / 2 + holeX / 2) / 2, 0, 0), new Vector3(L / 2 - holeX / 2, th, Wd), "Drape_Teal");
            b.Box("Drape_Left", new Vector3(0, 0, (Wd / 2 + holeZ / 2) / 2), new Vector3(holeX, th, Wd / 2 - holeZ / 2), "Drape_Teal");
            b.Box("Drape_Right", new Vector3(0, 0, -(Wd / 2 + holeZ / 2) / 2), new Vector3(holeX, th, Wd / 2 - holeZ / 2), "Drape_Teal");
            b.Box("Drop_North", new Vector3(0, -0.28f, Wd / 2), new Vector3(L, 0.56f, th), "Drape_Teal");
            b.Box("Drop_South", new Vector3(0, -0.28f, -Wd / 2), new Vector3(L, 0.56f, th), "Drape_Teal");
            b.Box("Adhesive_Frame", new Vector3(0, 0.001f, 0), new Vector3(holeX + 0.03f, th, holeZ + 0.03f), "Polymer_White");
            b.Info("PPE-04", "Patient drape set", "PPE", AssetRelease.MVP, 6000, "Fenestrated drape, cloth-sim placement, then static.");
            b.Save(Folder("PPE"));
        }

        // ───────────────────────────── Anatomy ─────────────────────────────

        /// <summary>Supine patient. Origin = table-top surface centre; head at −X, umbilicus pivot marks the camera port.</summary>
        static void PatientBody()
        {
            var b = new PB("ANA_PatientBody");
            b.CapE("Torso", new Vector3(-0.35f, 0.12f, 0), new Vector3(0.24f, 0.72f, 0.36f), "Skin", new Vector3(0, 0, 90));
            b.CapE("Pelvis", new Vector3(0.1f, 0.1f, 0), new Vector3(0.2f, 0.38f, 0.34f), "Skin", new Vector3(0, 0, 90));
            b.Cyl("Neck", new Vector3(-0.7f, 0.08f, 0), 0.1f, 0.1f, "Skin", PB.AlongX);
            b.Sph("Head", new Vector3(-0.83f, 0.1f, 0), new Vector3(0.21f, 0.18f, 0.17f), "Skin");
            b.Sph("Cap", new Vector3(-0.86f, 0.12f, 0), new Vector3(0.18f, 0.16f, 0.18f), "Cap_Blue");
            for (int s = -1; s <= 1; s += 2)
            {
                string n = s < 0 ? "R" : "L";
                b.Cap($"UpperArm_{n}", new Vector3(-0.55f, 0.07f, s * 0.36f), 0.085f, 0.3f, "Skin", PB.AlongZ);
                b.Cap($"Forearm_{n}", new Vector3(-0.55f, 0.06f, s * 0.64f), 0.075f, 0.28f, "Skin", PB.AlongZ);
                b.Box($"Hand_{n}", new Vector3(-0.55f, 0.05f, s * 0.82f), new Vector3(0.08f, 0.03f, 0.1f), "Skin");
                b.Cap($"Thigh_{n}", new Vector3(0.47f, 0.08f, s * 0.1f), 0.15f, 0.46f, "Skin", PB.AlongX);
                b.Cap($"Shin_{n}", new Vector3(0.88f, 0.065f, s * 0.1f), 0.11f, 0.42f, "Skin", PB.AlongX);
                b.Box($"Foot_{n}", new Vector3(1.1f, 0.1f, s * 0.1f), new Vector3(0.06f, 0.11f, 0.09f), "Skin");
            }
            b.CylE("PrepArea", new Vector3(-0.08f, 0.238f, 0), new Vector3(0.34f, 0.004f, 0.28f), "Skin_Prepped");
            b.Pivot("Umbilicus", new Vector3(-0.02f, 0.24f, 0));
            b.Collider(null, new Vector3(0.1f, 0.12f, 0), new Vector3(2.0f, 0.24f, 0.4f));
            b.Info("ANA-01", "Patient body", "Anatomy", AssetRelease.MVP, 20000, "Supine, mostly draped; low-poly exterior. Head at −X.");
            b.Save(Folder("Anatomy"));
        }

        /// <summary>Layered entry block. Origin = skin surface; layers stack downward with their own trigger volumes (FR-08).</summary>
        static void AbdominalWall()
        {
            var b = new PB("ANA_AbdominalWallLayers");
            (string name, float thick, string mat)[] layers = { ("Skin", 0.003f, "Skin_Prepped"), ("Fat", 0.025f, "Fat"), ("Fascia", 0.003f, "Fascia"), ("Peritoneum", 0.002f, "Peritoneum") };
            float y = 0f;
            var trig = b.Pivot("LayerTriggers", Vector3.zero);
            foreach (var (name, thick, mat) in layers)
            {
                float c = y - thick / 2;
                b.Box($"Layer_{name}", new Vector3(0, c, 0), new Vector3(0.3f, thick, 0.3f), mat);
                b.Collider($"Layer_{name}", new Vector3(0, c, 0), new Vector3(0.3f, Mathf.Max(thick, 0.002f), 0.3f), true, trig);
                y -= thick;
            }
            b.Info("ANA-02", "Abdominal wall layers", "Anatomy", AssetRelease.MVP, 8000, "Skin, fat, fascia and peritoneum as separate layers for entry resistance and depth (FR-08).");
            b.Save(Folder("Anatomy"));
        }

        /// <summary>Insufflated cavity seen only through the laparoscope. Origin = cavity floor centre. Double-sided wet interior.</summary>
        static void AbdominalCavity()
        {
            var b = new PB("ANA_AbdominalCavity");
            b.Sph("CavityWall", new Vector3(0, 0.07f, 0), new Vector3(0.44f, 0.24f, 0.36f), "Tissue_Wet");
            b.Sph("Liver", new Vector3(-0.13f, 0.1f, 0.06f), new Vector3(0.22f, 0.05f, 0.16f), "Liver", new Vector3(0, 20, 12));
            b.Sph("Gallbladder", new Vector3(-0.07f, 0.08f, 0.1f), new Vector3(0.05f, 0.028f, 0.03f), "Duct", new Vector3(0, 30, 0));
            var bowel = b.Group("BowelLoops", Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f;
                var p = Quaternion.Euler(0, a, 0) * new Vector3(0.09f + (i % 2) * 0.03f, 0, 0);
                b.Cap($"Loop_{i}", new Vector3(p.x + 0.04f, -0.02f + (i % 3) * 0.008f, p.z), 0.035f, 0.13f, "Bowel", new Vector3(90, a + 90, 0), bowel);
            }
            b.Pivot("ScopeViewTarget", new Vector3(0, 0.03f, 0));
            b.Pivot("TaskArea", new Vector3(0.04f, 0.0f, -0.02f));
            b.Info("ANA-03", "Abdominal cavity", "Anatomy", AssetRelease.MVP, 40000, "Insufflated interior, liver edge, bowel loops; seen only through the laparoscope.");
            b.Save(Folder("Anatomy"));
        }

        static void DissectionTissue()
        {
            var b = new PB("ANA_DissectionTissue");
            b.Sph("Tissue", Vector3.zero, new Vector3(0.08f, 0.02f, 0.06f), "Tissue_Pink");
            b.Sph("Membrane", new Vector3(0, 0.006f, 0), new Vector3(0.05f, 0.01f, 0.035f), "Peritoneum");
            b.Collider("CollisionProxy", Vector3.zero, new Vector3(0.07f, 0.016f, 0.05f));
            b.Info("ANA-04", "Dissection tissue", "Anatomy", AssetRelease.MVP, 10000, "Obi Softbody particle mesh plus a lower-res collision proxy; deforms in MVP, tears in R2.");
            b.Save(Folder("Anatomy"));
        }

        static void ClipCutStructure()
        {
            var b = new PB("ANA_ClipCutStructure");
            b.Sph("FatPad", new Vector3(0, -0.006f, 0), new Vector3(0.09f, 0.02f, 0.05f), "Fat");
            b.Cyl("Duct", new Vector3(0, 0.004f, 0), 0.006f, 0.09f, "Duct", PB.AlongX);
            b.Pivot("ClipPoint_1", new Vector3(-0.018f, 0.004f, 0));
            b.Pivot("ClipPoint_2", new Vector3(-0.006f, 0.004f, 0));
            b.Pivot("CutPoint", new Vector3(0.008f, 0.004f, 0));
            var guides = b.Group("GuidedMarkers", Vector3.zero);
            foreach (var x in new[] { -0.018f, -0.006f })
                b.Cyl($"ClipMark_{x:0.000}", new Vector3(x, 0.004f, 0), 0.0085f, 0.0015f, "Guided_Mint", PB.AlongX, guides);
            b.Cyl("CutMark", new Vector3(0.008f, 0.004f, 0), 0.0085f, 0.0015f, "Guided_Amber", PB.AlongX, guides);
            b.Info("ANA-05", "Clip-and-cut structure", "Anatomy", AssetRelease.MVP, 3000, "Duct/vessel with clip and cut points.");
            b.Save(Folder("Anatomy"));
        }

        static void EpigastricVessels()
        {
            var b = new PB("ANA_EpigastricVessels");
            for (int i = 0; i < 3; i++)
            {
                float z = -0.1f + i * 0.1f;
                b.Cyl($"Artery_{i}", new Vector3(0.004f * i, 0, z), 0.004f, 0.105f, "Vessel_Artery", new Vector3(90, 8 * (i - 1), 0));
                b.Cyl($"Vein_{i}", new Vector3(0.008f + 0.004f * i, 0, z), 0.005f, 0.105f, "Vessel_Vein", new Vector3(90, 8 * (i - 1), 0));
            }
            b.Pivot("BleedSocket", new Vector3(0.004f, 0.003f, 0));
            b.Info("ANA-06", "Epigastric vessels", "Anatomy", AssetRelease.R2, 3000, "Injury state with bleed-VFX socket (US-ACC-05).");
            b.Save(Folder("Anatomy"));
        }

        // ───────────────────────────── Training props ─────────────────────────────

        /// <summary>Peg board with 12 pegs and 6 rigid-body rings (square collider ring so they slide over pegs).</summary>
        static void PegBoard()
        {
            var b = new PB("PROP_PegBoard");
            b.Box("Board", new Vector3(0, 0.01f, 0), new Vector3(0.22f, 0.02f, 0.12f), "PegBoard");
            var pegCols = b.Pivot("PegColliders", Vector3.zero);
            for (int row = 0; row < 2; row++)
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector3(-0.08f + i * 0.032f, 0.0375f, row == 0 ? -0.03f : 0.03f);
                b.Cyl($"Peg_{row}_{i}", p, 0.006f, 0.035f, "Peg");
                b.Collider($"Peg_{row}_{i}", p, new Vector3(0.006f, 0.035f, 0.006f), false, pegCols);
            }
            b.Collider(null, new Vector3(0, 0.01f, 0), new Vector3(0.22f, 0.02f, 0.12f));

            var rings = b.Pivot("Rings", Vector3.zero);
            for (int i = 0; i < 6; i++)
            {
                var ring = new GameObject($"Ring_{i + 1}");
                ring.transform.SetParent(rings, false);
                ring.transform.localPosition = new Vector3(-0.08f + i * 0.032f, 0.024f, -0.03f);
                var vis = new GameObject("Visual").transform;
                vis.SetParent(ring.transform, false);
                b.Cyl("Ring", Vector3.zero, 0.02f, 0.005f, "Ring_Orange", default, vis);
                b.Cyl("Hole", new Vector3(0, 0.0001f, 0), 0.009f, 0.0052f, "Peg", default, vis);
                foreach (var (c, s) in new[] { (new Vector3(0.0075f, 0, 0), new Vector3(0.005f, 0.005f, 0.02f)), (new Vector3(-0.0075f, 0, 0), new Vector3(0.005f, 0.005f, 0.02f)),
                                               (new Vector3(0, 0, 0.0075f), new Vector3(0.02f, 0.005f, 0.005f)), (new Vector3(0, 0, -0.0075f), new Vector3(0.02f, 0.005f, 0.005f)) })
                {
                    var bc = ring.AddComponent<BoxCollider>();
                    bc.center = c; bc.size = s;
                }
                var rb = ring.AddComponent<Rigidbody>();
                rb.mass = 0.005f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                var grab = ring.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                grab.throwOnDetach = false;
                Ser.Set(ring.AddComponent<ImpactSound>(), "sound", (int)SoundId.ENV_RingDrop);
            }
            b.Info("PROP-01", "Peg board + 6 rings", "TrainingProp", AssetRelease.MVP, 3000, "Original design; rings as rigid bodies.");
            b.Save(Folder("TrainingProps"));
        }

        static void TransferObjects()
        {
            var b = new PB("PROP_TransferObjects");
            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject(i < 3 ? $"Bead_{i + 1}" : $"Block_{i - 2}");
                go.transform.SetParent(b.Root.transform, false);
                go.transform.localPosition = new Vector3(-0.05f + i * 0.02f, 0.006f, 0);
                var vis = new GameObject("Visual").transform;
                vis.SetParent(go.transform, false);
                if (i < 3)
                {
                    b.Sph("Bead", Vector3.zero, 0.012f, i % 2 == 0 ? "Bead_Mint" : "Bead_Coral", vis);
                    go.AddComponent<SphereCollider>().radius = 0.006f;
                }
                else
                {
                    b.Box("Block", Vector3.zero, Vector3.one * 0.012f, i % 2 == 0 ? "Bead_Mint" : "Bead_Coral", default, vis);
                    go.AddComponent<BoxCollider>().size = Vector3.one * 0.012f;
                }
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.004f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                go.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().throwOnDetach = false;
                Ser.Set(go.AddComponent<ImpactSound>(), "sound", (int)SoundId.ENV_RingDrop);
            }
            Object.DestroyImmediate(b.Visual.gameObject);
            b.Info("PROP-02", "Transfer objects", "TrainingProp", AssetRelease.MVP, 1000, "Beads/blocks for grasp-and-transfer.");
            b.Save(Folder("TrainingProps"));
        }

        /// <summary>Guided-mode dashed target rings for the three ports (screen 08). Origin = umbilicus.</summary>
        static void PortSiteMarkers()
        {
            var b = new PB("PROP_PortSiteMarkers");
            (string n, Vector3 p)[] sites = { ("Camera_Umbilical", Vector3.zero), ("LeftWorking", new Vector3(-0.09f, 0, 0.1f)), ("RightWorking", new Vector3(0.09f, 0, 0.1f)) };
            foreach (var (n, p) in sites)
            {
                var g = b.Group($"Marker_{n}", p);
                b.Quad("DashedRing", new Vector3(0, 0.003f, 0), new Vector2(0.06f, 0.06f), "Guided_DashedRing", new Vector3(90, 0, 0), g);
                b.Cyl("Dot", new Vector3(0, 0.002f, 0), 0.01f, 0.001f, "Guided_Mint", default, g);
                b.Pivot($"Target_{n}", p);
            }
            b.Info("PROP-03", "Port-site target markers", "TrainingProp", AssetRelease.MVP, 500, "Decals and dashed rings, Guided mode only.");
            b.Save(Folder("TrainingProps"));
        }

        /// <summary>Neutral, unbranded controller for the tutorial panel. Callout colours match screen 04 (trigger amber, grip mint).</summary>
        static void ControllerDiagram()
        {
            var b = new PB("PROP_ControllerDiagram");
            b.Cap("Handle", new Vector3(0, -0.035f, -0.005f), 0.036f, 0.13f, "Controller_Neutral", new Vector3(-20, 0, 0));
            b.CylE("TrackingRing", new Vector3(0, 0.035f, -0.015f), new Vector3(0.09f, 0.012f, 0.09f), "Controller_Neutral", new Vector3(60, 0, 0));
            b.CylE("FacePlate", new Vector3(0, 0.03f, 0.005f), new Vector3(0.05f, 0.01f, 0.05f), "Polymer_Dark", new Vector3(-20, 0, 0));
            b.Cyl("Thumbstick", new Vector3(0, 0.043f, 0.008f), 0.012f, 0.012f, "Polymer_Grey", new Vector3(-20, 0, 0));
            b.Cyl("Button_Menu", new Vector3(-0.014f, 0.036f, -0.006f), 0.007f, 0.004f, "Polymer_Grey", new Vector3(-20, 0, 0));
            b.Box("Trigger", new Vector3(0, 0.005f, 0.035f), new Vector3(0.014f, 0.022f, 0.012f), "Indicator_Amber", new Vector3(-30, 0, 0));
            b.Box("Grip", new Vector3(-0.02f, -0.035f, 0.0f), new Vector3(0.006f, 0.05f, 0.02f), "Indicator_Mint", new Vector3(-20, 0, 0));
            b.Info("PROP-04", "Controller diagram model", "TrainingProp", AssetRelease.MVP, 2000, "Neutral, unbranded controller for the tutorial panel.");
            b.Save(Folder("TrainingProps"));
        }

        // ───────────────────────────── Characters ─────────────────────────────

        /// <summary>Primitive mannequin (1.68 m) with humanoid-named transforms so the real rig can map onto it. Origin at the feet, facing +Z.</summary>
        static void Mannequin(string prefabName, string id, string display, AssetRelease rel, int tris, string topMat, string legMat, bool gloved, string notes)
        {
            var b = new PB(prefabName);
            var hips = b.Group("Hips", new Vector3(0, 0.92f, 0));
            b.CapE("Pelvis", Vector3.zero, new Vector3(0.22f, 0.34f, 0.2f), topMat, new Vector3(0, 0, 90), hips);
            for (int s = -1; s <= 1; s += 2)
            {
                string n = s < 0 ? "L" : "R";
                b.Cap($"UpperLeg_{n}", new Vector3(s * 0.1f, -0.24f, 0), 0.14f, 0.46f, legMat, default, hips);
                b.Cap($"LowerLeg_{n}", new Vector3(s * 0.1f, -0.63f, 0), 0.11f, 0.44f, legMat, default, hips);
                b.Box($"Foot_{n}", new Vector3(s * 0.1f, -0.885f, 0.05f), new Vector3(0.1f, 0.07f, 0.26f), "Rubber_Black", default, hips);
            }
            var spine = b.Group("Spine", new Vector3(0, 0.3f, 0), default, hips);
            b.CapE("Chest", new Vector3(0, 0, 0), new Vector3(0.4f, 0.6f, 0.24f), topMat, default, spine);
            b.Cyl("Neck", new Vector3(0, 0.3f, 0), 0.1f, 0.08f, "Skin", default, spine);
            var head = b.Group("Head", new Vector3(0, 0.42f, 0), default, spine);
            b.Sph("Skull", Vector3.zero, new Vector3(0.18f, 0.23f, 0.2f), "Skin", default, head);
            b.Sph("SurgicalCap", new Vector3(0, 0.05f, -0.01f), new Vector3(0.2f, 0.13f, 0.21f), "Cap_Blue", default, head);
            b.Box("Mask", new Vector3(0, -0.035f, 0.095f), new Vector3(0.15f, 0.09f, 0.03f), "Mask_Blue", default, head);
            for (int s = -1; s <= 1; s += 2)
            {
                string n = s < 0 ? "L" : "R";
                var ua = b.Group($"UpperArm_{n}", new Vector3(s * 0.23f, 0.2f, 0), new Vector3(15, 0, s * -8), spine);
                b.Cap("Upper", new Vector3(0, -0.14f, 0), 0.09f, 0.3f, topMat, default, ua);
                var fa = b.Group($"LowerArm_{n}", new Vector3(0, -0.29f, 0), new Vector3(-80, 0, 0), ua);
                b.Cap("Lower", new Vector3(0, -0.13f, 0), 0.08f, 0.28f, topMat, default, fa);
                var hand = b.Group($"Hand_{n}", new Vector3(0, -0.29f, 0), default, fa);
                b.Box("Palm", new Vector3(0, -0.04f, 0), new Vector3(0.08f, 0.1f, 0.03f), gloved ? "Glove_Latex" : "Skin", default, hand);
            }
            var cap = b.Root.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0, 0.84f, 0);
            cap.height = 1.68f;
            cap.radius = 0.24f;
            b.Info(id, display, "Character", rel, tris, notes);
            b.Save(Folder("Characters"));
        }

        static void CameraAssistantHands()
        {
            var b = new PB("CHR_CameraAssistantHands");
            for (int s = -1; s <= 1; s += 2)
            {
                var arm = b.Group(s < 0 ? "Arm_L" : "Arm_R", new Vector3(s * 0.08f, 0, -0.25f));
                b.Cap("Forearm", new Vector3(0, 0, 0.12f), 0.075f, 0.28f, "Gown_Blue", PB.AlongZ, arm);
                b.Box("Hand", new Vector3(0, 0, 0.29f), new Vector3(0.08f, 0.03f, 0.1f), "Glove_Latex", default, arm);
            }
            b.Pivot("ScopeGrip", new Vector3(0, 0, 0.05f));
            b.Info("CHR-03", "Camera assistant hands", "Character", AssetRelease.R2, 6000, "Hands on the laparoscope for voice camera control.");
            b.Save(Folder("Characters"));
        }
    }
}
