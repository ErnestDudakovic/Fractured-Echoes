// ============================================================================
// AudioManager.cs — Layered ambient audio system
// Manages multiple audio layers that blend together for atmosphere.
// Supports: base ambient loop, random distant sounds, directional triggers,
// positional whispers, dynamic filtering, and silence as tension.
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.Core.Events;
using FracturedEchoes.ScriptableObjects;

namespace FracturedEchoes.Audio
{
    /// <summary>
    /// Central audio manager with layered ambient system.
    /// Manages base ambient loops, random environmental sounds,
    /// tension ramps, and psychological audio effects.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Layer Configuration")]
        [Tooltip("Audio layer data assets to manage.")]
        [SerializeField] private AudioLayerData[] _layerDefinitions;

        [Header("Random Sounds")]
        [Tooltip("Clips to play randomly at intervals (distant sounds, creaks, etc.).")]
        [SerializeField] private AudioClip[] _randomSounds;

        [Tooltip("Minimum interval between random sounds (seconds).")]
        [SerializeField] private float _randomSoundMinInterval = 10f;

        [Tooltip("Maximum interval between random sounds (seconds).")]
        [SerializeField] private float _randomSoundMaxInterval = 45f;

        [Tooltip("Volume range for random sounds.")]
        [SerializeField] private Vector2 _randomSoundVolume = new Vector2(0.1f, 0.4f);

        [Header("Tension System")]
        [Tooltip("Master volume multiplier for tension ramp (0–1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _tensionLevel = 0f;

        [Tooltip("Low-pass filter cutoff for stressed state.")]
        [SerializeField] private float _stressedLowPassCutoff = 800f;

        [Tooltip("Normal low-pass filter cutoff.")]
        [SerializeField] private float _normalLowPassCutoff = 22000f;

        [Header("Master")]
        [Range(0f, 1f)]
        [SerializeField] private float _masterVolume = 1f;

        [Header("Events")]
        [SerializeField] private GameEvent _onSilenceStart;
        [SerializeField] private GameEvent _onSilenceEnd;

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private Dictionary<string, AudioLayerInstance> _activeLayers = new Dictionary<string, AudioLayerInstance>();
        private AudioLowPassFilter _lowPassFilter;
        private AudioSource _2dSource;
        private float _randomSoundTimer;
        private bool _isSilenceActive;
        private Coroutine _silenceCoroutine;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            // Add low-pass filter for stress effects
            _lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
            _lowPassFilter.cutoffFrequency = _normalLowPassCutoff;

            // Dedicated 2D source for UI/narrative sounds
            _2dSource = gameObject.AddComponent<AudioSource>();
            _2dSource.spatialBlend = 0f;
            _2dSource.playOnAwake = false;

            // Initialize layers from definitions
            InitializeLayers();
        }

        private void Update()
        {
            // Update tension-based effects
            UpdateTensionEffects();

            // Handle random ambient sounds
            UpdateRandomSounds();

            // Update all layer volumes
            foreach (var layer in _activeLayers.Values)
            {
                layer.Update();
            }
        }

        // =====================================================================
        // LAYER MANAGEMENT
        // =====================================================================

        private void InitializeLayers()
        {
            if (_layerDefinitions == null) return;

            foreach (AudioLayerData layerData in _layerDefinitions)
            {
                if (layerData == null) continue;

                // Create AudioSource for each layer
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.clip = layerData.clip;
                source.loop = layerData.loop;
                source.volume = 0f; // Start silent
                source.playOnAwake = false;
                source.priority = layerData.priority;
                source.spatialBlend = layerData.is3D ? 1f : 0f;
                source.minDistance = layerData.minDistance;
                source.maxDistance = layerData.maxDistance;

                var instance = new AudioLayerInstance(layerData, source);
                _activeLayers[layerData.layerName] = instance;
            }
        }

        /// <summary>
        /// Starts playing a specific audio layer by name.
        /// </summary>
        public void PlayLayer(string layerName)
        {
            if (_activeLayers.TryGetValue(layerName, out var layer))
            {
                layer.Play();
                Debug.Log($"[Audio] Playing layer: {layerName}");
            }
            else
            {
                Debug.LogWarning($"[Audio] Layer not found: {layerName}");
            }
        }

        /// <summary>
        /// Stops a specific audio layer by name.
        /// </summary>
        public void StopLayer(string layerName)
        {
            if (_activeLayers.TryGetValue(layerName, out var layer))
            {
                layer.Stop();
                Debug.Log($"[Audio] Stopped layer: {layerName}");
            }
        }

        /// <summary>
        /// Fades a layer to a target volume over a duration.
        /// </summary>
        public void FadeLayer(string layerName, float targetVolume, float duration)
        {
            if (_activeLayers.TryGetValue(layerName, out var layer))
            {
                layer.FadeTo(targetVolume, duration);
            }
        }

        /// <summary>
        /// Stops all layers with a fade-out.
        /// </summary>
        public void StopAllLayers(float fadeDuration = 2f)
        {
            foreach (var layer in _activeLayers.Values)
            {
                layer.FadeTo(0f, fadeDuration);
            }
        }

        /// <summary>
        /// Crossfades between two layers.
        /// </summary>
        public void CrossfadeLayers(string fromLayer, string toLayer, float duration)
        {
            FadeLayer(fromLayer, 0f, duration);
            PlayLayer(toLayer);
            FadeLayer(toLayer, 1f, duration);
        }

        // =====================================================================
        // RANDOM AMBIENT SOUNDS
        // =====================================================================

        private void UpdateRandomSounds()
        {
            if (_randomSounds == null || _randomSounds.Length == 0) return;
            if (_isSilenceActive) return;

            _randomSoundTimer -= Time.deltaTime;

            if (_randomSoundTimer <= 0f)
            {
                PlayRandomSound();
                _randomSoundTimer = UnityEngine.Random.Range(_randomSoundMinInterval, _randomSoundMaxInterval);
            }
        }

        private void PlayRandomSound()
        {
            AudioClip clip = _randomSounds[UnityEngine.Random.Range(0, _randomSounds.Length)];
            float volume = UnityEngine.Random.Range(_randomSoundVolume.x, _randomSoundVolume.y) * _masterVolume;

            // Play at a random position around the listener for directionality
            Vector3 randomDir = UnityEngine.Random.insideUnitSphere.normalized * 10f;
            Vector3 playPosition = transform.position + randomDir;

            AudioSource.PlayClipAtPoint(clip, playPosition, volume);
        }

        // =====================================================================
        // TENSION SYSTEM
        // =====================================================================

        /// <summary>
        /// Sets the tension level (0–1). Affects low-pass filter and volume dynamics.
        /// </summary>
        public void SetTensionLevel(float level)
        {
            _tensionLevel = Mathf.Clamp01(level);
        }

        private void UpdateTensionEffects()
        {
            if (_lowPassFilter == null) return;

            // Dynamic low-pass filter — higher tension = more muffled
            float targetCutoff = Mathf.Lerp(_normalLowPassCutoff, _stressedLowPassCutoff, _tensionLevel);
            _lowPassFilter.cutoffFrequency = Mathf.Lerp(
                _lowPassFilter.cutoffFrequency,
                targetCutoff,
                Time.deltaTime * 2f
            );
        }

        // =====================================================================
        // SILENCE AS TENSION MECHANIC
        // =====================================================================

        /// <summary>
        /// Activates silence mode — fades all layers to near-zero.
        /// Silence is a powerful tension tool in horror.
        /// </summary>
        public void StartSilence(float fadeDuration, float silenceDuration)
        {
            if (_silenceCoroutine != null)
            {
                StopCoroutine(_silenceCoroutine);
            }

            _silenceCoroutine = StartCoroutine(SilenceRoutine(fadeDuration, silenceDuration));
        }

        private IEnumerator SilenceRoutine(float fadeDuration, float silenceDuration)
        {
            _isSilenceActive = true;
            _onSilenceStart?.Raise();

            // Store current volumes and fade down
            Dictionary<string, float> savedVolumes = new Dictionary<string, float>();
            foreach (var kvp in _activeLayers)
            {
                savedVolumes[kvp.Key] = kvp.Value.CurrentVolume;
                kvp.Value.FadeTo(0f, fadeDuration);
            }

            yield return new WaitForSeconds(fadeDuration + silenceDuration);

            // Restore volumes
            foreach (var kvp in savedVolumes)
            {
                if (_activeLayers.TryGetValue(kvp.Key, out var layer))
                {
                    layer.FadeTo(kvp.Value, fadeDuration);
                }
            }

            _isSilenceActive = false;
            _onSilenceEnd?.Raise();
        }

        // =====================================================================
        // ONE-SHOT SOUNDS
        // =====================================================================

        /// <summary>
        /// Plays a one-shot sound at a specific world position.
        /// Used for puzzle feedback, interaction sounds, etc.
        /// </summary>
        public void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, volume * _masterVolume);
        }

        /// <summary>
        /// Plays a 2D one-shot sound (UI, narrative, etc.).
        /// </summary>
        public void PlaySound2D(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            _2dSource.PlayOneShot(clip, volume * _masterVolume);
        }

        /// <summary>
        /// Sets the master volume.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
        }
    }

    // =========================================================================
    // AUDIO LAYER INSTANCE — Runtime wrapper for AudioLayerData
    // =========================================================================

    /// <summary>
    /// Runtime instance of an audio layer. Manages fade transitions
    /// and volume control for a single AudioSource.
    /// </summary>
    public class AudioLayerInstance : IAudioLayer
    {
        private readonly AudioLayerData _data;
        private readonly AudioSource _source;
        private float _targetVolume;
        private float _fadeSpeed;
        private bool _isFading;

        public AudioLayerInstance(AudioLayerData data, AudioSource source)
        {
            _data = data;
            _source = source;
        }

        public string LayerName => _data.layerName;

        public float Volume
        {
            get => _source.volume;
            set => _source.volume = value;
        }

        public float CurrentVolume => _source.volume;

        public bool IsPlaying => _source.isPlaying;

        public void Play()
        {
            if (!_source.isPlaying)
            {
                // Apply random pitch variation
                if (_data.pitchVariation > 0f)
                {
                    _source.pitch = 1f + UnityEngine.Random.Range(-_data.pitchVariation, _data.pitchVariation);
                }

                _source.Play();
            }

            FadeTo(_data.defaultVolume, _data.fadeInDuration);
        }

        public void Stop()
        {
            FadeTo(0f, _data.fadeOutDuration);
        }

        public void FadeTo(float targetVolume, float duration)
        {
            _targetVolume = Mathf.Clamp01(targetVolume);
            _fadeSpeed = duration > 0f ? 1f / duration : 100f;
            _isFading = true;
        }

        /// <summary>
        /// Must be called every frame to process fade transitions.
        /// </summary>
        public void Update()
        {
            if (!_isFading) return;

            _source.volume = Mathf.MoveTowards(_source.volume, _targetVolume, _fadeSpeed * Time.deltaTime);

            if (Mathf.Approximately(_source.volume, _targetVolume))
            {
                _source.volume = _targetVolume;
                _isFading = false;

                // Stop source if faded to zero
                if (_targetVolume <= 0f && _source.isPlaying)
                {
                    _source.Stop();
                }
            }
        }
    }
}
