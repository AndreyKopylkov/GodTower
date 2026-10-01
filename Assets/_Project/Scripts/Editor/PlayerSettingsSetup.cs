using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace GodTower.Editor
{
    /// <summary>Applies the locked Player/Quality settings for the Android deliverable (see Docs/Plan.md §3).</summary>
    public static class PlayerSettingsSetup
    {
        public const string ApplicationId = "com.andreykopylkov.godtower";
        public const string ProductName = "God Tower";
        public const string CompanyName = "Andrey Kopylkov";
        public const string Version = "1.0.0";
        private const string MobileRenderPipelineAsset = "Mobile_RPAsset";
        private const string AppIcon = "Assets/Art/UI/Logo/app_icon_1024.png";

        public static void Apply()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, ApplicationId);

            ApplyPortraitOrientation();
            ApplyIcon();
            ApplyAndroid();
            ApplyAndroidQualityLevel();
        }

        /// <summary>App icon from lane I (1024² RGB, full bleed): default icon for every platform, Android scales it down.</summary>
        private static void ApplyIcon()
        {
            var importer = AssetImporter.GetAtPath(AppIcon) as TextureImporter;
            if (importer == null)
                return;

            if (importer.textureType != TextureImporterType.Default || importer.mipmapEnabled || importer.maxTextureSize != 1024)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIcon);
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        private static void ApplyPortraitOrientation()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
        }

        private static void ApplyAndroid()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;

            // The /bump webhook opens a TCP socket, which needs the INTERNET permission.
            PlayerSettings.Android.forceInternetPermission = true;
            PlayerSettings.Android.optimizedFramePacing = true;

            PlayerSettings.Android.textureCompressionFormats = new[] { TextureCompressionFormat.ASTC };
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        }

        /// <summary>Makes the quality level that uses <c>Mobile_RPAsset</c> the Android default.</summary>
        private static void ApplyAndroidQualityLevel()
        {
            int mobileLevel = FindQualityLevelUsing(MobileRenderPipelineAsset);
            if (mobileLevel < 0)
                throw new InvalidOperationException($"No quality level uses '{MobileRenderPipelineAsset}'.");

            var qualitySettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            SerializedProperty perPlatform = qualitySettings.FindProperty("m_PerPlatformDefaultQuality");
            for (int i = 0; i < perPlatform.arraySize; i++)
            {
                SerializedProperty entry = perPlatform.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue == "Android")
                    entry.FindPropertyRelative("second").intValue = mobileLevel;
            }

            qualitySettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int FindQualityLevelUsing(string renderPipelineAssetName)
        {
            for (int level = 0; level < QualitySettings.names.Length; level++)
            {
                RenderPipelineAsset asset = QualitySettings.GetRenderPipelineAssetAt(level);
                if (asset != null && asset.name == renderPipelineAssetName)
                    return level;
            }

            return -1;
        }
    }
}
