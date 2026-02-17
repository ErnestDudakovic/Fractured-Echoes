// ============================================================================
// LightFlickerAction.cs — Concrete EventAction: randomised light flicker
// Attach to a GameObject that has a Light component.  When the scripted
// event system triggers this action, the light flickers with configurable
// intensity range, frequency, and optional colour shifts.
// ============================================================================

using System.Collections;
using UnityEngine;

namespace FracturedEchoes.Environment
{
    /// <summary>
    /// Produces a randomised flicker on the attached <see cref="Light"/>.
    /// Designed to be used as an <see cref="EventAction"/> but can also be
    /// triggered manually via <see cref="PlayFlicker"/>.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LightFlickerAction : EventAction
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Flicker Settings")]
        [Tooltip("Minimum intensity multiplier during flicker.")]
        [SerializeField] private float _minIntensity = 0.05f;

        [Tooltip("Maximum intensity multiplier during flicker.")]
        [SerializeField] private float _maxIntensity = 1.2f;

        [Tooltip("Minimum interval between flicker changes (seconds).")]
        [SerializeField] private float _minInterval = 0.02f;

        [Tooltip("Maximum interval between flicker changes (seconds).")]
        [SerializeField] private float _maxInterval = 0.15f;

        [Header("Colour Shift (optional)")]
        [Tooltip("Enable subtle colour temperature shift during flicker.")]
        [SerializeField] private bool _enableColourShift;

        [Tooltip("Warm colour to shift toward during low-intensity flicker.")]
        [SerializeField] private Color _warmColour = new Color(1f, 0.85f, 0.6f);

        [Tooltip("Cool colour to shift toward during high-intensity flicker.")]
        [SerializeField] private Color _coolColour = Color.white;

        // =====================================================================
        // RUNTIME
        // =====================================================================

        private Light _light;
        private float _originalIntensity;
        private Color _originalColour;
        private Coroutine _activeFlicker;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _originalIntensity = _light.intensity;
            _originalColour = _light.color;
        }

        // =====================================================================
        // EventAction OVERRIDE
        // =====================================================================

        public override IEnumerator Execute(float duration)
        {
            yield return FlickerRoutine(duration);
        }

        public override void Cancel()
        {
            base.Cancel();
            RestoreDefaults();
        }

        // =====================================================================
        // PUBLIC API — manual usage
        // =====================================================================

        /// <summary>Play a flicker for the given duration (standalone use).</summary>
        public void PlayFlicker(float duration)
        {
            if (_activeFlicker != null) StopCoroutine(_activeFlicker);
            _activeFlicker = StartCoroutine(FlickerRoutine(duration));
        }

        /// <summary>Immediately restore original intensity and colour.</summary>
        public void RestoreDefaults()
        {
            if (_light != null)
            {
                _light.intensity = _originalIntensity;
                _light.color = _originalColour;
            }
        }

        // =====================================================================
        // COROUTINE
        // =====================================================================

        private IEnumerator FlickerRoutine(float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float multiplier = Random.Range(_minIntensity, _maxIntensity);
                _light.intensity = _originalIntensity * multiplier;

                if (_enableColourShift)
                {
                    // Blend between warm and cool based on intensity
                    float colourT = Mathf.InverseLerp(_minIntensity, _maxIntensity, multiplier);
                    _light.color = Color.Lerp(_warmColour, _coolColour, colourT);
                }

                float wait = Random.Range(_minInterval, _maxInterval);
                yield return new WaitForSeconds(wait);
                elapsed += wait;
            }

            // Restore
            _light.intensity = _originalIntensity;
            _light.color = _originalColour;
            _activeFlicker = null;
        }
    }
}
