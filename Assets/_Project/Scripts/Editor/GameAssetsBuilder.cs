using System;
using System.Linq;
using GodTower.Events;
using GodTower.Gameplay;
using GodTower.Levels;
using UnityEditor;
using UnityEngine;

namespace GodTower.Editor
{
    /// <summary>
    /// Creates or updates the gameplay ScriptableObjects. The level table of Docs/Plan.md §1 lives here,
    /// so levels are reproducible from code; tune values here, not in the generated assets.
    /// </summary>
    public static class GameAssetsBuilder
    {
        private static readonly VillainKind[] AllVillains = { VillainKind.Missile, VillainKind.Truck, VillainKind.Axes };

        /// <summary>Docs/Plan.md §1 level table; hero event times chosen to land mid-level.</summary>
        private static readonly LevelDefinition[] Levels =
        {
            new(1, 300f, 75f, new[] { VillainKind.Missile }, 8f, 1.0f, false, new HeroEventEntry(HeroKind.Jetpack, 30f)),
            new(2, 400f, 85f, new[] { VillainKind.Missile, VillainKind.Truck }, 7f, 0.9f, false, new HeroEventEntry(HeroKind.Jetpack, 35f)),
            new(3, 500f, 95f, AllVillains, 6f, 0.8f, false,
                new HeroEventEntry(HeroKind.Jetpack, 30f), new HeroEventEntry(HeroKind.Phoenix, 60f)),
            new(4, 600f, 105f, AllVillains, 5f, 0.7f, false, new HeroEventEntry(HeroKind.Phoenix, 50f)),
            new(5, 750f, 120f, AllVillains, 4f, 0.6f, true,
                new HeroEventEntry(HeroKind.Jetpack, 35f), new HeroEventEntry(HeroKind.Phoenix, 75f))
        };

        public static GameplayConfig BuildGameplayConfig() => LoadOrCreate<GameplayConfig>(ProjectPaths.GameplayConfig);

        public static LevelCatalog BuildLevels()
        {
            AssetFolders.Ensure(ProjectPaths.LevelConfigs);
            LevelConfig[] levels = Levels.Select(BuildLevel).ToArray();

            LevelCatalog catalog = LoadOrCreate<LevelCatalog>(ProjectPaths.LevelCatalog);
            var serialized = new SerializedObject(catalog);
            SerializedProperty list = Find(serialized, "_levels");
            list.arraySize = levels.Length;
            for (int i = 0; i < levels.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        public static TowerSet BuildTowerSet()
        {
            TowerSet set = LoadOrCreate<TowerSet>(ProjectPaths.TowerSet);
            var serialized = new SerializedObject(set);

            Material placeholder = MaterialFactory.Lit(ProjectPaths.Materials + "/Environment/M_TowerPlaceholder.mat",
                new Color(0.71f, 0.74f, 0.64f), smoothness: 0.1f);
            Find(serialized, "_placeholderMaterial").objectReferenceValue = placeholder;

            bool hasModels = TowerImportSetup.IsAvailable;
            SerializedProperty segments = Find(serialized, "_segments");
            segments.arraySize = hasModels ? TowerImportSetup.SegmentModels.Length : 0;
            for (int i = 0; i < segments.arraySize; i++)
                segments.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(TowerImportSetup.SegmentModels[i]);

            Find(serialized, "_top").objectReferenceValue =
                hasModels ? AssetDatabase.LoadAssetAtPath<GameObject>(TowerImportSetup.TopModel) : null;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return set;
        }

        private static LevelConfig BuildLevel(LevelDefinition definition)
        {
            LevelConfig level = LoadOrCreate<LevelConfig>(ProjectPaths.LevelConfig(definition.Number));
            var serialized = new SerializedObject(level);
            Find(serialized, "_number").intValue = definition.Number;
            Find(serialized, "_towerHeight").floatValue = definition.TowerHeight;
            Find(serialized, "_timeLimit").floatValue = definition.TimeLimit;
            Find(serialized, "_villainInterval").floatValue = definition.VillainInterval;
            Find(serialized, "_telegraphDuration").floatValue = definition.Telegraph;
            Find(serialized, "_backToBackPairs").boolValue = definition.BackToBackPairs;
            Find(serialized, "_seed").intValue = 1000 * definition.Number + 7;

            SerializedProperty villains = Find(serialized, "_villains");
            villains.arraySize = definition.Villains.Length;
            for (int i = 0; i < definition.Villains.Length; i++)
                villains.GetArrayElementAtIndex(i).enumValueIndex = (int)definition.Villains[i];

            SerializedProperty heroes = Find(serialized, "_heroEvents");
            heroes.arraySize = definition.HeroEvents.Length;
            for (int i = 0; i < definition.HeroEvents.Length; i++)
            {
                SerializedProperty entry = heroes.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_kind").enumValueIndex = (int)definition.HeroEvents[i].Kind;
                entry.FindPropertyRelative("_time").floatValue = definition.HeroEvents[i].Time;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return level;
        }

        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            AssetFolders.Ensure(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static SerializedProperty Find(SerializedObject serialized, string propertyPath) =>
            serialized.FindProperty(propertyPath)
            ?? throw new InvalidOperationException($"{serialized.targetObject.GetType().Name} has no serialized field '{propertyPath}'.");

        private readonly struct LevelDefinition
        {
            public readonly int Number;
            public readonly float TowerHeight;
            public readonly float TimeLimit;
            public readonly VillainKind[] Villains;
            public readonly float VillainInterval;
            public readonly float Telegraph;
            public readonly bool BackToBackPairs;
            public readonly HeroEventEntry[] HeroEvents;

            public LevelDefinition(int number, float towerHeight, float timeLimit, VillainKind[] villains,
                float villainInterval, float telegraph, bool backToBackPairs, params HeroEventEntry[] heroEvents)
            {
                Number = number;
                TowerHeight = towerHeight;
                TimeLimit = timeLimit;
                Villains = villains;
                VillainInterval = villainInterval;
                Telegraph = telegraph;
                BackToBackPairs = backToBackPairs;
                HeroEvents = heroEvents;
            }
        }
    }
}
