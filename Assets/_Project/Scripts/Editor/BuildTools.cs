using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GodTower.Editor
{
    /// <summary>
    /// CLI entry points, e.g.
    /// <c>Unity.exe -batchmode -quit -projectPath . -executeMethod GodTower.Editor.BuildTools.SetupProject -logFile Logs/setup.log</c>.
    /// Failures exit the editor with code 1 so scripts can detect them.
    /// </summary>
    public static class BuildTools
    {
        /// <summary>
        /// Applies Player/Quality/input settings and the VContainer root, configures art imports,
        /// (re)generates configs, prefabs, scenes and the build list.
        /// </summary>
        [MenuItem("GodTower/Setup Project")]
        public static void SetupProject() => RunOrExit(() =>
        {
            PlayerSettingsSetup.Apply();
            VContainerRootSetup.Apply();
            InputSetup.Apply();
            TowerImportSetup.Apply();
            HeroImportSetup.Apply();
            HeroIconRenderer.Render();
            PropImportSetup.Apply();
            UiImportSetup.Apply();
            FontSetup.Apply();
            AudioSetup.Apply();
            SkySetup.Apply();
            TextureImportPolicy.Apply();
            RenderingSetup.Apply();

            GameAssetsBuilder.BuildLevels();
            GameAssetsBuilder.BuildGameplayConfig();
            GameAssetsBuilder.BuildTowerSet();
            GameAssetsBuilder.BuildEventsConfig();
            GameAssetsBuilder.BuildBumpEffectConfig();
            AssetDatabase.SaveAssets();

            SceneBuilder.BuildAll();
            AssetDatabase.SaveAssets();
            Debug.Log("[BuildTools] Project setup complete.");
        });

        /// <summary>Asset hygiene check (missing scripts/references, Asset Store demo content); exits with code 1 on problems.</summary>
        [MenuItem("GodTower/Validate Project")]
        public static void ValidateProject() => RunOrExit(() =>
        {
            IReadOnlyList<string> issues = ProjectValidator.Run();
            foreach (string issue in issues)
                Debug.LogError("[Validate] " + issue);
            if (issues.Count > 0)
                throw new InvalidOperationException($"Project validation found {issues.Count} problem(s).");

            Debug.Log("[BuildTools] Project validation passed.");
        });

        /// <summary>Builds <c>Builds/GodTower.apk</c>. Run with <c>-buildTarget Android</c> to avoid a platform switch inside the call.</summary>
        [MenuItem("GodTower/Build Android APK")]
        public static void BuildAndroid() => RunOrExit(() =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ProjectPaths.AndroidBuild));

            var options = new BuildPlayerOptions
            {
                scenes = ProjectPaths.BuildScenes,
                locationPathName = ProjectPaths.AndroidBuild,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Android build {summary.result} with {summary.totalErrors} error(s).");

            Debug.Log($"[BuildTools] Built {ProjectPaths.AndroidBuild} ({summary.totalSize / (1024f * 1024f):F1} MB).");
        });

        private static void RunOrExit(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                else
                    throw;
            }
        }
    }
}
