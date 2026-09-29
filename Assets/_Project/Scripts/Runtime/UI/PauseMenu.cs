using SurgicalFoundations.Audio;
using SurgicalFoundations.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurgicalFoundations.UI
{
    /// <summary>
    /// Menu button → world dims, timer freezes, pause card appears in front of the learner (US-ACS-03, screen 13).
    /// Lives in 00_Bootstrap so it works in every scene.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] UIPanel panel;
        [SerializeField] GameObject dimmer;
        [SerializeField] float distance = 1.1f;

        InputAction menuAction;

        void Awake()
        {
            // The XRI sample action map has no Menu action, so bind the system-agnostic paths directly.
            menuAction = new InputAction("Menu", InputActionType.Button);
            menuAction.AddBinding("<XRController>{LeftHand}/menuButton");
            menuAction.AddBinding("<XRController>{LeftHand}/menu");
            menuAction.AddBinding("<Keyboard>/escape"); // desk testing without a headset
        }

        void OnEnable()
        {
            menuAction.performed += OnMenu;
            menuAction.Enable();
        }

        void OnDisable()
        {
            menuAction.performed -= OnMenu;
            menuAction.Disable();
        }

        void Start()
        {
            // Start, not OnEnable: SessionManager.Awake may not have run yet when this object is enabled.
            if (SessionManager.Instance != null) SessionManager.Instance.PauseChanged += Apply;
            panel.gameObject.SetActive(false);
            if (dimmer != null) dimmer.SetActive(false);
        }

        void OnDestroy()
        {
            if (SessionManager.Instance != null) SessionManager.Instance.PauseChanged -= Apply;
        }

        void OnMenu(InputAction.CallbackContext _)
        {
            var s = SessionManager.Instance;
            if (s == null || !s.IsRunning || (SceneLoader.Instance != null && SceneLoader.Instance.IsBusy)) return;
            s.TogglePause();
        }

        void Apply(bool paused)
        {
            AudioManager.Instance?.Play(paused ? SoundId.FB_PauseIn : SoundId.FB_PauseOut);
            if (paused)
            {
                PlaceInFront();
                panel.Show();
            }
            else panel.Hide();
            if (dimmer != null) dimmer.SetActive(paused);
        }

        void PlaceInFront()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            panel.transform.parent.position = cam.transform.position + fwd * distance - Vector3.up * 0.1f;
            panel.transform.parent.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            if (dimmer != null) dimmer.transform.position = cam.transform.position;
        }

        public void Resume() => SessionManager.Instance?.SetPaused(false);

        public void Recalibrate()
        {
            SessionManager.Instance?.SetPaused(false);
            SceneLoader.Instance?.LoadFrontend(SceneIds.SkillsLab);
        }

        public void RestartScenario()
        {
            SessionManager.Instance?.SetPaused(false);
            Scenario.ScenarioDirector.Instance?.RetryScenario();
        }

        public void EndSession()
        {
            SessionManager.Instance?.SetPaused(false);
            SessionManager.Instance?.EndSession();
            SceneLoader.Instance?.LoadFrontend(SceneIds.Lobby);
        }
    }
}
