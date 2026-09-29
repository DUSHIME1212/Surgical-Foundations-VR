namespace SurgicalFoundations.Core
{
    /// <summary>Scene names as registered in Build Settings. One persistent scene, the rest load additively.</summary>
    public static class SceneIds
    {
        public const string Bootstrap = "00_Bootstrap";
        public const string Lobby = "01_Lobby";
        public const string SkillsLab = "02_SkillsLab";
        public const string OperatingTheatre = "10_OR_Base";
        public const string StagePrep = "11_Stage_Prep";
        public const string StageAccess = "12_Stage_Access";
        public const string StageOperate = "13_Stage_Operate";
        public const string StageClose = "14_Stage_Close";
        public const string ReplayViewer = "90_ReplayViewer";

        public static readonly string[] Stages = { StagePrep, StageAccess, StageOperate, StageClose };

        public static bool IsStage(string sceneName) => System.Array.IndexOf(Stages, sceneName) >= 0;
    }
}
