using System;
using System.Collections.Generic;
using System.Linq;
using SurgicalFoundations.Contracts;
using UnityEngine;

namespace SurgicalFoundations.Detection
{
    // ───────────────────────────── Port sites (screen 08) ─────────────────────────────

    public class PortSite
    {
        public string name;
        public Vector3 target;
        public bool marked;
        public Vector3 mark;
        public float errorMm;
        public bool onTarget;
        internal bool deviationLogged;
    }

    /// <summary>
    /// The learner marks three port sites on the abdomen. Each mark is measured against its landmark target; the first
    /// off-target mark of a port is a deviation, and the learner may re-mark to correct it before entry.
    /// </summary>
    public class PortSitePlan
    {
        /// <summary>A mark further than this from every target isn't a port mark at all (e.g. a stray trigger pull).</summary>
        public const float MaxAssignDistance = 0.12f;

        readonly float radius;
        readonly ProtocolLog log;

        public PortSitePlan(IEnumerable<(string name, Vector3 target)> targets, float targetRadiusMetres, ProtocolLog log)
        {
            Sites = targets.Select(t => new PortSite { name = t.name, target = t.target }).ToList();
            radius = targetRadiusMetres;
            this.log = log;
        }

        public event Action Changed;

        public List<PortSite> Sites { get; }
        public bool AllMarked => Sites.All(s => s.marked);
        public bool AllOnTarget => Sites.All(s => s.marked && s.onTarget);

        /// <summary>Marks (or re-marks) the port nearest to the point. Null if the point is nowhere near a port site.</summary>
        public PortSite Mark(Vector3 point)
        {
            // Unmarked ports first, so three marks made in a row never pile onto one port.
            var site = Nearest(point, Sites.Where(s => !s.marked)) ?? Nearest(point, Sites);
            if (site == null) return null;

            site.marked = true;
            site.mark = point;
            site.errorMm = Flat(point - site.target).magnitude * 1000f;
            site.onTarget = site.errorMm <= radius * 1000f;
            if (site.onTarget)
                log(EventClass.OnProtocol, EventCodes.PortMarked, $"{site.name} port on target ({site.errorMm:0} mm)");
            else if (!site.deviationLogged)
            {
                site.deviationLogged = true;
                log(EventClass.Deviation, EventCodes.PortOffTarget, $"{site.name} port marked {site.errorMm:0} mm off target");
            }
            Changed?.Invoke();
            return site;
        }

        public void SkipUnmarked()
        {
            foreach (var s in Sites.Where(s => !s.marked))
                log(EventClass.Deviation, EventCodes.PortMarkingSkipped, $"{s.name} port site not marked");
        }

        PortSite Nearest(Vector3 point, IEnumerable<PortSite> candidates)
        {
            PortSite best = null;
            var bestDistance = MaxAssignDistance;
            foreach (var s in candidates)
            {
                var d = Flat(point - s.target).magnitude;
                if (d <= bestDistance) { best = s; bestDistance = d; }
            }
            return best;
        }

        // Port position is judged on the skin surface; height above it doesn't matter.
        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }

    // ───────────────────────────── Trocar entry (screen 09, FR-08) ─────────────────────────────

    public enum TrocarPhase { Approaching, Entering, Placed, Unsafe }

    [Serializable]
    public class TrocarLimits
    {
        /// <summary>Skin + fat + fascia + peritoneum: the tip is inside the cavity from here.</summary>
        public float entryDepth = 0.033f;
        /// <summary>Deepest a placed port should sit.</summary>
        public float safeDepth = 0.058f;
        /// <summary>Beyond this the tip reaches vessels or bowel.</summary>
        public float unsafeDepth = 0.070f;
        public float angleToleranceDeg = 15f;
        public float maxForceN = 60f;
        /// <summary>Held in the safe window this long = placed.</summary>
        public float holdSeconds = 1f;
        /// <summary>No force sensor in a controller: push speed stands in for force (0.15 m/s ≈ the 60 N limit).</summary>
        public float newtonsPerMetrePerSecond = 400f;
    }

    /// <summary>
    /// Judges one trocar insertion from tip depth below the skin, angle off the ideal axis, and a force estimate.
    /// Too shallow an angle is a technique flag; too much force or going too deep are deviations (too deep also
    /// triggers the vessel-injury branch).
    /// </summary>
    public class TrocarEntry
    {
        readonly string port;
        readonly TrocarLimits limits;
        readonly ProtocolLog log;
        float lastDepth = -1f;
        float hold;
        float angleOffTime;
        float angleOkTime;
        bool forceLogged;

        public TrocarEntry(string portName, TrocarLimits limits, ProtocolLog log)
        {
            port = portName;
            this.limits = limits;
            this.log = log;
        }

        public string Port => port;
        public TrocarPhase Phase { get; private set; }
        public float Depth { get; private set; }
        public float Angle { get; private set; }
        public float Force { get; private set; }
        public bool AngleWarning { get; private set; }
        public float HoldProgress => Mathf.Clamp01(hold / limits.holdSeconds);
        public bool Finished => Phase == TrocarPhase.Placed || Phase == TrocarPhase.Unsafe;

        public void Tick(float depthMetres, float angleDeg, float dt)
        {
            if (Finished || dt <= 0f) return;

            var speed = lastDepth < 0f ? 0f : Mathf.Max(0f, (depthMetres - lastDepth) / dt);
            lastDepth = depthMetres;
            // Smoothed so one tracking jitter frame doesn't read as a shove.
            Force = Mathf.Lerp(Force, speed * limits.newtonsPerMetrePerSecond, Mathf.Clamp01(dt * 12f));
            Depth = Mathf.Max(0f, depthMetres);
            Angle = angleDeg;

            if (depthMetres <= 0.002f)
            {
                Phase = TrocarPhase.Approaching;
                hold = 0f;
                return;
            }
            Phase = TrocarPhase.Entering;

            var angleOk = angleDeg <= limits.angleToleranceDeg;
            if (!angleOk && depthMetres > 0.005f)
            {
                angleOkTime = 0f;
                angleOffTime += dt;
                if (angleOffTime >= 0.2f && !AngleWarning)
                {
                    AngleWarning = true;
                    log(EventClass.Delayed, EventCodes.TrocarAngle, $"{port} port: trocar {angleDeg:0}° off the entry axis");
                }
            }
            else
            {
                angleOffTime = 0f;
                angleOkTime += dt;
                if (angleOkTime >= 0.5f) AngleWarning = false; // corrected; a later slip is flagged again
            }

            if (Force > limits.maxForceN && !forceLogged)
            {
                forceLogged = true;
                log(EventClass.Deviation, EventCodes.TrocarForce, $"{port} port: excess force on entry");
            }

            if (depthMetres >= limits.unsafeDepth)
            {
                Phase = TrocarPhase.Unsafe;
                log(EventClass.Deviation, EventCodes.TrocarUnsafeEntry, $"{port} port: trocar entered too deep ({depthMetres * 1000f:0} mm) — vessel injury");
                return;
            }

            if (depthMetres >= limits.entryDepth && depthMetres <= limits.safeDepth && angleOk)
            {
                hold += dt;
                if (hold >= limits.holdSeconds)
                {
                    Phase = TrocarPhase.Placed;
                    log(EventClass.OnProtocol, EventCodes.TrocarPlaced, $"{port} port placed");
                }
            }
            else
            {
                hold = 0f;
            }
        }
    }
}
