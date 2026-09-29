using System;
using System.Collections.Generic;
using System.IO;
using SurgicalFoundations.Audio;
using UnityEditor;
using UnityEngine;

namespace SurgicalFoundations.EditorTools
{
    /// <summary>
    /// Builds Data/Audio/SoundBank.asset by matching every SoundId to a clip file of the same name
    /// (SFX_UI_Click.wav → UI_Click). Keeps hand-tuned volumes on re-run; only fills in new entries and clips.
    /// </summary>
    public static class SoundBankBuilder
    {
        static readonly Dictionary<SoundId, string> Subtitles = new Dictionary<SoundId, string>
        {
            { SoundId.VO_Lobby_Welcome, "Welcome back. Choose a mode and a posture, then start the scenario." },
            { SoundId.VO_Calibrate_Height, "Stand or sit naturally, and look ahead." },
            { SoundId.VO_Calibrate_Table, "Grab the table edge and move it until your elbows sit at about 90°." },
            { SoundId.VO_Tutorial_SqueezeTrigger, "Squeeze the trigger to close the grasper around the peg." },
            { SoundId.VO_Prep_HandsAboveElbows, "Keep your hands above your elbows so water runs away from your fingertips." },
            { SoundId.VO_Prep_SterilityBroken, "Sterility broken. Step back from the sterile field." },
            { SoundId.VO_Access_MarkRightPort, "Mark the right working port. Keep it clear of the epigastric vessels." },
            { SoundId.VO_Access_AngleShallow, "Angle too shallow. Raise the trocar toward the target zone before pushing further." },
            { SoundId.VO_Operate_DriftLeft, "Bring your left instrument back into view before you move it." },
            { SoundId.VO_Close_SwabMissing, "Closure blocked. One swab is unaccounted for." },
            { SoundId.VO_Close_WatchPortSite, "Watch the port site on the monitor as you withdraw." },
            { SoundId.VO_Summary_Completed, "Closure completed. Here is your session summary." },
        };

        [MenuItem(SFPaths.MenuRoot + "Build/2 · Sound Bank", priority = 2)]
        public static SoundBank Build()
        {
            var clips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { SFPaths.Audio }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var key = Path.GetFileNameWithoutExtension(path);
                if (key.StartsWith("SFX_")) key = key.Substring(4);
                if (key.EndsWith("_Loop")) key = key.Substring(0, key.Length - 5);
                clips[key] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(SFPaths.SoundBank));
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(SFPaths.SoundBank);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<SoundBank>();
                AssetDatabase.CreateAsset(bank, SFPaths.SoundBank);
            }

            int missing = 0;
            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
            {
                if (id == SoundId.None) continue;
                var def = bank.sounds.Find(s => s.id == id);
                bool isNew = def == null;
                if (isNew) { def = new SoundDefinition { id = id }; bank.sounds.Add(def); }

                if (clips.TryGetValue(id.ToString(), out var clip)) def.clips = new[] { clip };
                else { missing++; Debug.LogWarning($"[SoundBank] No clip for {id}"); }

                if (isNew) ApplyDefaults(def);
                if (Subtitles.TryGetValue(id, out var sub)) def.subtitle = sub;
            }

            bank.sounds.Sort((a, b) => ((int)a.id).CompareTo((int)b.id));
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Surgical Foundations] Sound bank: {bank.sounds.Count} sounds, {missing} without clips.");
            return bank;
        }

        static void ApplyDefaults(SoundDefinition d)
        {
            var name = d.id.ToString();
            if (name.StartsWith("UI_")) { d.category = AudioCategory.UI; d.volume = 0.7f; d.pitchVariance = 0.02f; }
            else if (name.StartsWith("FB_")) { d.category = AudioCategory.Feedback; d.volume = 0.8f; }
            else if (name.StartsWith("VO_")) { d.category = AudioCategory.Voice; d.volume = 1f; }
            else if (name.StartsWith("AMB_"))
            {
                d.category = AudioCategory.Ambience; d.loop = true;
                bool roomTone = name.Contains("RoomTone") || name.Contains("Lobby");
                d.spatialBlend = roomTone ? 0f : 1f;
                d.volume = roomTone ? 0.5f : 0.6f;
                d.minDistance = 0.6f; d.maxDistance = 8f;
            }
            else
            {
                d.category = AudioCategory.SFX; d.spatialBlend = 1f; d.volume = 0.85f; d.pitchVariance = 0.06f;
                d.minDistance = 0.3f; d.maxDistance = 10f;
                if (name.EndsWith("TableMotor")) d.loop = true;
            }
            if (d.id == SoundId.AMB_TapWater) { d.volume = 0.8f; d.minDistance = 0.3f; d.maxDistance = 5f; }
            if (d.id == SoundId.AMB_SurgicalLight_Hum) { d.volume = 0.25f; d.maxDistance = 4f; }
        }
    }
}
