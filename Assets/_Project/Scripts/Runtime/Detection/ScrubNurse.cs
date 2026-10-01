using System.Collections.Generic;
using SurgicalFoundations.Audio;
using SurgicalFoundations.Contracts;
using SurgicalFoundations.Core;
using SurgicalFoundations.Scenario;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurgicalFoundations.Detection
{
    /// <summary>
    /// The scrub nurse as a coach (Guided mode only). She turns to face the learner, and when detection logs something
    /// worth a word she says it: what went wrong and what to do about it, in the terms a real scrub nurse would use.
    /// Lines are subtitles until the voice-over is recorded (<see cref="AudioManager.Say"/>). In Assessment she is silent.
    /// </summary>
    public class ScrubNurse : MonoBehaviour
    {
        const float TurnSpeed = 90f;      // degrees per second
        const float MinGap = 2.5f;        // seconds between lines, so two events don't talk over each other
        const float FindInterval = 1f;

        /// <summary>What she says for each detected event. No entry = no comment.</summary>
        static readonly Dictionary<string, string> Lines = new Dictionary<string, string>
        {
            { EventCodes.ScrubStepSkipped, "You skipped a step of the scrub." },
            { EventCodes.ScrubHandsDropped, "Keep your hands up, above your elbows." },
            { EventCodes.ScrubComplete, "Good scrub. Gloves are on the back table." },
            { EventCodes.Regloved, "Fresh gloves on. You are sterile again." },
            { EventCodes.GlovingSkipped, "You are not gloved. That is a break in sterility." },
            { EventCodes.TrayComplete, "Opening count agreed." },
            { EventCodes.TrayItemUnchecked, "The opening count was not finished." },
            { EventCodes.PortOffTarget, "That mark is off the landmark. Mark it again." },
            { EventCodes.BleedTrocarWithdrawn, "Now press on the site and hold it." },
            { EventCodes.BleedPressureHeld, "Let go and watch the site." },
            { EventCodes.BleedControlled, "It is dry. Bleeding controlled." },
            { EventCodes.BleedUnmanaged, "That site is still bleeding." },
            { EventCodes.TrocarForce, "Gently. Let the trocar do the work." },
            { EventCodes.TrocarUnsafeEntry, "Too deep. We have bleeding. Stop and draw the trocar back." },
            { EventCodes.PegDropped, "Ring dropped. Pick it up and carry on." },
            { EventCodes.TissueForce, "Easy on the tissue." },
            { EventCodes.ClipMisplaced, "That clip is off the mark." },
            { EventCodes.CutWrongPlace, "Not there. Cut between the clips." },
            { EventCodes.CutUnclipped, "That was cut before it was clipped." },
            { EventCodes.CountComplete, "Swab count is correct." },
            { EventCodes.CountMismatchUnresolved, "We are closing with a swab unaccounted for." },
            { EventCodes.PortRemovedBlind, "I could not see that port site on the monitor." },
            { EventCodes.PortRemovedOutOfOrder, "The camera port comes out last." },
            { EventCodes.SiteNotClosed, "That port site is still open." },
        };

        Transform nurse;
        float nextFind;
        float lastLine = -100f;
        EventLogger subscribed;

        void Update()
        {
            // The event log lives in the bootstrap scene and may appear after the theatre does.
            if (subscribed == null && EventLogger.Instance != null)
            {
                subscribed = EventLogger.Instance;
                subscribed.EventLogged += OnEvent;
            }
            FaceLearner();
        }

        void OnDestroy()
        {
            if (subscribed != null) subscribed.EventLogged -= OnEvent;
        }

        void OnEvent(ProtocolEvent e)
        {
            if (!Guided || e == null || !Lines.TryGetValue(e.code, out var line)) return;
            if (Time.unscaledTime - lastLine < MinGap) return;
            lastLine = Time.unscaledTime;
            AudioManager.Instance?.Say(line);
        }

        static bool Guided => SessionManager.Instance == null || SessionManager.Instance.Settings.mode == TrainingMode.Guided;

        void FaceLearner()
        {
            if (nurse == null)
            {
                if (Time.unscaledTime < nextFind) return;
                nextFind = Time.unscaledTime + FindInterval;
                nurse = FindNurse();
                if (nurse == null) return;
            }
            var head = LearnerRig.Head;
            if (head == null || !Guided) return;

            var to = head.position - nurse.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.04f) return;
            var want = Quaternion.LookRotation(to, Vector3.up);
            nurse.rotation = Quaternion.RotateTowards(nurse.rotation, want, TurnSpeed * Time.deltaTime);
        }

        /// <summary>She stands in whichever stage scene the builder placed her (the Prep back table today).</summary>
        static Transform FindNurse()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(false))
                    if (t.name.StartsWith("CHR_ScrubNurse")) return t;
            }
            return null;
        }
    }
}
