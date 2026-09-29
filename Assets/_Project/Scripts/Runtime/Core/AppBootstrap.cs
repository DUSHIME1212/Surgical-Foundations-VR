using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurgicalFoundations.Core
{
    /// <summary>
    /// Entry point in 00_Bootstrap. Also makes every content scene playable on its own in the editor:
    /// pressing Play in 13_Stage_Operate pulls in 00_Bootstrap (XR rig, services) and 10_OR_Base automatically.
    /// </summary>
    public class AppBootstrap : MonoBehaviour
    {
        [SerializeField] string firstScene = SceneIds.Lobby;

        static string s_openedScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureBootstrap()
        {
            s_openedScene = null;
            var active = SceneManager.GetActiveScene().name;
            if (active == SceneIds.Bootstrap || !Application.CanStreamedLevelBeLoaded(SceneIds.Bootstrap)) return;
            if (!IsProjectScene(active)) return; // sample/sandbox scenes run untouched
            s_openedScene = active;
            SceneManager.LoadScene(SceneIds.Bootstrap, LoadSceneMode.Additive);
        }

        // 90_ReplayViewer is excluded: it ships as a separate desktop/WebGL build without the XR rig.
        static bool IsProjectScene(string name) =>
            name == SceneIds.Lobby || name == SceneIds.SkillsLab || name == SceneIds.OperatingTheatre || SceneIds.IsStage(name);

        IEnumerator Start()
        {
            var loader = SceneLoader.Instance;
            if (loader == null) yield break;

            if (string.IsNullOrEmpty(s_openedScene))
            {
                loader.Fader?.SetOpaque();
                yield return null;
                if (SceneIds.IsStage(firstScene)) loader.LoadStage(firstScene);
                else loader.LoadFrontend(firstScene);
                yield break;
            }

            // Editor convenience path: a content scene was opened directly.
            loader.Fader?.SetOpaque();
            if (SceneIds.IsStage(s_openedScene) && !SceneManager.GetSceneByName(SceneIds.OperatingTheatre).isLoaded)
            {
                var op = SceneManager.LoadSceneAsync(SceneIds.OperatingTheatre, LoadSceneMode.Additive);
                while (!op.isDone) yield return null;
            }
            var owner = SceneIds.IsStage(s_openedScene) ? SceneIds.OperatingTheatre : s_openedScene;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(owner));
            yield return null;
            loader.PlaceRig(s_openedScene);
            loader.Fader?.FadeIn();
        }
    }
}
