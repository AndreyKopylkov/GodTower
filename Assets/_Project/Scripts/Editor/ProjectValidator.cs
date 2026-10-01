using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GodTower.Editor
{
    /// <summary>
    /// Asset hygiene (U-68): no missing scripts or broken object references in the build scenes, our prefabs and our
    /// configs, and nothing in the build pulled from Asset Store demo content (demo scenes, demo scripts/prefabs).
    /// Run from the CLI (<c>BuildTools.ValidateProject</c>) or through the EditMode test that calls <see cref="Run"/>.
    /// </summary>
    public static class ProjectValidator
    {
        /// <summary>Third-party packs whose demo folders must never be referenced.</summary>
        private static readonly string[] AssetStoreRoots = { "Assets/JMO Assets/", "Assets/Hovl Studio/" };

        /// <summary>Returns one line per problem; empty when the project is clean.</summary>
        public static IReadOnlyList<string> Run()
        {
            var issues = new List<string>();
            foreach (string scenePath in ProjectPaths.BuildScenes)
                CheckScene(scenePath, issues);

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { ProjectPaths.Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                CheckHierarchy(prefab, path, issues);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { ProjectPaths.Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path).Where(asset => asset is ScriptableObject))
                    CheckReferences(asset, path, issues);
            }

            CheckDemoContent(issues);
            return issues;
        }

        private static void CheckScene(string path, List<string> issues)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                    CheckHierarchy(root, path, issues);
            }
            finally
            {
                if (!wasLoaded)
                    EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        private static void CheckHierarchy(GameObject root, string context, List<string> issues)
        {
            if (root == null)
            {
                issues.Add($"{context}: could not be loaded.");
                return;
            }

            foreach (Transform node in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                GameObject gameObject = node.gameObject;
                int missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
                if (missingScripts > 0)
                    issues.Add($"{context}: '{PathOf(node)}' has {missingScripts} missing script(s).");

                foreach (Component component in gameObject.GetComponents<Component>())
                    if (component != null)
                        CheckReferences(component, $"{context}: '{PathOf(node)}'", issues);
            }
        }

        /// <summary>A reference is broken when it points at an object that no longer exists (stored id, null value).</summary>
        private static void CheckReferences(Object target, string context, List<string> issues)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.GetIterator();
            while (property.NextVisible(enterChildren: true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                if (property.objectReferenceValue == null && property.objectReferenceEntityIdValue.IsValid())
                    issues.Add($"{context} {target.GetType().Name}.{property.propertyPath}: missing reference.");
            }
        }

        /// <summary>Everything the build scenes depend on must come from outside Asset Store demo folders.</summary>
        private static void CheckDemoContent(List<string> issues)
        {
            foreach (string dependency in AssetDatabase.GetDependencies(ProjectPaths.BuildScenes, recursive: true))
            {
                if (!AssetStoreRoots.Any(root => dependency.StartsWith(root, StringComparison.Ordinal)))
                    continue;

                bool demo = dependency.IndexOf("demo", StringComparison.OrdinalIgnoreCase) >= 0;
                if (demo || dependency.EndsWith(".unity", StringComparison.Ordinal))
                    issues.Add($"Build depends on Asset Store demo content: {dependency}");
            }
        }

        private static string PathOf(Transform node) =>
            node.parent == null ? node.name : $"{PathOf(node.parent)}/{node.name}";
    }
}
