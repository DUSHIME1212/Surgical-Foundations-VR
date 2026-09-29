namespace SurgicalFoundations.Audio
{
    /// <summary>
    /// Every sound in the game. Values are explicit and grouped by hundreds so adding a sound never shifts
    /// serialized references. The name matches the clip file name without its "SFX_" prefix and "_Loop" suffix
    /// (SFX_UI_Click.wav → UI_Click, AMB_OR_RoomTone_Loop.wav → AMB_OR_RoomTone); the SoundBank builder relies on it.
    /// </summary>
    public enum SoundId
    {
        None = 0,

        // UI (100)
        UI_Hover = 100, UI_Click, UI_Confirm, UI_Back, UI_Toggle, UI_PanelOpen, UI_PanelClose, UI_Denied,

        // Feedback (200)
        FB_OnProtocol = 200, FB_Delayed, FB_Deviation, FB_TechniqueFlag, FB_Contamination, FB_DriftPing,
        FB_StageComplete, FB_CountTick, FB_CountMatch, FB_SummaryPass, FB_PauseIn, FB_PauseOut, FB_Transition,

        // Instruments (300)
        INST_PickupMetal = 300, INST_PlaceOnTray, INST_JawOpen, INST_JawClose, INST_Ratchet, INST_ClipFire,
        INST_ScissorsCut, INST_ScalpelIncision, INST_TrocarPop, INST_TrocarValve, INST_SuturePull,

        // Environment one-shots and machine loops (400)
        ENV_SoapPump = 400, ENV_GloveSnap, ENV_GownRustle, ENV_DrapeRustle, ENV_PacketTear, ENV_SwabDrop,
        ENV_RingDrop, ENV_BucketDrop, ENV_MonitorPowerOn, ENV_LightSwitch, ENV_MonitorBeep, ENV_TableMotor,

        // Ambience loops (500)
        AMB_OR_RoomTone = 500, AMB_Lobby_Pad, AMB_SkillsLab_RoomTone, AMB_AnaesthesiaMonitor_72bpm,
        AMB_Insufflator, AMB_TapWater, AMB_SurgicalLight_Hum,

        // Voice prompts (600) — TTS placeholders until recorded VO
        VO_Lobby_Welcome = 600, VO_Calibrate_Height, VO_Calibrate_Table, VO_Tutorial_SqueezeTrigger,
        VO_Prep_HandsAboveElbows, VO_Prep_SterilityBroken, VO_Access_MarkRightPort, VO_Access_AngleShallow,
        VO_Operate_DriftLeft, VO_Close_SwabMissing, VO_Close_WatchPortSite, VO_Summary_Completed,
    }

    public enum AudioCategory { UI, Feedback, SFX, Ambience, Voice }
}
