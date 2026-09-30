using System.IO;
using SurgicalFoundations.Backend;
using UnityEditor;
using UnityEngine;

namespace SurgicalFoundations.EditorTools
{
    /// <summary>Menu: Surgical Foundations ▸ Backend. Connects the headset build to the API.</summary>
    public static class BackendSetup
    {
        const string SettingsPath = SFPaths.Root + "/Resources/" + BackendSettings.ResourceName + ".asset";

        [MenuItem(SFPaths.MenuRoot + "Backend/Create or Select Settings", priority = 50)]
        public static void CreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<BackendSettings>(SettingsPath);
            if (settings == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                settings = ScriptableObject.CreateInstance<BackendSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Surgical Foundations] Created " + SettingsPath);
            }
            Selection.activeObject = settings;
        }

        /// <summary>
        /// Android blocks plain HTTP. Development builds may use http://localhost (via `adb reverse`) or a LAN dev server;
        /// release builds stay HTTPS-only.
        /// </summary>
        [MenuItem(SFPaths.MenuRoot + "Backend/Allow HTTP in Development Builds", priority = 51)]
        public static void AllowHttpInDevelopment()
        {
            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
            Debug.Log("[Surgical Foundations] Player setting 'Allow downloads over HTTP' = Allowed in development builds.");
        }

        /// <summary>Forget the signed-in account and pending uploads on this machine (editor testing).</summary>
        [MenuItem(SFPaths.MenuRoot + "Backend/Reset Local Sign-in and Upload Queue", priority = 52)]
        public static void ResetLocalState()
        {
            if (!EditorUtility.DisplayDialog("Reset local backend state",
                    "Sign out this editor and delete queued uploads that haven't synced? This can't be undone.", "Reset", "Cancel")) return;
            PlayerPrefs.DeleteKey("sf.account.v1");
            PlayerPrefs.Save();
            foreach (var dir in new[] { "sync", "learners" })
            {
                var path = Path.Combine(Application.persistentDataPath, dir);
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            var cache = Path.Combine(Application.persistentDataPath, "scoring-config.json");
            if (File.Exists(cache)) File.Delete(cache);
            Debug.Log("[Surgical Foundations] Local sign-in, upload queue and caches cleared.");
        }
    }
}
