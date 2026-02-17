// ============================================================================
// LightingController.cs — Dynamic lighting system for horror atmosphere
// Controls light flicker, color temperature shifts, shadow exaggeration,
// intensity transitions, slow pulses, and selective darkness.
// Atmosphere priority — lighting is the primary tension tool.
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FracturedEchoes.Core.Events;

namespace FracturedEchoes.Lighting
{
    /// <summary>
    /// Dynamic lighting controller for psychological horror atmosphere.
    /// Manages individual lights with flicker patterns, color shifts,
    /// intensity pulses, and coordinated group transitions.
    /// </summary>
    public class LightingController : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Managed Lights")]
        [Tooltip("All controllable lights in this location.")]
        [SerializeField] private LightProfile[] _lightProfiles;

        [Header("Global Settings")]
        [Tooltip("Master intensity multiplier for all managed lights.")]
        [Range(0f, 2f)]
        [SerializeField] private float _masterIntensity = 1f;

        [Tooltip("Global color temperature offset (negative = cooler, positive = warmer).")]
        [Range(-1f, 1f)]
        [SerializeField] private float _temperatureShift = 0f;

        [Header("Flicker Presets")]
        [SerializeField] private FlickerPreset _subtleFlicker;
        [SerializeField] private FlickerPreset _aggressiveFlicker;
        [SerializeField] private FlickerPreset _dyingBulbFlicker;

        [Header("Events")]
        [SerializeField] private GameEvent _onBlackout;
        [SerializeField] private GameEvent _onLightsRestored;

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private Dictionary<string, LightProfile> _lightLookup = new Dictionary<string, LightProfile>();
        private Dictionary<Light, Coroutine> _activeFlickers = new Dictionary<Light, Coroutine>();
        private Dictionary<Light, Coroutine> _activePulses = new Dictionary<Light, Coroutine>();
        private float _lastAppliedTempShift = float.MinValue;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            // Build lookup
            if (_lightProfiles != null)
            {
                foreach (var profile in _lightProfiles)
                {
                    if (!string.IsNullOrEmpty(profile.lightID))
                    {
                        _lightLookup[profile.lightID] = profile;
                    }

                    // Store original values
                    if (profile.light != null)
                    {
                        profile.originalIntensity = profile.light.intensity;
                        profile.originalColor = profile.light.color;
                    }
                }
            }
        }

        private void Update()
        {
            // Apply global settings
            ApplyGlobalSettings();
        }

        // =====================================================================
        // GLOBAL CONTROLS
        // =====================================================================

        private void ApplyGlobalSettings()
        {
            // Skip if temperature hasn't changed
            if (Mathf.Approximately(_temperatureShift, _lastAppliedTempShift)) return;
            _lastAppliedTempShift = _temperatureShift;

            // Apply temperature shift to all lights
            if (Mathf.Abs(_temperatureShift) > 0.01f)
            {
                foreach (var profile in _lightProfiles)
                {
                    if (profile.light == null || profile.isFlickering) continue;

                    Color tempColor = _temperatureShift > 0
                        ? Color.Lerp(profile.originalColor, new Color(1f, 0.85f, 0.7f), _temperatureShift)
                        : Color.Lerp(profile.originalColor, new Color(0.7f, 0.85f, 1f), -_temperatureShift);

                    profile.light.color = tempColor;
                }
            }
            else
            {
                // Reset all to original color when shift is ~0
                foreach (var profile in _lightProfiles)
                {
                    if (profile.light != null && !profile.isFlickering)
                        profile.light.color = profile.originalColor;
                }
            }
        }

        /// <summary>
        /// Sets the global temperature shift. Negative = colder (blue), Positive = warmer (orange).
        /// Slowly shifting to colder tones creates psychological unease.
        /// </summary>
        public void SetTemperatureShift(float shift)
        {
            _temperatureShift = Mathf.Clamp(shift, -1f, 1f);
        }

        /// <summary>
        /// Gradually shifts temperature over time (for slow tension build).
        /// </summary>
        public void ShiftTemperatureOverTime(float targetShift, float duration)
        {
            StartCoroutine(TemperatureShiftRoutine(targetShift, duration));
        }

        private IEnumerator TemperatureShiftRoutine(float targetShift, float duration)
        {
            float startShift = _temperatureShift;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _temperatureShift = Mathf.Lerp(startShift, targetShift, elapsed / duration);
                yield return null;
            }

            _temperatureShift = targetShift;
        }

        // =====================================================================
        // FLICKER CONTROL
        // =====================================================================

        /// <summary>
        /// Starts a flicker effect on a specific light by ID.
        /// </summary>
        public void StartFlicker(string lightID, FlickerPreset preset = null)
        {
            if (!_lightLookup.TryGetValue(lightID, out var profile)) return;
            if (profile.light == null) return;

            FlickerPreset usePreset = preset ?? _subtleFlicker;
            StartFlickerOnLight(profile, usePreset);
        }

        /// <summary>
        /// Stops a flicker effect on a specific light.
        /// </summary>
        public void StopFlicker(string lightID)
        {
            if (!_lightLookup.TryGetValue(lightID, out var profile)) return;
            if (profile.light == null) return;

            StopFlickerOnLight(profile);
        }

        /// <summary>
        /// Starts flicker on all managed lights.
        /// </summary>
        public void StartAllFlickers(FlickerPreset preset = null)
        {
            foreach (var profile in _lightProfiles)
            {
                if (profile.light != null)
                {
                    StartFlickerOnLight(profile, preset ?? _subtleFlicker);
                }
            }
        }

        /// <summary>
        /// Stops all active flickers.
        /// </summary>
        public void StopAllFlickers()
        {
            foreach (var profile in _lightProfiles)
            {
                if (profile.light != null && profile.isFlickering)
                {
                    StopFlickerOnLight(profile);
                }
            }
        }

        private void StartFlickerOnLight(LightProfile profile, FlickerPreset preset)
        {
            if (profile.isFlickering) StopFlickerOnLight(profile);

            profile.isFlickering = true;
            Coroutine routine = StartCoroutine(FlickerRoutine(profile, preset));
            _activeFlickers[profile.light] = routine;
        }

        private void StopFlickerOnLight(LightProfile profile)
        {
            profile.isFlickering = false;

            if (_activeFlickers.TryGetValue(profile.light, out var routine))
            {
                StopCoroutine(routine);
                _activeFlickers.Remove(profile.light);
            }

            // Restore original intensity
            profile.light.intensity = profile.originalIntensity * _masterIntensity;
            profile.light.color = profile.originalColor;
        }

        private IEnumerator FlickerRoutine(LightProfile profile, FlickerPreset preset)
        {
            while (profile.isFlickering)
            {
                // Random intensity variation
                float flickerIntensity = profile.originalIntensity *
                    UnityEngine.Random.Range(preset.minIntensityMultiplier, preset.maxIntensityMultiplier) *
                    _masterIntensity;

                profile.light.intensity = flickerIntensity;

                // Occasional complete blackout
                if (UnityEngine.Random.value < preset.blackoutChance)
                {
                    profile.light.intensity = 0f;
                    yield return new WaitForSeconds(UnityEngine.Random.Range(preset.blackoutMinDuration, preset.blackoutMaxDuration));
                    profile.light.intensity = flickerIntensity;
                }

                // Wait for next flicker
                float waitTime = UnityEngine.Random.Range(preset.minInterval, preset.maxInterval);
                yield return new WaitForSeconds(waitTime);
            }
        }

        // =====================================================================
        // INTENSITY PULSE (Slow breathing light)
        // =====================================================================

        /// <summary>
        /// Starts a slow intensity pulse on a light (breathing effect).
        /// Creates subtle unease when used on ambient lights.
        /// </summary>
        public void StartPulse(string lightID, float minIntensity, float maxIntensity, float cycleTime)
        {
            if (!_lightLookup.TryGetValue(lightID, out var profile)) return;
            if (profile.light == null) return;

            StopPulse(lightID);

            Coroutine routine = StartCoroutine(PulseRoutine(profile, minIntensity, maxIntensity, cycleTime));
            _activePulses[profile.light] = routine;
        }

        /// <summary>
        /// Stops a pulse effect on a specific light.
        /// </summary>
        public void StopPulse(string lightID)
        {
            if (!_lightLookup.TryGetValue(lightID, out var profile)) return;
            if (profile.light == null) return;

            if (_activePulses.TryGetValue(profile.light, out var routine))
            {
                StopCoroutine(routine);
                _activePulses.Remove(profile.light);
                profile.light.intensity = profile.originalIntensity * _masterIntensity;
            }
        }

        private IEnumerator PulseRoutine(LightProfile profile, float minIntensity, float maxIntensity, float cycleTime)
        {
            float timer = 0f;

            while (true)
            {
                timer += Time.deltaTime;
                float t = (Mathf.Sin(timer * Mathf.PI * 2f / cycleTime) + 1f) * 0.5f;
                profile.light.intensity = Mathf.Lerp(minIntensity, maxIntensity, t) * _masterIntensity;
                yield return null;
            }
        }

        // =====================================================================
        // INTENSITY TRANSITIONS
        // =====================================================================

        /// <summary>
        /// Smoothly transitions a light's intensity to a target value.
        /// </summary>
        public void TransitionIntensity(string lightID, float targetIntensity, float duration)
        {
            if (!_lightLookup.TryGetValue(lightID, out var profile)) return;
            if (profile.light == null) return;

            StartCoroutine(IntensityTransitionRoutine(profile.light, targetIntensity * _masterIntensity, duration));
        }

        /// <summary>
        /// Transitions all managed lights to a target intensity.
        /// </summary>
        public void TransitionAllIntensities(float targetMultiplier, float duration)
        {
            foreach (var profile in _lightProfiles)
            {
                if (profile.light != null)
                {
                    float target = profile.originalIntensity * targetMultiplier * _masterIntensity;
                    StartCoroutine(IntensityTransitionRoutine(profile.light, target, duration));
                }
            }
        }

        private IEnumerator IntensityTransitionRoutine(Light light, float targetIntensity, float duration)
        {
            float startIntensity = light.intensity;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t); // Smooth step
                light.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
                yield return null;
            }

            light.intensity = targetIntensity;
        }

        // =====================================================================
        // BLACKOUT / SELECTIVE DARKNESS
        // =====================================================================

        /// <summary>
        /// Triggers a full blackout (all lights off).
        /// </summary>
        public void TriggerBlackout(float fadeDuration)
        {
            TransitionAllIntensities(0f, fadeDuration);
            _onBlackout?.Raise();
        }

        /// <summary>
        /// Restores all lights to their original intensity.
        /// </summary>
        public void RestoreLights(float fadeDuration)
        {
            foreach (var profile in _lightProfiles)
            {
                if (profile.light != null)
                {
                    StartCoroutine(IntensityTransitionRoutine(
                        profile.light,
                        profile.originalIntensity * _masterIntensity,
                        fadeDuration
                    ));
                }
            }

            _onLightsRestored?.Raise();
        }

        /// <summary>
        /// Creates selective darkness by turning off specific lights.
        /// Leaves others on to create isolation and vulnerability.
        /// </summary>
        public void SelectiveDarkness(string[] lightIDsToKeep, float fadeDuration)
        {
            HashSet<string> keepSet = new HashSet<string>(lightIDsToKeep);

            foreach (var profile in _lightProfiles)
            {
                if (profile.light == null) continue;

                if (!keepSet.Contains(profile.lightID))
                {
                    StartCoroutine(IntensityTransitionRoutine(profile.light, 0f, fadeDuration));
                }
            }
        }

        /// <summary>
        /// Sets the master intensity multiplier.
        /// </summary>
        public void SetMasterIntensity(float intensity)
        {
            _masterIntensity = Mathf.Clamp(intensity, 0f, 2f);
        }

        // =====================================================================
        // COLOR TRANSITIONS
        // =====================================================================

        /// <summary>
        /// Transitions a light's color smoothly.
        /// </summary>
        public void TransitionColor(string lightID, Color targetColor, float duration)
        {
            if (!_lightLookup.TryGetValue(lightID, out var profile)) return;
            if (profile.light == null) return;

            StartCoroutine(ColorTransitionRoutine(profile, targetColor, duration));
        }

        private IEnumerator ColorTransitionRoutine(LightProfile profile, Color targetColor, float duration)
        {
            Color startColor = profile.light.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                profile.light.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            profile.light.color = targetColor;
        }
    }

    // =========================================================================
    // DATA STRUCTURES
    // =========================================================================

    /// <summary>
    /// Profile for a managed light. Pairs a Light component with an ID
    /// and stores original values for restoration.
    /// </summary>
    [Serializable]
    public class LightProfile
    {
        [Tooltip("Unique identifier for this light.")]
        public string lightID;

        [Tooltip("The Light component to control.")]
        public Light light;

        [Tooltip("Light group for coordinated effects.")]
        public string group;

        // Runtime state (not serialized to Inspector)
        [HideInInspector] public float originalIntensity;
        [HideInInspector] public Color originalColor;
        [HideInInspector] public bool isFlickering;
    }

    /// <summary>
    /// Defines a flicker behavior pattern.
    /// Create different presets for subtle, aggressive, dying bulb effects.
    /// </summary>
    [Serializable]
    public class FlickerPreset
    {
        [Tooltip("Name of this preset.")]
        public string presetName = "Default";

        [Header("Intensity")]
        [Range(0f, 1f)] public float minIntensityMultiplier = 0.3f;
        [Range(0f, 2f)] public float maxIntensityMultiplier = 1.2f;

        [Header("Timing")]
        public float minInterval = 0.02f;
        public float maxInterval = 0.15f;

        [Header("Blackout")]
        [Range(0f, 1f)] public float blackoutChance = 0.05f;
        public float blackoutMinDuration = 0.05f;
        public float blackoutMaxDuration = 0.3f;
    }
}
