using System;
using System.Linq;
using GodTower.Effects;
using GodTower.Events;
using GodTower.Gameplay;
using GodTower.Levels;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GodTower.Editor
{
    /// <summary>
    /// Creates or updates the gameplay ScriptableObjects. The level table of Docs/Plan.md §1 lives here,
    /// so levels are reproducible from code; tune values here, not in the generated assets.
    /// </summary>
    public static class GameAssetsBuilder
    {
        /// <summary>Cartoon FX Remaster prefabs (gitignored Asset Store pack; references stay empty without it).</summary>
        public const string CfxrPrefabs = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/";

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

        /// <summary>Villain/hero presentation configs. Effects come from gitignored asset-store packs and may be missing.</summary>
        public static EventsConfig BuildEventsConfig()
        {
            const string cfxr = CfxrPrefabs;
            const string hovl = "Assets/Hovl Studio/Magic effects pack/Prefabs/";

            VillainEventConfig missile = BuildVillain(VillainKind.Missile, "Missile", "icon_missile",
                PropImportSetup.Missile, 2f, cfxr + "Explosions/CFXR Explosion 1.prefab", 1.6f, approach: 0.8f);
            VillainEventConfig truck = BuildVillain(VillainKind.Truck, "Truck", "icon_truck",
                PropImportSetup.Truck, 1.3f, cfxr + "Explosions/CFXR2 WW Explosion.prefab", 1.6f, approach: 0.9f);
            VillainEventConfig axes = BuildVillain(VillainKind.Axes, "Axes", "icon_axes",
                PropImportSetup.Axe, 1.3f, cfxr + "Impacts/CFXR Hit A (Red).prefab", 1.5f, approach: 0.8f);

            HeroEventConfig jetpack = BuildHero(HeroKind.Jetpack, "Jetpack", "icon_jetpack", 0.10f, 2f,
                PropImportSetup.Jetpack, new Vector3(0f, 1.15f, -0.2f), Vector3.zero, 1f,
                cfxr + "Fire/CFXR Fire.prefab", new Vector3(0f, 0.55f, -0.45f), 0.6f,
                cfxr + "Misc/CFXR Magic Poof.prefab", 1.5f, zoomOut: false);
            HeroEventConfig phoenix = BuildHero(HeroKind.Phoenix, "Phoenix", "icon_phoenix", 0.20f, 3f,
                PropImportSetup.PhoenixPrefab, new Vector3(0f, -2.4f, -1f), new Vector3(-90f, 0f, 0f), 2.5f,
                cfxr + "Fire/CFXR Fire.prefab", new Vector3(0f, -3.4f, -1f), 3f,
                hovl + "Hits and explosions/Explosion.prefab", 3f, zoomOut: true);

            EventsConfig config = LoadOrCreate<EventsConfig>(ProjectPaths.EventsConfig);
            var serialized = new SerializedObject(config);
            SetArray(Find(serialized, "_villains"), missile, truck, axes);
            SetArray(Find(serialized, "_heroes"), jetpack, phoenix);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        /// <summary>Webhook bump effect: glove model and asset-store bursts (missing packs leave the bursts empty).</summary>
        public static BumpEffectConfig BuildBumpEffectConfig()
        {
            const string cfxr = CfxrPrefabs;
            BumpEffectConfig config = LoadOrCreate<BumpEffectConfig>(ProjectPaths.BumpEffectConfig);
            var serialized = new SerializedObject(config);
            Find(serialized, "_glovePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PropImportSetup.BoxingGlove);
            Find(serialized, "_hitEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(cfxr + "Impacts/CFXR Hit D 3D (Yellow).prefab");
            Find(serialized, "_comicEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(cfxr + "Texts/CFXR _POW_.prefab");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static VillainEventConfig BuildVillain(VillainKind kind, string displayName, string icon, string prop,
            float propScale, string effect, float effectScale, float approach)
        {
            var config = LoadOrCreate<VillainEventConfig>($"{ProjectPaths.EventConfigs}/Villain_{kind}.asset");
            var serialized = new SerializedObject(config);
            Find(serialized, "_kind").enumValueIndex = (int)kind;
            Find(serialized, "_displayName").stringValue = displayName;
            Find(serialized, "_icon").objectReferenceValue = UiImportSetup.Sprite($"Icons/{icon}.png");
            Find(serialized, "_prop").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(prop);
            Find(serialized, "_propScale").floatValue = propScale;
            Find(serialized, "_impactEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(effect);
            Find(serialized, "_impactEffectScale").floatValue = effectScale;
            Find(serialized, "_approachDuration").floatValue = approach;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static HeroEventConfig BuildHero(HeroKind kind, string displayName, string icon, float heightFraction,
            float duration, string prop, Vector3 propOffset, Vector3 propEuler, float propScale,
            string trail, Vector3 trailOffset, float trailScale, string startEffect, float startScale, bool zoomOut)
        {
            var config = LoadOrCreate<HeroEventConfig>($"{ProjectPaths.EventConfigs}/Hero_{kind}.asset");
            var serialized = new SerializedObject(config);
            Find(serialized, "_kind").enumValueIndex = (int)kind;
            Find(serialized, "_displayName").stringValue = displayName;
            Find(serialized, "_icon").objectReferenceValue = UiImportSetup.Sprite($"Icons/{icon}.png");
            Find(serialized, "_heightFraction").floatValue = heightFraction;
            Find(serialized, "_duration").floatValue = duration;
            Find(serialized, "_prop").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(prop);
            Find(serialized, "_propOffset").vector3Value = propOffset;
            Find(serialized, "_propEuler").vector3Value = propEuler;
            Find(serialized, "_propScale").floatValue = propScale;
            Find(serialized, "_trailEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(trail);
            Find(serialized, "_trailOffset").vector3Value = trailOffset;
            Find(serialized, "_trailEffectScale").floatValue = trailScale;
            Find(serialized, "_startEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(startEffect);
            Find(serialized, "_startEffectScale").floatValue = startScale;
            Find(serialized, "_zoomOutCamera").boolValue = zoomOut;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static void SetArray(SerializedProperty array, params Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
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
