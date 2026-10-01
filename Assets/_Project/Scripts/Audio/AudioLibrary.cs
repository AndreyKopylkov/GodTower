using System;
using System.Linq;
using UnityEngine;

namespace GodTower.Audio
{
    /// <summary>One-shot and looping sound effects (Docs/Status/sound.md).</summary>
    public enum SoundId
    {
        Punch,
        Explosion,
        TruckCrash,
        AxeWhoosh,
        MissileFlyby,
        WarningBeep,
        Jetpack,
        Phoenix,
        ClimbStep,
        LaneShift,
        Fall,
        Win,
        Lose,
        CountdownTick,
        UiClick,
        UiLocked,
        UiOpen
    }

    public enum MusicId
    {
        None,
        Menu,
        Level
    }

    /// <summary>Clip variations and mix settings of one <see cref="SoundId"/>.</summary>
    [Serializable]
    public sealed class SoundEntry
    {
        [SerializeField] private SoundId _id;
        [SerializeField] private AudioClip[] _clips = Array.Empty<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;
        [Tooltip("Random pitch range (1 = original).")]
        [SerializeField] private Vector2 _pitch = Vector2.one;
        [Tooltip("UI sounds keep playing while the game (and its sound effects) is paused.")]
        [SerializeField] private bool _ui;

        public SoundId Id => _id;
        public float Volume => _volume;
        public bool IsUi => _ui;

        /// <summary>A random variation, or null when none is configured.</summary>
        public AudioClip PickClip() => _clips.Length == 0 ? null : _clips[UnityEngine.Random.Range(0, _clips.Length)];

        public float PickPitch() => UnityEngine.Random.Range(Mathf.Min(_pitch.x, _pitch.y), Mathf.Max(_pitch.x, _pitch.y));
    }

    /// <summary>All game audio: sound effects by id and the two music loops, with bus volumes.</summary>
    [CreateAssetMenu(menuName = "God Tower/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [SerializeField] private SoundEntry[] _sounds = Array.Empty<SoundEntry>();
        [SerializeField] private AudioClip _menuMusic;
        [SerializeField] private AudioClip _levelMusic;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;
        [SerializeField, Min(0f)] private float _musicFade = 0.6f;

        public float MusicVolume => _musicVolume;
        public float SfxVolume => _sfxVolume;
        public float MusicFade => _musicFade;

        /// <summary>The entry for <paramref name="id"/>, or null when it has no clips configured.</summary>
        public SoundEntry Find(SoundId id) => _sounds.FirstOrDefault(entry => entry != null && entry.Id == id);

        public AudioClip Music(MusicId id) => id switch
        {
            MusicId.Menu => _menuMusic,
            MusicId.Level => _levelMusic,
            _ => null
        };
    }
}
