using SurgicalFoundations.Contracts;
using SurgicalFoundations.Core;
using SurgicalFoundations.Interaction;
using SurgicalFoundations.Scenario;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SurgicalFoundations.Detection
{
    /// <summary>
    /// Watches the learner's gloved hands for sterility breaks (FR-07) for the whole procedure, so it lives in
    /// 10_OR_Base, which stays loaded across stages. A break is: touching a non-sterile surface (colliders named
    /// "NonSterileEdge…", e.g. the back-table edge), or dropping a hand below the sterile field. It also owns how
    /// the hands look: bare, gloved, contaminated.
    /// </summary>
    public class SterilityMonitor : MonoBehaviour
    {
        public static SterilityMonitor Instance { get; private set; }

        const string NonSterilePrefix = "NonSterileEdge";
        const float ProbeRadius = 0.05f;
        /// <summary>How far below the table top still counts as the sterile field.</summary>
        const float BelowFieldMargin = 0.20f;
        const float BelowFieldSeconds = 1.5f;
        /// <summary>With no glove packet in reach (later stages), the scrub nurse re-gloves the learner after this long.</summary>
        const float AssistedRegloveSeconds = 6f;

        readonly Collider[] hits = new Collider[16];
        readonly float[] belowTime = new float[2];
        readonly bool[] belowArmed = new bool[2];
        float contaminatedTime;
        bool heldAtBreak;

        public SterilityState State { get; private set; }

        void Awake()
        {
            Instance = this;
            ResetState();
        }

        void OnEnable()
        {
            if (SessionManager.Instance != null) SessionManager.Instance.SessionStarted += ResetState;
            ScenarioDirector.StageEntered += OnStage;
        }

        void OnDisable()
        {
            if (SessionManager.Instance != null) SessionManager.Instance.SessionStarted -= ResetState;
            ScenarioDirector.StageEntered -= OnStage;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void ResetState()
        {
            State = new SterilityState(DetectionLog.Write);
            State.Changed += ApplyLook;
            contaminatedTime = 0f;
            ApplyLook();
        }

        void OnStage(ScenarioStage stage)
        {
            // Starting straight at a later stage (retry, or a scene opened in the editor): the learner is already gowned.
            if (stage != ScenarioStage.Prep && stage != ScenarioStage.Summary && State.Left == HandSterility.Bare)
                AssumeGloved();
        }

        /// <summary>Gloved without the event, for stages entered directly.</summary>
        public void AssumeGloved() => State.StartGloved();

        void Update()
        {
            if (State == null) return;
            var sm = SessionManager.Instance;
            if (sm != null && (!sm.IsRunning || sm.IsPaused)) return;

            Probe(true);
            Probe(false);

            if (State.AnyContaminated)
            {
                // Recovery: pick up a fresh glove packet. A packet already in the hand at the moment of the break
                // doesn't count; it has to be put down and taken again.
                var packet = FindGlovePacket();
                if (contaminatedTime == 0f) heldAtBreak = packet != null && packet.isSelected;
                contaminatedTime += Time.deltaTime;
                if (packet != null)
                {
                    if (!packet.isSelected) heldAtBreak = false;
                    else if (!heldAtBreak) State.Glove();
                }
                else if (contaminatedTime >= AssistedRegloveSeconds) State.Glove();
            }
            else contaminatedTime = 0f;
        }

        void Probe(bool left)
        {
            if ((left ? State.Left : State.Right) != HandSterility.Gloved) return;
            var hand = LearnerRig.Hand(left);
            if (hand == null || !hand.gameObject.activeInHierarchy || !LearnerRig.Tracked(left)) return;

            int count = Physics.OverlapSphereNonAlloc(hand.position, ProbeRadius, hits, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (!hits[i].name.StartsWith(NonSterilePrefix)) continue;
                State.Contact(left, "the non-sterile edge of the back table");
                return;
            }

            var tableTop = TableHeight();
            int h = left ? 0 : 1;
            if (hand.position.y >= tableTop - BelowFieldMargin)
            {
                belowTime[h] = 0f;
                belowArmed[h] = true;
            }
            // One break per drop: the hand has to come back up before dropping it again can count again.
            else if (belowArmed[h])
            {
                belowTime[h] += Time.deltaTime;
                if (belowTime[h] >= BelowFieldSeconds)
                {
                    belowTime[h] = 0f;
                    belowArmed[h] = false;
                    State.Contact(left, "below the sterile field (hand dropped below table level)");
                }
            }
        }

        static float TableHeight() =>
            SessionManager.Instance != null ? SessionManager.Instance.Settings.tableHeightCm / 100f : 0.92f;

        void ApplyLook()
        {
            if (HandAppearance.Instance == null || State == null) return;
            HandAppearance.Instance.Set(LookFor(State.Left), LookFor(State.Right));
        }

        static HandLook LookFor(HandSterility s) =>
            s == HandSterility.Gloved ? HandLook.Gloved : s == HandSterility.Contaminated ? HandLook.Contaminated : HandLook.Bare;

        static XRGrabInteractable cachedPacket;
        static float nextPacketSearch;

        /// <summary>The glove packet on the back table, if the Prep stage is loaded. Searched at most twice a second.</summary>
        public static XRGrabInteractable FindGlovePacket()
        {
            if (cachedPacket != null) return cachedPacket;
            if (Time.unscaledTime < nextPacketSearch) return null;
            nextPacketSearch = Time.unscaledTime + 0.5f;
            foreach (var grab in FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Exclude))
                if (grab.name.StartsWith("PPE_GlovesPacket")) return cachedPacket = grab;
            return null;
        }
    }
}
