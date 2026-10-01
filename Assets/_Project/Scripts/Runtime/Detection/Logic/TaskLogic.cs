using System;
using System.Linq;
using SurgicalFoundations.Contracts;

namespace SurgicalFoundations.Detection
{
    public enum OperateTask { PegTransfer = 0, Dissection = 1, ClipAndCut = 2 }

    /// <summary>What a task cost the learner: shown live in Guided mode and logged when the task ends.</summary>
    public class TaskMetrics
    {
        public float seconds;
        public float pathMetres;
        public int errors;

        public void Reset() { seconds = 0f; pathMetres = 0f; errors = 0; }
    }

    // ───────────────────────────── Peg transfer ─────────────────────────────

    /// <summary>Move every ring from the near row of pegs to the far row. A ring let go anywhere else is a drop.</summary>
    public class PegTransfer
    {
        readonly ProtocolLog log;

        public PegTransfer(int rings, ProtocolLog log)
        {
            Total = rings;
            this.log = log;
        }

        public event Action Changed;
        public int Total { get; }
        public int Transferred { get; private set; }
        public int Drops { get; private set; }
        public bool Complete => Transferred >= Total;

        public void Placed()
        {
            if (Complete) return;
            Transferred++;
            log(EventClass.OnProtocol, EventCodes.PegTransferred, $"Ring {Transferred} of {Total} transferred");
            Changed?.Invoke();
        }

        public void Dropped()
        {
            Drops++;
            log(EventClass.Delayed, EventCodes.PegDropped, "Ring dropped");
            Changed?.Invoke();
        }
    }

    // ───────────────────────────── Dissection ─────────────────────────────

    /// <summary>
    /// Open the tissue plane point by point by spreading the dissector's jaws in it. Moving an instrument fast while
    /// it is in the tissue is excess force (the stand-in until soft tissue reports real strain).
    /// </summary>
    public class Dissection
    {
        /// <summary>One rough movement is one event, however many frames it lasts.</summary>
        public const float ForceCooldown = 2f;

        readonly ProtocolLog log;
        readonly bool[] opened;
        readonly float maxTipSpeed;
        float cooldown;

        public Dissection(int points, float maxTipSpeed, ProtocolLog log)
        {
            opened = new bool[points];
            this.maxTipSpeed = maxTipSpeed;
            this.log = log;
        }

        public event Action Changed;
        public int Total => opened.Length;
        public int Opened => opened.Count(o => o);
        public int ForceEvents { get; private set; }
        public bool Complete => opened.All(o => o);
        public bool IsOpen(int point) => opened[point];

        /// <summary>The dissector's jaws were spread at this point. True if that opened it.</summary>
        public bool Open(int point)
        {
            if (opened[point]) return false;
            opened[point] = true;
            log(EventClass.OnProtocol, EventCodes.DissectionOpened, $"Plane opened {Opened} of {Total}");
            Changed?.Invoke();
            return true;
        }

        /// <summary>Call every frame for each instrument tip. True when this frame logged an excess-force event.</summary>
        public bool Contact(bool inTissue, float tipSpeed, float dt)
        {
            cooldown = Math.Max(0f, cooldown - dt);
            if (!inTissue || tipSpeed <= maxTipSpeed || cooldown > 0f) return false;
            cooldown = ForceCooldown;
            ForceEvents++;
            log(EventClass.Deviation, EventCodes.TissueForce, "Excess force on tissue");
            Changed?.Invoke();
            return true;
        }
    }

    // ───────────────────────────── Clip and cut ─────────────────────────────

    /// <summary>
    /// Clip the structure at every clip site, then cut between the clips. Cutting first divides an unsecured
    /// structure: the task ends, with a deviation.
    /// </summary>
    public class ClipAndCut
    {
        readonly ProtocolLog log;
        readonly bool[] clipped;

        public ClipAndCut(int clipSites, ProtocolLog log)
        {
            clipped = new bool[clipSites];
            this.log = log;
        }

        public event Action Changed;
        public int Sites => clipped.Length;
        public int Clips => clipped.Count(c => c);
        public int Misplaced { get; private set; }
        public bool AllClipped => clipped.All(c => c);
        public bool IsClipped(int site) => clipped[site];
        public bool Complete { get; private set; }
        public bool CutWithoutClips { get; private set; }

        public bool PlaceClip(int site)
        {
            if (Complete || clipped[site]) return false;
            clipped[site] = true;
            log(EventClass.OnProtocol, EventCodes.ClipPlaced, $"Clip {Clips} of {Sites} placed");
            Changed?.Invoke();
            return true;
        }

        /// <summary>The applier was fired on the structure but not on a clip site.</summary>
        public void ClipMisplaced()
        {
            if (Complete) return;
            Misplaced++;
            log(EventClass.Delayed, EventCodes.ClipMisplaced, "Clip placed off the marked site");
            Changed?.Invoke();
        }

        /// <summary>The scissors closed on the structure. True if this divided it.</summary>
        public bool Cut(bool atCutPoint)
        {
            if (Complete) return false;
            if (!atCutPoint)
            {
                log(EventClass.Deviation, EventCodes.CutWrongPlace, "Cut away from the marked point");
                Changed?.Invoke();
                return false;
            }
            Complete = true;
            if (AllClipped) log(EventClass.OnProtocol, EventCodes.CutDone, "Structure divided between the clips");
            else
            {
                CutWithoutClips = true;
                log(EventClass.Deviation, EventCodes.CutUnclipped, $"Structure cut with {Clips} of {Sites} clips in place");
            }
            Changed?.Invoke();
            return true;
        }
    }

    // ───────────────────────────── Port removal and closure ─────────────────────────────

    /// <summary>
    /// Ports come out under vision (the site watched through the scope for bleeding), working ports first and the
    /// camera port last, because the camera is what does the watching.
    /// </summary>
    public class PortRemoval
    {
        readonly ProtocolLog log;
        readonly string[] names;
        readonly bool[] removed;
        readonly int cameraIndex;

        public PortRemoval(string[] names, int cameraIndex, ProtocolLog log)
        {
            this.names = names;
            this.cameraIndex = cameraIndex;
            this.log = log;
            removed = new bool[names.Length];
        }

        public event Action Changed;
        public int Count => names.Length;
        public string Name(int port) => names[port];
        public bool IsRemoved(int port) => removed[port];
        public bool Complete => removed.All(r => r);

        /// <param name="siteInView">Whether the port site was on the monitor as the port came out.</param>
        public bool Remove(int port, bool siteInView)
        {
            if (removed[port]) return false;
            removed[port] = true;
            if (port == cameraIndex)
            {
                if (Complete) log(EventClass.OnProtocol, EventCodes.PortRemoved, $"{names[port]} port removed last");
                else log(EventClass.Deviation, EventCodes.PortRemovedOutOfOrder, $"{names[port]} port removed before the working ports");
            }
            else if (siteInView) log(EventClass.OnProtocol, EventCodes.PortRemoved, $"{names[port]} port removed under vision");
            else log(EventClass.Deviation, EventCodes.PortRemovedBlind, $"{names[port]} port removed without camera view");
            Changed?.Invoke();
            return true;
        }

        public void SkipRemaining()
        {
            for (int i = 0; i < names.Length; i++)
                if (!removed[i]) log(EventClass.Deviation, EventCodes.PortLeftIn, $"{names[i]} port left in place");
        }
    }

    /// <summary>Each port site is closed with one stitch: a bite of the needle on each side of the wound.</summary>
    public class PortClosure
    {
        readonly ProtocolLog log;
        readonly string[] names;
        readonly bool[,] bites;
        readonly bool[] closed;

        public PortClosure(string[] names, ProtocolLog log)
        {
            this.names = names;
            this.log = log;
            bites = new bool[names.Length, 2];
            closed = new bool[names.Length];
        }

        public event Action Changed;
        public int Count => names.Length;
        public bool IsClosed(int site) => closed[site];
        public bool HasBite(int site, int side) => bites[site, side];
        public bool Complete => closed.All(c => c);

        /// <summary>The needle took a bite at one side (0 or 1) of a site. True when that completed the stitch.</summary>
        public bool Bite(int site, int side)
        {
            if (closed[site] || bites[site, side]) return false;
            bites[site, side] = true;
            Changed?.Invoke();
            if (!bites[site, 0] || !bites[site, 1]) return false;
            closed[site] = true;
            log(EventClass.OnProtocol, EventCodes.SiteClosed, $"{names[site]} port site closed");
            Changed?.Invoke();
            return true;
        }

        public void SkipRemaining()
        {
            for (int i = 0; i < names.Length; i++)
                if (!closed[i]) log(EventClass.Deviation, EventCodes.SiteNotClosed, $"{names[i]} port site not closed");
        }
    }
}
