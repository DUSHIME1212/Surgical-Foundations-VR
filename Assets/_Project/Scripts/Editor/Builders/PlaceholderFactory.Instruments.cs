using SurgicalFoundations.Audio;
using SurgicalFoundations.Placeholders;
using UnityEngine;

namespace SurgicalFoundations.EditorTools
{
    /// <summary>
    /// Instruments (×11). Convention: long axis +Z, tip at +Z, "Attach" at the handle, "Pivot_Tip" at the working end and
    /// "Pivot_Port" where the shaft meets the port (FR-10 fulcrum; updated at runtime). Jaws are separate hinged meshes.
    /// </summary>
    public static partial class PlaceholderFactory
    {
        const float ShaftLength = 0.33f;
        const float ShaftStart = 0.03f;

        enum JawStyle { Grasper, Maryland, Scissors, ClipApplier }

        static void LapInstrument(string prefabName, string id, string display, JawStyle style, string notes)
        {
            var b = new PB(prefabName);
            float shaftD = style == JawStyle.ClipApplier ? 0.01f : 0.005f;

            // Pistol-grip handle with thumb ring and finger ring
            var handle = b.Group("Handle", Vector3.zero);
            b.Box("Body", new Vector3(0, -0.005f, -0.01f), new Vector3(0.024f, 0.035f, 0.07f), "Instrument_Handle", default, handle);
            b.Box("Grip", new Vector3(0, -0.06f, -0.03f), new Vector3(0.022f, 0.09f, 0.028f), "Instrument_Handle", new Vector3(-15, 0, 0), handle);
            b.Cyl("FingerRing", new Vector3(0, -0.105f, -0.045f), 0.036f, 0.01f, "Instrument_Handle", PB.AlongX, handle);
            b.Box("ThumbLever", new Vector3(0, -0.05f, -0.065f), new Vector3(0.012f, 0.08f, 0.012f), "Instrument_Handle", new Vector3(10, 0, 0), handle);
            b.Cyl("ThumbRing", new Vector3(0, -0.095f, -0.075f), 0.034f, 0.01f, "Instrument_Handle", PB.AlongX, handle);
            b.Box("Ratchet", new Vector3(0, -0.03f, 0.012f), new Vector3(0.004f, 0.02f, 0.004f), "Instrument_Steel", default, handle);
            b.Cyl("RotationKnob", new Vector3(0, 0, 0.022f), 0.02f, 0.016f, "Instrument_Steel", PB.AlongZ, handle);

            b.Cyl("Shaft", new Vector3(0, 0, ShaftStart + ShaftLength / 2), shaftD, ShaftLength, "Instrument_Insulation", PB.AlongZ);

            float tipZ = ShaftStart + ShaftLength;
            var hinge = b.Group("JawHinge", new Vector3(0, 0, tipZ));
            var upper = b.Group("Jaw_Upper", Vector3.zero, default, hinge);
            var lower = b.Group("Jaw_Lower", Vector3.zero, default, hinge);
            float jawLen = style == JawStyle.Scissors ? 0.018f : 0.02f;
            string jawMat = "Instrument_Steel";
            switch (style)
            {
                case JawStyle.Maryland:
                    // Curved jaws: two segments bending toward +X
                    b.Box("Seg1", new Vector3(0, 0.0011f, 0.006f), new Vector3(0.0042f, 0.0022f, 0.012f), jawMat, default, upper);
                    b.Box("Seg2", new Vector3(0.0018f, 0.0011f, 0.0155f), new Vector3(0.0036f, 0.0022f, 0.009f), jawMat, new Vector3(0, 25, 0), upper);
                    b.Box("Seg1", new Vector3(0, -0.0011f, 0.006f), new Vector3(0.0042f, 0.0022f, 0.012f), jawMat, default, lower);
                    b.Box("Seg2", new Vector3(0.0018f, -0.0011f, 0.0155f), new Vector3(0.0036f, 0.0022f, 0.009f), jawMat, new Vector3(0, 25, 0), lower);
                    break;
                case JawStyle.Scissors:
                    b.Box("Blade", new Vector3(0, 0.0008f, jawLen / 2), new Vector3(0.0045f, 0.0012f, jawLen), jawMat, new Vector3(0, 0, 8), upper);
                    b.Box("Blade", new Vector3(0, -0.0008f, jawLen / 2), new Vector3(0.0045f, 0.0012f, jawLen), jawMat, new Vector3(0, 0, -8), lower);
                    break;
                case JawStyle.ClipApplier:
                    b.Box("Jaw", new Vector3(0, 0.0022f, 0.011f), new Vector3(0.008f, 0.003f, 0.022f), jawMat, default, upper);
                    b.Box("Jaw", new Vector3(0, -0.0022f, 0.011f), new Vector3(0.008f, 0.003f, 0.022f), jawMat, default, lower);
                    var clip = b.Group("LoadedClip", new Vector3(0, 0, 0.012f), default, hinge);
                    b.Box("Leg_A", new Vector3(0, 0.0012f, 0), new Vector3(0.0012f, 0.0012f, 0.009f), "Instrument_Titanium", default, clip);
                    b.Box("Leg_B", new Vector3(0, -0.0012f, 0), new Vector3(0.0012f, 0.0012f, 0.009f), "Instrument_Titanium", default, clip);
                    break;
                default: // atraumatic grasper: fenestrated flat jaws
                    b.Box("Jaw", new Vector3(0, 0.0011f, jawLen / 2), new Vector3(0.0045f, 0.0022f, jawLen), jawMat, default, upper);
                    b.Box("Jaw", new Vector3(0, -0.0011f, jawLen / 2), new Vector3(0.0045f, 0.0022f, jawLen), jawMat, default, lower);
                    break;
            }

            b.Pivot("Pivot_Tip", new Vector3(0, 0, tipZ + jawLen));
            b.Pivot("Pivot_Port", new Vector3(0, 0, ShaftStart + ShaftLength * 0.55f));
            b.Info(id, display, "Instrument", AssetRelease.MVP, 4000, notes);
            b.Grabbable(0.25f, new Vector3(0, -0.055f, -0.03f), Vector3.zero, jaws: true);
            b.Save(Folder("Instruments"));
        }

        static void Trocar(string prefabName, string id, string display, float cannulaD, float housingD)
        {
            var b = new PB(prefabName);
            b.Cyl("Housing", Vector3.zero, housingD, 0.045f, "Trocar_Polymer", PB.AlongZ);
            b.Cyl("ValveCap", new Vector3(0, 0, -0.027f), housingD * 0.75f, 0.01f, "Rubber_Black", PB.AlongZ);
            b.Cyl("SidePort", new Vector3(housingD * 0.6f, 0, 0), 0.008f, 0.03f, "Trocar_Polymer", PB.AlongX);
            b.Box("Stopcock", new Vector3(housingD * 0.6f + 0.016f, 0, 0), new Vector3(0.006f, 0.02f, 0.006f), "Indicator_Mint");
            b.Cyl("Cannula", new Vector3(0, 0, 0.0725f), cannulaD, 0.1f, "Trocar_Polymer", PB.AlongZ);
            var obt = b.Group("Obturator", Vector3.zero);
            b.Sph("Knob", new Vector3(0, 0, -0.045f), housingD * 0.8f, "Polymer_White", obt);
            b.Cyl("Shaft", new Vector3(0, 0, 0.035f), cannulaD * 0.85f, 0.13f, "Polymer_White", PB.AlongZ, obt);
            b.Cap("Tip", new Vector3(0, 0, 0.1f), cannulaD * 0.85f, 0.03f, "Polymer_White", PB.AlongZ, obt);
            b.Pivot("Pivot_Tip", new Vector3(0, 0, 0.13f));
            b.Pivot("Pivot_Port", new Vector3(0, 0, 0.09f));
            b.Info(id, display, "Instrument", AssetRelease.MVP, 3000, "Cannula + obturator as 2 parts; valve cap. Obturator withdraws on port placement.");
            b.Grabbable(0.08f, new Vector3(0, 0, -0.03f), Vector3.zero, grabSound: SoundId.INST_TrocarValve);
            b.Save(Folder("Instruments"));
        }

        static void Laparoscope()
        {
            var b = new PB("INST_Laparoscope30");
            b.Cyl("CameraHead", new Vector3(0, 0, -0.07f), 0.045f, 0.08f, "Polymer_Dark", PB.AlongZ);
            b.Cyl("CameraCable", new Vector3(0, 0, -0.26f), 0.007f, 0.3f, "Rubber_Black", PB.AlongZ);
            b.Box("CameraButtons", new Vector3(0, 0.024f, -0.07f), new Vector3(0.02f, 0.006f, 0.04f), "Polymer_Grey");
            b.Cyl("Coupler", new Vector3(0, 0, -0.015f), 0.03f, 0.03f, "Instrument_Steel", PB.AlongZ);
            b.Cyl("Eyepiece", new Vector3(0, 0, 0.008f), 0.022f, 0.016f, "Instrument_Steel", PB.AlongZ);
            b.Cyl("LightPost", new Vector3(0, 0.018f, 0.025f), 0.008f, 0.03f, "Instrument_Steel");
            b.Cyl("LightCable", new Vector3(0, 0.1f, 0.025f), 0.006f, 0.14f, "Rubber_Black");
            b.Cyl("Shaft", new Vector3(0, 0, 0.018f + ShaftLength / 2), 0.01f, ShaftLength, "Instrument_Steel", PB.AlongZ);
            float tip = 0.018f + ShaftLength;
            b.Cyl("Lens", new Vector3(0, 0, tip), 0.008f, 0.001f, "Screen_Off", new Vector3(120, 0, 0));
            b.Pivot("Pivot_Tip", new Vector3(0, 0, tip));
            b.Pivot("Pivot_Port", new Vector3(0, 0, 0.2f));
            b.Pivot("CameraSocket", new Vector3(0, 0, tip), new Vector3(30, 0, 0));
            b.Info("INST-03", "Laparoscope 30°", "Instrument", AssetRelease.MVP, 4000, "Scope, camera head, light cable; camera socket at tip, 30° offset.");
            b.Grabbable(0.45f, new Vector3(0, 0, -0.07f), Vector3.zero);
            b.Save(Folder("Instruments"));
        }

        static void SurgicalClip()
        {
            var b = new PB("INST_SurgicalClip");
            b.Box("Crown", new Vector3(0, 0, 0), new Vector3(0.0012f, 0.004f, 0.0012f), "Instrument_Titanium");
            b.Box("Leg_A", new Vector3(0, 0.0015f, 0.0045f), new Vector3(0.0012f, 0.0012f, 0.009f), "Instrument_Titanium", new Vector3(-6, 0, 0));
            b.Box("Leg_B", new Vector3(0, -0.0015f, 0.0045f), new Vector3(0.0012f, 0.0012f, 0.009f), "Instrument_Titanium", new Vector3(6, 0, 0));
            b.Info("INST-08", "Surgical clip", "Instrument", AssetRelease.MVP, 200, "Tiny; GPU-instanced.");
            b.Save(Folder("Instruments"));
        }

        static void Scalpel()
        {
            var b = new PB("INST_Scalpel");
            b.Box("Handle", new Vector3(0, 0, 0.065f), new Vector3(0.009f, 0.003f, 0.13f), "Instrument_Steel");
            b.Box("Grip", new Vector3(0, 0, 0.05f), new Vector3(0.0092f, 0.0032f, 0.05f), "Stainless_Satin");
            b.Box("Blade", new Vector3(0, 0.001f, 0.147f), new Vector3(0.0005f, 0.007f, 0.034f), "Instrument_Steel", new Vector3(0, 0, 90));
            b.Pivot("Pivot_Tip", new Vector3(0, -0.002f, 0.165f));
            b.Info("INST-09", "Scalpel", "Instrument", AssetRelease.MVP, 1000, "Skin incision at port sites.");
            b.Grabbable(0.03f, new Vector3(0, 0, 0.05f), Vector3.zero, impact: SoundId.INST_PlaceOnTray);
            b.Save(Folder("Instruments"));
        }

        static void NeedleHolder()
        {
            var b = new PB("INST_NeedleHolder");
            b.Cyl("Ring_L", new Vector3(-0.018f, 0, -0.01f), 0.026f, 0.006f, "Instrument_Steel");
            b.Cyl("Ring_R", new Vector3(0.018f, 0, -0.01f), 0.026f, 0.006f, "Instrument_Steel");
            b.Box("Shank_L", new Vector3(-0.007f, 0, 0.05f), new Vector3(0.004f, 0.005f, 0.11f), "Instrument_Steel", new Vector3(0, 10, 0));
            b.Box("Shank_R", new Vector3(0.007f, 0, 0.05f), new Vector3(0.004f, 0.005f, 0.11f), "Instrument_Steel", new Vector3(0, -10, 0));
            b.Box("BoxLock", new Vector3(0, 0, 0.105f), new Vector3(0.009f, 0.007f, 0.012f), "Instrument_Steel");
            var jaws = b.Group("JawHinge", new Vector3(0, 0, 0.11f));
            var up = b.Group("Jaw_Upper", Vector3.zero, default, jaws);
            var lo = b.Group("Jaw_Lower", Vector3.zero, default, jaws);
            b.Box("Jaw", new Vector3(0, 0.001f, 0.014f), new Vector3(0.005f, 0.002f, 0.028f), "Instrument_Titanium", default, up);
            b.Box("Jaw", new Vector3(0, -0.001f, 0.014f), new Vector3(0.005f, 0.002f, 0.028f), "Instrument_Titanium", default, lo);
            var needle = b.Group("Needle", new Vector3(0, 0, 0.135f));
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.Lerp(-80f, 80f, i / 5f) * Mathf.Deg2Rad;
                b.Cyl($"Seg_{i}", new Vector3(Mathf.Sin(a) * 0.009f, -Mathf.Cos(a) * 0.009f + 0.009f, 0), 0.0011f, 0.0055f, "Instrument_Steel", new Vector3(0, 0, a * Mathf.Rad2Deg + 90), needle);
            }
            b.Cyl("Suture", new Vector3(0.009f, -0.14f, 0.135f), 0.0008f, 0.28f, "Suture_Violet");
            b.Info("INST-10", "Needle holder + suture", "Instrument", AssetRelease.MVP, 3000, "Port-site closure; suture as spline mesh.");
            b.Grabbable(0.06f, new Vector3(0, 0, -0.005f), Vector3.zero, jaws: true);
            b.Save(Folder("Instruments"));
        }

        static void Swab()
        {
            var b = new PB("INST_Swab");
            b.Box("Gauze", Vector3.zero, new Vector3(0.1f, 0.006f, 0.1f), "Swab_White");
            b.Box("XRayStripe", new Vector3(0.03f, 0, 0), new Vector3(0.008f, 0.0064f, 0.1f), "Swab_Stripe");
            b.Info("INST-11", "Swab / gauze", "Instrument", AssetRelease.MVP, 1000, "Cloth-sim version plus a folded static version; hideable (US-CLS-05). This is the folded static version.");
            b.Grabbable(0.01f, Vector3.zero, Vector3.zero, grabSound: SoundId.ENV_DrapeRustle, impact: SoundId.ENV_SwabDrop);
            b.Save(Folder("Instruments"));
        }
    }
}
