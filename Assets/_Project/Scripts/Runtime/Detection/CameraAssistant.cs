using System.Linq;
using SurgicalFoundations.Contracts;
using SurgicalFoundations.Interaction;
using SurgicalFoundations.Lighting;
using UnityEngine;

namespace SurgicalFoundations.Detection
{
    /// <summary>
    /// The camera assistant of the Operate stage. By default the assistant holds the laparoscope, so the learner has
    /// both hands for the instruments and steers the view by command (the chips under the monitor): pan, and zoom by
    /// moving the scope in or out. "Hold myself" hands the scope to the learner instead. Every command is logged:
    /// how often a trainee has to ask for the camera to be moved is itself a measure of how they plan their view.
    /// </summary>
    public class CameraAssistant : MonoBehaviour
    {
        /// <summary>The scope's tip stays at least this far inside the port.</summary>
        const float MinInsertion = 0.03f;

        static readonly string[] Spoken = { "pan left", "pan right", "pan up", "pan down", "zoom in", "zoom out" };

        public static CameraAssistant Instance { get; private set; }

        LapInstrument scope;
        Transform view;
        Vector3 baseDirection, panAxis, tiltAxis;

        public CameraAim Aim { get; private set; }
        /// <summary>True while the learner is holding the scope themselves.</summary>
        public bool SelfHold { get; private set; }
        public bool Ready => Aim != null;
        public int Commands { get; private set; }

        void OnEnable() => Instance = this;
        void OnDisable() { if (Instance == this) Instance = null; }

        void Update()
        {
            if (Aim == null && !TryTakeScope()) return;
            if (SelfHold || scope == null || !scope.InPort) return;

            Aim.Step(Time.deltaTime);
            var direction = Quaternion.AngleAxis(Aim.Yaw, panAxis) * Quaternion.AngleAxis(Aim.Pitch, tiltAxis) * baseDirection;
            scope.Aim(direction, Aim.Insertion);
        }

        bool TryTakeScope()
        {
            scope = FindObjectsByType<LapInstrument>(FindObjectsInactive.Exclude)
                .FirstOrDefault(i => i.gameObject.scene == gameObject.scene && i.name.Contains("Laparoscope") && i.InPort);
            if (scope == null) return false;
            var feed = FindObjectsByType<LaparoscopeFeed>(FindObjectsInactive.Include).FirstOrDefault(f => f.gameObject.scene == gameObject.scene);
            view = feed != null ? feed.transform : scope.transform;
            Rebase();
            scope.Grabbable = false; // the assistant has it
            return true;
        }

        /// <summary>Commands are relative to where the scope is now.</summary>
        void Rebase()
        {
            // "Left" and "up" mean left and up in the picture on the monitor, so the axes come from the camera's
            // view, not from the scope (whose roll is whatever the port left it at).
            baseDirection = scope.transform.forward;
            panAxis = view.up;
            tiltAxis = view.right;
            Aim = new CameraAim(scope.Insertion, MinInsertion, scope.MaxInsertion);
        }

        /// <summary>One request to the assistant. False if nobody is there to act on it, or the scope can't go further.</summary>
        public bool Command(CameraCommand command)
        {
            if (Aim == null || SelfHold) return false;
            Commands++;
            DetectionLog.Write(EventClass.Info, EventCodes.CameraCommand, "Camera: " + Spoken[(int)command]);
            return Aim.Apply(command);
        }

        public void SetSelfHold(bool selfHold)
        {
            if (Aim == null || SelfHold == selfHold) return;
            SelfHold = selfHold;
            scope.Grabbable = selfHold;
            if (!selfHold) Rebase(); // the assistant takes over from wherever the learner left the scope
            DetectionLog.Write(EventClass.Info, EventCodes.CameraHold, selfHold ? "Camera: learner holds the scope" : "Camera: assistant holds the scope");
        }
    }
}
