namespace GodTower.Editor
{
    /// <summary>Asset paths generated or consumed by the editor tooling.</summary>
    public static class ProjectPaths
    {
        public const string Root = "Assets/_Project";
        public const string Configs = Root + "/Configs";
        public const string LevelConfigs = Configs + "/Levels";
        public const string Materials = Root + "/Materials";
        public const string Animation = Root + "/Animation";
        public const string Prefabs = Root + "/Prefabs";
        public const string Scenes = Root + "/Scenes";

        public const string VContainerSettings = Configs + "/VContainerSettings.asset";
        public const string RootLifetimeScopePrefab = Prefabs + "/RootLifetimeScope.prefab";
        public const string HeroPrefab = Prefabs + "/Hero.prefab";

        public const string GameplayConfig = Configs + "/GameplayConfig.asset";
        public const string TowerSet = Configs + "/TowerSet.asset";
        public const string LevelCatalog = LevelConfigs + "/LevelCatalog.asset";
        public const string EventConfigs = Configs + "/Events";
        public const string EventsConfig = EventConfigs + "/EventsConfig.asset";

        public const string MenuScene = Scenes + "/Menu.unity";
        public const string GameScene = Scenes + "/Game.unity";

        /// <summary>Build order: the first scene is the one the player boots into.</summary>
        public static readonly string[] BuildScenes = { MenuScene, GameScene };

        public const string AndroidBuild = "Builds/GodTower.apk";

        public static string LevelConfig(int number) => $"{LevelConfigs}/Level_{number:00}.asset";
    }
}
