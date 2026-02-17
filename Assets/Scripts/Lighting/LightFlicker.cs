// ============================================================================
// LightFlicker.cs — Lightweight per-light flicker component
// Attach to any GameObject with a Light to get independent, configurable
// flicker behaviour without relying on the centralised LightingController.
// Supports always-on ambient flicker, one-shot bursts, and trigger-based
// activation via public API.
// ============================================================================

using System.Collections;
using UnityEngine;

namespace FracturedEchoes.Lighting
{
    /// <summary>
    /// Self-contained light flicker.  Useful for candles, broken fluorescents,
    /// lanterns — anything that should flicker independently of the global
    /// <see cref="LightingController"/>.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Behaviour")]
        [Tooltip("Start flickering automatically on enable.")]
        [SerializeField] private bool _flickerOnEnable = true;

        [Tooltip("Loop the flicker indefinitely.  If false, plays once for Duration then stops.")]
        [SerializeField] private bool _loop = true;

        [Header("Intensity")]
        [Tooltip("Minimum multiplier applied to the light's base intensity.")]
        [SerializeField, Range(0f, 1f)] private float _minIntensity = 0.3f;

        [Tooltip("Maximum multiplier applied to the light's base intensity.")]
        [SerializeField, Range(0.5f, 2f)] private float _maxIntensity = 1.1f;

        [Header("Timing")]
        [Tooltip("Minimum wait between intensity changes (seconds).")]
        [SerializeField] private float _minInterval = 0.03f;

        [Tooltip("Maximum wait between intensity changes (seconds).")]
        [SerializeField] private float _maxInterval = 0.12f;

        [Tooltip("Total flicker duration when not looping (seconds).")]
        [SerializeField] private float _duration = 3f;

        [Header("Smoothing")]
        [Tooltip("Lerp speed toward new intensity.  Higher = snappier.")]
        [SerializeField] private float _smoothSpeed = 12f;

        [Header("Colour (optional)")]
        [Tooltip("Shift colour temperature during flicker.")]
        [SerializeField] private bool _colourShift;

        [SerializeField] private Color _warmTint = new Color(1f, 0.85f, 0.6f);
        [SerializeField] private Color _coolTint = Color.white;

        // =====================================================================
        // RUNTIME STATE
        // =====================================================================

        private Light _light;
        private float _baseIntensity;
        private Color _baseColour;
        private float _targetIntensity;
        private Coroutine _flickerCoroutine;

        /// <summary>True while a flicker routine is actively running.</summary>
        public bool IsFlickering => _flickerCoroutine != null;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            _light = GetComponent<Light>();
            _baseIntensity = _light.intensity;
            _baseColour = _light.color;
            _targetIntensity = _baseIntensity;
        }

        private void OnEnable()
        {
            if (_flickerOnEnable)
            {
                StartFlicker();
            }
        }

        private void OnDisable()
        {
            StopFlicker();
        }

        private void Update()
        {
            // Smooth toward the target intensity every frame
            if (_light != null && !Mathf.Approximately(_light.intensity, _targetIntensity))
            {
                _light.intensity = Mathf.Lerp(_light.intensity, _targetIntensity, Time.deltaTime * _smoothSpeed);
            }
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>Start (or restart) the flicker.</summary>
        public void StartFlicker()
        {
            StopFlicker();
            _flickerCoroutine = StartCoroutine(FlickerRoutine());
        }

        /// <summary>Start a one-shot flicker for the given duration.</summary>
        public void StartFlicker(float duration)
        {
            StopFlicker();
            _flickerCoroutine = StartCoroutine(FlickerRoutine(duration, false));
        }

        /// <summary>Stop flickering and restore original intensity/colour.</summary>
        public void StopFlicker()
        {
            if (_flickerCoroutine != null)
            {
                StopCoroutine(_flickerCoroutine);
                _flickerCoroutine = null;
            }

            _targetIntensity = _baseIntensity;

            if (_light != null)
            {
                _light.intensity = _baseIntensity;
                _light.color = _baseColour;
            }
        }

        /// <summary>Update the stored base intensity (e.g. when dimming a lamp).</summary>
        public void SetBaseIntensity(float newBase)
        {
            _baseIntensity = newBase;
        }

        // =====================================================================
        // COROUTINE
        // =====================================================================

        private IEnumerator FlickerRoutine()
        {
            yield return FlickerRoutine(_duration, _loop);
        }

        private IEnumerator FlickerRoutine(float duration, bool loop)
        {
            do
            {
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    float multiplier = Random.Range(_minIntensity, _maxIntensity);
                    _targetIntensity = _baseIntensity * multiplier;

                    if (_colourShift && _light != null)
                    {
                        float t = Mathf.InverseLerp(_minIntensity, _maxIntensity, multiplier);
                        _light.color = Color.Lerp(_warmTint, _coolTint, t);
                    }

                    float wait = Random.Range(_minInterval, _maxInterval);
                    yield return new WaitForSeconds(wait);
                    elapsed += wait;
                }
            }
            while (loop);

            // Non-looping: restore defaults
            _targetIntensity = _baseIntensity;
            if (_light != null) _light.color = _baseColour;
            _flickerCoroutine = null;
        }
    }
}
