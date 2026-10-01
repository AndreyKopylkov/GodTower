namespace GodTower.Editor
{
    /// <summary>Asset paths generated or consumed by the editor tooling.</summary>
    public static class ProjectPaths
    {
        public const string Root = "Assets/_Project";
        public const string Configs = Root + "/Configs";
        public const string Prefabs = Root + "/Prefabs";
        public const string Scenes = Root + "/Scenes";

        public const string VContainerSettings = Configs + "/VContainerSettings.asset";
        public const string RootLifetimeScopePrefab = Prefabs + "/RootLifetimeScope.prefab";

        public const string MenuScene = Scenes + "/Menu.unity";
        public const string GameScene = Scenes + "/Game.unity";

        /// <summary>Build order: the first scene is the one the player boots into.</summary>
        public static readonly string[] BuildScenes = { MenuScene, GameScene };

        public const string AndroidBuild = "Builds/GodTower.apk";
    }
}
