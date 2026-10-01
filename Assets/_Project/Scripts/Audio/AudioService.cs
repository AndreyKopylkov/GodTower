using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace GodTower.Audio
{
    /// <summary>Plays the game's sound effects and music.</summary>
    public interface IAudioService
    {
        /// <summary>Fire-and-forget one-shot of a random variation of <paramref name="id"/>.</summary>
        void Play(SoundId id, float volumeScale = 1f);

        /// <summary>Starts a looping sound; dispose the handle to stop it.</summary>
        IDisposable StartLoop(SoundId id);

        /// <summary>Cross-fades to <paramref name="id"/> (no-op if it already plays).</summary>
        void PlayMusic(MusicId id);

        /// <summary>Pauses gameplay sounds (music and UI sounds keep playing).</summary>
        void SetGameplayPaused(bool paused);
    }

    /// <summary>
    /// Session-wide audio: a pool of 2D sources for one-shots, dedicated sources for loops, two music sources
    /// for cross-fades. Buses are plain volumes from the <see cref="AudioLibrary"/> (no AudioMixer asset — it cannot be
    /// authored from code). Gameplay pause uses <see cref="AudioListener.pause"/>; music and UI sources ignore it.
    /// </summary>
    public sealed class AudioService : IAudioService, IStartable, IDisposable
    {
        private const int OneShotVoices = 16;

        private readonly AudioLibrary _library;
        private readonly List<AudioSource> _voices = new();
        private readonly AudioSource[] _music = new AudioSource[2];
        private GameObject _root;
        private AudioSource _uiVoice;
        private int _nextVoice;
        private int _activeMusic;
        private MusicId _currentMusic = MusicId.None;

        public AudioService(AudioLibrary library) => _library = library;

        public MusicId CurrentMusic => _currentMusic;

        public void Start() => EnsureCreated();

        public void Play(SoundId id, float volumeScale = 1f)
        {
            SoundEntry entry = _library != null ? _library.Find(id) : null;
            AudioClip clip = entry?.PickClip();
            if (clip == null)
                return;

            EnsureCreated();
            AudioSource voice = entry.IsUi ? _uiVoice : NextVoice();
            voice.pitch = entry.PickPitch();
            voice.PlayOneShot(clip, entry.Volume * volumeScale * _library.SfxVolume);
        }

        public IDisposable StartLoop(SoundId id)
        {
            SoundEntry entry = _library != null ? _library.Find(id) : null;
            AudioClip clip = entry?.PickClip();
            if (clip == null)
                return LoopHandle.Empty;

            EnsureCreated();
            AudioSource source = CreateSource($"Loop_{id}", ignorePause: entry.IsUi);
            source.clip = clip;
            source.loop = true;
            source.volume = entry.Volume * _library.SfxVolume;
            source.pitch = entry.PickPitch();
            source.Play();
            return new LoopHandle(source);
        }

        public void PlayMusic(MusicId id)
        {
            if (id == _currentMusic || _library == null)
                return;

            EnsureCreated();
            _currentMusic = id;
            AudioSource from = _music[_activeMusic];
            _activeMusic = 1 - _activeMusic;
            AudioSource to = _music[_activeMusic];

            float fade = _library.MusicFade;
            Tween.StopAll(from);
            Tween.StopAll(to);
            if (from.isPlaying)
                Tween.AudioVolume(from, 0f, fade, useUnscaledTime: true).OnComplete(from, source => source.Stop());

            to.clip = _library.Music(id);
            if (to.clip == null)
                return;

            to.volume = 0f;
            to.Play();
            Tween.AudioVolume(to, _library.MusicVolume, fade, useUnscaledTime: true);
        }

        public void SetGameplayPaused(bool paused) => AudioListener.pause = paused;

        public void Dispose()
        {
            AudioListener.pause = false;
            if (_root == null)
                return;

            foreach (AudioSource source in _music)
                Tween.StopAll(source);
            Object.Destroy(_root);
            _root = null;
        }

        private void EnsureCreated()
        {
            if (_root != null)
                return;

            _root = new GameObject("Audio");
            Object.DontDestroyOnLoad(_root);
            _voices.Clear();
            for (int i = 0; i < OneShotVoices; i++)
                _voices.Add(CreateSource($"Voice_{i}", ignorePause: false));
            _uiVoice = CreateSource("Voice_UI", ignorePause: true);
            for (int i = 0; i < _music.Length; i++)
            {
                _music[i] = CreateSource($"Music_{i}", ignorePause: true);
                _music[i].loop = true;
            }
        }

        private AudioSource NextVoice()
        {
            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Count;
            return voice;
        }

        private AudioSource CreateSource(string name, bool ignorePause)
        {
            var source = new GameObject(name).AddComponent<AudioSource>();
            source.transform.SetParent(_root.transform, false);
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = ignorePause;
            return source;
        }

        private sealed class LoopHandle : IDisposable
        {
            public static readonly IDisposable Empty = new LoopHandle(null);

            private AudioSource _source;

            public LoopHandle(AudioSource source) => _source = source;

            public void Dispose()
            {
                if (_source != null)
                    Object.Destroy(_source.gameObject);
                _source = null;
            }
        }
    }
}
