using System.Linq;
using GodTower.Scopes;
using UnityEditor;
using UnityEngine;
using VContainer.Unity;

namespace GodTower.Editor
{
    /// <summary>
    /// Creates the <see cref="RootLifetimeScope"/> prefab and a <see cref="VContainerSettings"/> asset pointing at it,
    /// and registers the settings as a preloaded asset so VContainer builds the root container at app launch.
    /// </summary>
    public static class VContainerRootSetup
    {
        public static void Apply()
        {
            LifetimeScope rootPrefab = LoadOrCreateRootPrefab();
            VContainerSettings settings = LoadOrCreateSettings();

            settings.RootLifetimeScope = rootPrefab;
            settings.RemoveClonePostfix = true;
            EditorUtility.SetDirty(settings);

            var preloaded = PlayerSettings.GetPreloadedAssets().Where(asset => asset != null && asset is not VContainerSettings).ToList();
            preloaded.Add(settings);
            PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
        }

        private static LifetimeScope LoadOrCreateRootPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RootLifetimeScope>(ProjectPaths.RootLifetimeScopePrefab);
            if (existing != null)
                return existing;

            AssetFolders.Ensure(ProjectPaths.Prefabs);
            var gameObject = new GameObject(nameof(RootLifetimeScope));
            gameObject.AddComponent<RootLifetimeScope>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, ProjectPaths.RootLifetimeScopePrefab);
            Object.DestroyImmediate(gameObject);
            return prefab.GetComponent<RootLifetimeScope>();
        }

        private static VContainerSettings LoadOrCreateSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<VContainerSettings>(ProjectPaths.VContainerSettings);
            if (existing != null)
                return existing;

            AssetFolders.Ensure(ProjectPaths.Configs);
            var settings = ScriptableObject.CreateInstance<VContainerSettings>();
            AssetDatabase.CreateAsset(settings, ProjectPaths.VContainerSettings);
            return settings;
        }
    }
}
