using System;
using System.Collections.Generic;
using SurgicalFoundations.Contracts;

namespace SurgicalFoundations.Detection
{
    // ───────────────────────────── Instrument drift (FR-13) ─────────────────────────────

    /// <summary>
    /// An instrument tip left out of the laparoscope's view for longer than the threshold is a drift (working blind
    /// is how unseen injuries happen). Bringing it back logs the correction and how long it took.
    /// </summary>
    public class DriftMonitor
    {
        class Track
        {
            public float outTime;
            public bool drifting;
        }

        readonly float threshold;
        readonly ProtocolLog log;
        readonly Dictionary<string, Track> tracks = new Dictionary<string, Track>();

        public DriftMonitor(float thresholdSeconds, ProtocolLog log)
        {
            threshold = thresholdSeconds;
            this.log = log;
        }

        public event Action Changed;

        /// <summary>The instrument currently drifting, or null.</summary>
        public string Drifting { get; private set; }
        public int DriftCount { get; private set; }

        public void Tick(string instrument, bool tipInView, float dt)
        {
            if (!tracks.TryGetValue(instrument, out var t)) tracks[instrument] = t = new Track();

            if (!tipInView)
            {
                t.outTime += dt;
                if (!t.drifting && t.outTime > threshold)
                {
                    t.drifting = true;
                    DriftCount++;
                    Drifting = instrument;
                    log(EventClass.Delayed, EventCodes.Drift, $"{instrument} drifted out of view");
                    Changed?.Invoke();
                }
                return;
            }

            if (t.drifting)
            {
                t.drifting = false;
                log(EventClass.Info, EventCodes.DriftCorrected, $"Drift corrected after {t.outTime:0.0} s");
                if (Drifting == instrument) Drifting = null;
                foreach (var kv in tracks)
                    if (kv.Value.drifting) Drifting = kv.Key;
                Changed?.Invoke();
            }
            t.outTime = 0f;
        }
    }

    // ───────────────────────────── Swab count (FR-15) ─────────────────────────────

    /// <summary>
    /// Closing count against the opening count. Closure is blocked until every swab is accounted for; closing anyway
    /// is the one critical event that fails the run whatever the score.
    /// </summary>
    public class SwabCount
    {
        readonly ProtocolLog log;

        public SwabCount(int opening, ProtocolLog log)
        {
            Opening = opening;
            this.log = log;
        }

        public event Action Changed;

        public int Opening { get; }
        public int Counted { get; private set; }
        public int Missing => Opening - Counted;
        public bool Complete => Counted >= Opening;
        public bool ClosedWithMismatch { get; private set; }

        /// <param name="wasHidden">The swab that had been left in the field.</param>
        /// <param name="searchSeconds">Time since the count started; logged for the hidden swab.</param>
        public void Count(bool wasHidden, float searchSeconds)
        {
            if (Complete) return;
            Counted++;
            if (wasHidden)
                log(EventClass.Delayed, EventCodes.SwabFound, $"Missing swab located · search took {searchSeconds:0} s");
            log(EventClass.OnProtocol, EventCodes.SwabCounted, $"Swab {Counted} of {Opening} counted");
            if (Complete) log(EventClass.OnProtocol, EventCodes.CountComplete, "Swab count correct");
            Changed?.Invoke();
        }

        /// <summary>The learner closed without resolving the count.</summary>
        public void CloseWithMismatch()
        {
            if (Complete || ClosedWithMismatch) return;
            ClosedWithMismatch = true;
            log(EventClass.Deviation, EventCodes.CountMismatchUnresolved,
                $"Closed with {Missing} swab{(Missing == 1 ? "" : "s")} unaccounted for");
            Changed?.Invoke();
        }
    }
}
