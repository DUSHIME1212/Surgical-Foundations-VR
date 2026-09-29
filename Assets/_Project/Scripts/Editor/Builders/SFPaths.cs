namespace SurgicalFoundations.EditorTools
{
    /// <summary>Single source of truth for project folder locations used by the builders.</summary>
    public static class SFPaths
    {
        public const string Root = "Assets/_Project";

        public const string Materials = Root + "/Art/Materials";
        public const string Textures = Root + "/Art/Textures";
        public const string Prefabs = Root + "/Prefabs";
        public const string UIPrefabs = Prefabs + "/UI";
        public const string UIScreens = UIPrefabs + "/Screens";
        public const string UIHud = UIPrefabs + "/HUD";
        public const string Audio = Root + "/Audio";
        public const string Data = Root + "/Data";
        public const string SoundBank = Data + "/Audio/SoundBank.asset";
        public const string UITheme = Data + "/UI/UITheme.asset";
        public const string Lighting = Root + "/Lighting";
        public const string LightingSettings = Lighting + "/LightingSettings";
        public const string VolumeProfiles = Lighting + "/VolumeProfiles";
        public const string Scenes = Root + "/Scenes";
        public const string Sprites = Root + "/UI/Sprites";
        public const string Icons = Root + "/UI/Icons";

        public const string XROriginPrefab = "Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Variant.prefab";

        public const string MenuRoot = "Surgical Foundations/";
    }
}
