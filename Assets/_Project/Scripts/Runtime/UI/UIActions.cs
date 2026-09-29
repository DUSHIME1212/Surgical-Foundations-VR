using SurgicalFoundations.Core;
using SurgicalFoundations.Scenario;
using UnityEngine;

namespace SurgicalFoundations.UI
{
    /// <summary>
    /// Scene-local bridge so buttons can call services that live in other scenes (UnityEvents can't reference
    /// objects across scenes). One per scene; buttons target it.
    /// </summary>
    public class UIActions : MonoBehaviour
    {
        public void GoToLobby() => SceneLoader.Instance?.LoadFrontend(SceneIds.Lobby);
        public void GoToSkillsLab() => SceneLoader.Instance?.LoadFrontend(SceneIds.SkillsLab);

        public void StartScenario()
        {
            SessionManager.Instance?.BeginSession();
            EventLogger.Instance?.Clear();
            SceneLoader.Instance?.LoadStage(SceneIds.StagePrep);
        }

        public void NextStage() => ScenarioDirector.Instance?.Next();
        public void RetryScenario() => ScenarioDirector.Instance?.RetryScenario();
        public void RetryAccess() => ScenarioDirector.Instance?.RetryStage((int)ScenarioStage.Access);
        public void BackToLobby() => ScenarioDirector.Instance?.BackToLobby();
        public void TogglePause() => SessionManager.Instance?.TogglePause();

        public void SetGuided(bool on) { if (on) SessionManager.Instance?.SetMode(TrainingMode.Guided); }
        public void SetAssessment(bool on) { if (on) SessionManager.Instance?.SetMode(TrainingMode.Assessment); }
        public void SetSeated(bool on) { if (on) SessionManager.Instance?.SetPosture(Posture.Seated); }
        public void SetStanding(bool on) { if (on) SessionManager.Instance?.SetPosture(Posture.Standing); }
        public void SetControllers(bool on) { if (on) SessionManager.Instance?.SetInput(InputMode.Controllers); }
        public void SetHands(bool on) { if (on) SessionManager.Instance?.SetInput(InputMode.Hands); }
        public void SetSubtitles(bool on) => SessionManager.Instance?.SetSubtitles(on);
        public void SetTextSmall(bool on) { if (on) SessionManager.Instance?.SetTextScale(0.9f); }
        public void SetTextMedium(bool on) { if (on) SessionManager.Instance?.SetTextScale(1f); }
        public void SetTextLarge(bool on) { if (on) SessionManager.Instance?.SetTextScale(1.15f); }

        public void PlayVoice(int soundId) => Audio.AudioManager.Instance?.Play((Audio.SoundId)soundId);
    }
}
