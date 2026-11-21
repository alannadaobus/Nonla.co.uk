namespace DanhCu.Editor
{
    /// <summary>Asset paths owned by the setup tool. Keep in sync with CLAUDE.md "Project layout".</summary>
    internal static class ProjectPaths
    {
        public const string Root = "Assets/_Project";

        public const string Scenes = Root + "/Scenes";
        public const string ArenaScenes = Scenes + "/Arenas";
        public const string BootstrapScene = Scenes + "/Bootstrap.unity";
        public const string MainMenuScene = Scenes + "/MainMenu.unity";
        public const string GameplayScene = Scenes + "/Gameplay.unity";
        public const string DefaultArenaSceneName = "Arena_SanDinh";
        public const string DefaultArenaScene = ArenaScenes + "/" + DefaultArenaSceneName + ".unity";

        public const string RenderingSettings = Root + "/Settings/Rendering";
        public const string UrpHighAsset = RenderingSettings + "/URP_HighFidelity.asset";
        public const string UrpHighRenderer = RenderingSettings + "/URP_HighFidelity_Renderer.asset";
        public const string UrpMobileAsset = RenderingSettings + "/URP_Mobile.asset";
        public const string UrpMobileRenderer = RenderingSettings + "/URP_Mobile_Renderer.asset";
        public const string UrpDefaultPostProcessData = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";

        public const string InputActions = Root + "/Settings/Input/DanhCuInputActions.inputactions";

        public const string Data = Root + "/Data";
        public const string GameConfig = Root + "/Resources/DanhCu/GameConfig.asset";

        public static readonly string[] Folders =
        {
            Root + "/Art/Models",
            Root + "/Art/Textures",
            Root + "/Art/Materials",
            Root + "/Art/Shaders",
            Root + "/Art/VFX",
            Root + "/Audio/Music",
            Root + "/Audio/SFX",
            Data + "/Tops",
            Data + "/TopMaterials",
            Data + "/Arenas",
            Data + "/GameModes",
            Data + "/AI",
            Data + "/Physics",
            Root + "/Prefabs/Tops",
            Root + "/Prefabs/Arenas",
            Root + "/Prefabs/UI",
            Root + "/Resources/DanhCu",
            ArenaScenes,
            RenderingSettings,
            Root + "/Settings/Input",
            Root + "/UI",
            Root + "/Localization",
        };
    }
}
