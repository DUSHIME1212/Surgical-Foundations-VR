using System;

namespace SurgicalFoundations.Scenario
{
    public enum ScenarioStage { Prep, Access, Operate, Close, Summary }

    /// <summary>The three learner-facing classes from FR-16, plus Info for neutral system messages.</summary>
    public enum EventClass { OnProtocol, Delayed, Deviation, Info }

    /// <summary>
    /// One protocol event. The client-generated id makes re-syncs idempotent (FR-25).
    /// Will move to the shared contracts library once the backend exists (NFR-07).
    /// </summary>
    [Serializable]
    public class ProtocolEvent
    {
        public string id;
        public float sessionTime;
        public ScenarioStage stage;
        public EventClass eventClass;
        public string code;
        public string message;

        public static ProtocolEvent Create(float sessionTime, ScenarioStage stage, EventClass cls, string code, string message) =>
            new ProtocolEvent
            {
                id = Guid.NewGuid().ToString("N"),
                sessionTime = sessionTime,
                stage = stage,
                eventClass = cls,
                code = code,
                message = message
            };
    }
}
