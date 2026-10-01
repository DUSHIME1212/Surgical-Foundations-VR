using System;
using System.Linq;
using SurgicalFoundations.Contracts;

namespace SurgicalFoundations.Detection
{
    // ───────────────────────────── Opening count (screen 07) ─────────────────────────────

    /// <summary>
    /// The opening count: every instrument and swab on the back table is pointed at and confirmed before the field is
    /// draped. What is confirmed here is what the closing count is checked against. Confirming the sterile field with
    /// lines still unchecked is allowed, and logged line by line.
    /// </summary>
    public class TrayCheck
    {
        readonly ProtocolLog log;
        readonly string[] names;
        readonly int[] quantity;
        readonly int[] confirmed;

        public TrayCheck((string name, int quantity)[] lines, ProtocolLog log)
        {
            this.log = log;
            names = lines.Select(l => l.name).ToArray();
            quantity = lines.Select(l => l.quantity).ToArray();
            confirmed = new int[lines.Length];
        }

        public event Action Changed;
        public int Lines => names.Length;
        public string Name(int line) => names[line];
        public int Quantity(int line) => quantity[line];
        public int Confirmed(int line) => confirmed[line];
        public bool LineDone(int line) => confirmed[line] >= quantity[line];
        public int TotalItems => quantity.Sum();
        public int TotalConfirmed => confirmed.Sum();
        public int LinesUnchecked => Enumerable.Range(0, names.Length).Count(i => !LineDone(i));
        public bool Complete => LinesUnchecked == 0;

        /// <summary>One item of this line was pointed at and confirmed. False if the line was already complete.</summary>
        public bool Confirm(int line)
        {
            if (LineDone(line)) return false;
            confirmed[line]++;
            if (LineDone(line))
                log(EventClass.OnProtocol, EventCodes.TrayItemConfirmed, $"{names[line]} ×{quantity[line]} confirmed");
            if (Complete)
                log(EventClass.OnProtocol, EventCodes.TrayComplete, $"Opening count complete: {TotalItems} items");
            Changed?.Invoke();
            return true;
        }

        public void SkipRemaining()
        {
            for (int i = 0; i < names.Length; i++)
                if (!LineDone(i))
                    log(EventClass.Deviation, EventCodes.TrayItemUnchecked, $"{names[i]} not confirmed in the opening count ({confirmed[i]} of {quantity[i]})");
        }
    }

    // ───────────────────────────── Port-site bleed (screen 10) ─────────────────────────────

    public enum BleedPhase { Withdraw = 0, Pressure = 1, Observe = 2, Controlled = 3 }

    /// <summary>
    /// Managing a vessel injury at a port site after an unsafe entry: stop and draw the trocar back, hold pressure on
    /// the site, then let go and watch that it stays dry. The time to control is logged. The steps and their durations
    /// are placeholders for the SME to define.
    /// </summary>
    public class BleedControl
    {
        readonly ProtocolLog log;
        readonly string port;
        readonly float pressureSeconds;
        readonly float observeSeconds;
        float held;
        float observed;

        public BleedControl(string portName, float pressureSeconds, float observeSeconds, ProtocolLog log)
        {
            port = portName;
            this.pressureSeconds = pressureSeconds;
            this.observeSeconds = observeSeconds;
            this.log = log;
        }

        public event Action Changed;
        public BleedPhase Phase { get; private set; }
        public bool Controlled => Phase == BleedPhase.Controlled;
        public bool Skipped { get; private set; }
        /// <summary>Seconds since the injury.</summary>
        public float Elapsed { get; private set; }
        /// <summary>Times the pressure was let go before it had been held long enough.</summary>
        public int Interruptions { get; private set; }

        /// <summary>Seconds still needed in the current phase (0 for the withdraw phase).</summary>
        public float Remaining =>
            Phase == BleedPhase.Pressure ? Math.Max(0f, pressureSeconds - held)
            : Phase == BleedPhase.Observe ? Math.Max(0f, observeSeconds - observed) : 0f;

        /// <param name="withdrawn">The trocar tip is back above the danger depth.</param>
        /// <param name="pressing">A hand is on the bleeding site.</param>
        /// <param name="watching">The learner is looking at the site.</param>
        public void Tick(bool withdrawn, bool pressing, bool watching, float dt)
        {
            if (Controlled || Skipped || dt <= 0f) return;
            Elapsed += dt;

            switch (Phase)
            {
                case BleedPhase.Withdraw:
                    if (!withdrawn) return;
                    Phase = BleedPhase.Pressure;
                    log(EventClass.OnProtocol, EventCodes.BleedTrocarWithdrawn, $"{port} port: trocar drawn back");
                    Changed?.Invoke();
                    break;

                case BleedPhase.Pressure:
                    if (pressing)
                    {
                        held += dt;
                        if (held >= pressureSeconds)
                        {
                            Phase = BleedPhase.Observe;
                            log(EventClass.OnProtocol, EventCodes.BleedPressureHeld, $"{port} port: pressure held on the site");
                        }
                        Changed?.Invoke();
                    }
                    else if (held > 0f)
                    {
                        // Pressure only works if it is kept on: letting go starts the hold again.
                        held = 0f;
                        Interruptions++;
                        Changed?.Invoke();
                    }
                    break;

                case BleedPhase.Observe:
                    if (pressing || !watching) return;
                    observed += dt;
                    if (observed >= observeSeconds)
                    {
                        Phase = BleedPhase.Controlled;
                        log(EventClass.Delayed, EventCodes.BleedControlled, $"{port} port: bleeding controlled in {Elapsed:0} s");
                    }
                    Changed?.Invoke();
                    break;
            }
        }

        /// <summary>The learner carried on without controlling the bleed.</summary>
        public void Skip()
        {
            if (Controlled || Skipped) return;
            Skipped = true;
            log(EventClass.Deviation, EventCodes.BleedUnmanaged, $"{port} port: carried on with the bleeding not controlled");
            Changed?.Invoke();
        }

        public StepState State(BleedPhase row)
        {
            if (row < Phase) return StepState.Done;
            if (Skipped) return StepState.Skipped;
            return row == Phase ? StepState.Active : StepState.Pending;
        }
    }
}
