using System.Linq;
using GodTower.Audio;
using GodTower.Scopes;
using UnityEditor;
using UnityEngine;

namespace GodTower.Editor
{
    /// <summary>
    /// Import settings for lane S's clips (Docs/Status/sound.md) and the <see cref="AudioLibrary"/> mapping them to
    /// <see cref="SoundId"/>s; the library is assigned to the root scope prefab. Music streams, short effects are
    /// decompressed on load (mono). Looping (music, jetpack) is set on the playing <see cref="AudioSource"/> by
    /// <see cref="AudioService"/> — Unity has no per-clip loop flag for WAV imports.
    /// </summary>
    public static class AudioSetup
    {
        public const string Folder = ProjectPaths.Root + "/Audio/Generated";
        public const string LibraryPath = ProjectPaths.Configs + "/AudioLibrary.asset";

        private static readonly Sound[] Sounds =
        {
            new(SoundId.Punch, 1f, 0.9f, 1.15f, false, "sfx_punch_01", "sfx_punch_02", "sfx_punch_03"),
            new(SoundId.Explosion, 0.9f, 0.95f, 1.05f, false, "sfx_explosion_01", "sfx_explosion_02"),
            new(SoundId.TruckCrash, 0.9f, 0.95f, 1.05f, false, "sfx_truck_crash"),
            new(SoundId.AxeWhoosh, 0.85f, 0.9f, 1.1f, false, "sfx_axe_whoosh"),
            new(SoundId.MissileFlyby, 0.7f, 0.95f, 1.05f, false, "sfx_missile_flyby"),
            new(SoundId.WarningBeep, 0.8f, 1f, 1f, false, "sfx_warning_beep"),
            new(SoundId.Jetpack, 0.7f, 1f, 1f, false, "sfx_jetpack"),
            new(SoundId.Phoenix, 0.9f, 1f, 1f, false, "sfx_phoenix"),
            new(SoundId.ClimbStep, 0.35f, 0.9f, 1.1f, false, "sfx_climb_step_01", "sfx_climb_step_02", "sfx_climb_step_03"),
            new(SoundId.LaneShift, 0.7f, 0.95f, 1.08f, false, "sfx_lane_shift"),
            new(SoundId.Fall, 0.7f, 0.95f, 1.05f, false, "sfx_fall"),
            new(SoundId.Win, 1f, 1f, 1f, true, "sfx_win"),
            new(SoundId.Lose, 1f, 1f, 1f, true, "sfx_lose"),
            new(SoundId.CountdownTick, 0.7f, 1f, 1f, false, "sfx_countdown_tick"),
            new(SoundId.UiClick, 0.8f, 0.97f, 1.03f, true, "ui_click"),
            new(SoundId.UiLocked, 0.9f, 1f, 1f, true, "ui_locked"),
            new(SoundId.UiOpen, 0.8f, 1f, 1f, true, "ui_open")
        };

        public static void Apply()
        {
            ConfigureImports();
            AudioLibrary library = BuildLibrary();
            AssignToRootScope(library);
        }

        private static void ConfigureImports()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                bool music = System.IO.Path.GetFileName(path).StartsWith("music_");

                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = music ? 0.6f : 0.8f;
                settings.preloadAudioData = !music;

                AudioImporterSampleSettings current = importer.defaultSampleSettings;
                bool same = current.loadType == settings.loadType && current.compressionFormat == settings.compressionFormat
                            && Mathf.Approximately(current.quality, settings.quality)
                            && current.preloadAudioData == settings.preloadAudioData && importer.forceToMono == !music;
                if (same)
                    continue;

                importer.defaultSampleSettings = settings;
                importer.forceToMono = !music;
                importer.SaveAndReimport();
            }
        }

        private static AudioLibrary BuildLibrary()
        {
            AudioLibrary library = GameAssetsBuilder.LoadOrCreate<AudioLibrary>(LibraryPath);
            var serialized = new SerializedObject(library);

            SerializedProperty sounds = GameAssetsBuilder.Find(serialized, "_sounds");
            sounds.arraySize = Sounds.Length;
            for (int i = 0; i < Sounds.Length; i++)
            {
                Sound sound = Sounds[i];
                SerializedProperty entry = sounds.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_id").enumValueIndex = (int)sound.Id;
                entry.FindPropertyRelative("_volume").floatValue = sound.Volume;
                entry.FindPropertyRelative("_pitch").vector2Value = sound.Pitch;
                entry.FindPropertyRelative("_ui").boolValue = sound.Ui;

                AudioClip[] clips = sound.Clips.Select(Clip).Where(clip => clip != null).ToArray();
                SerializedProperty clipArray = entry.FindPropertyRelative("_clips");
                clipArray.arraySize = clips.Length;
                for (int c = 0; c < clips.Length; c++)
                    clipArray.GetArrayElementAtIndex(c).objectReferenceValue = clips[c];
            }

            GameAssetsBuilder.Find(serialized, "_menuMusic").objectReferenceValue = Clip("music_menu_loop");
            GameAssetsBuilder.Find(serialized, "_levelMusic").objectReferenceValue = Clip("music_level_loop");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return library;
        }

        private static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{name}.wav");

        private static void AssignToRootScope(AudioLibrary library)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ProjectPaths.RootLifetimeScopePrefab);
            try
            {
                var serialized = new SerializedObject(root.GetComponent<RootLifetimeScope>());
                GameAssetsBuilder.Find(serialized, "_audio").objectReferenceValue = library;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, ProjectPaths.RootLifetimeScopePrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private readonly struct Sound
        {
            public readonly SoundId Id;
            public readonly float Volume;
            public readonly Vector2 Pitch;
            public readonly bool Ui;
            public readonly string[] Clips;

            public Sound(SoundId id, float volume, float minPitch, float maxPitch, bool ui, params string[] clips)
            {
                Id = id;
                Volume = volume;
                Pitch = new Vector2(minPitch, maxPitch);
                Ui = ui;
                Clips = clips;
            }
        }
    }
}
