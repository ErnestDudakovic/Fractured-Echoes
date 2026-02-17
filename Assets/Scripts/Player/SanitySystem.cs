// ============================================================================
// SanitySystem.cs — Player sanity tracker
// Tracks a 0–100 sanity value. Scripted events (jumpscares, horror triggers)
// drain sanity. Over time, sanity slowly recovers. Low sanity feeds into
// camera stress, audio distortion, and visual effects.
// Implements ISaveable for persistence.
// ============================================================================

using System;
using UnityEngine;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.Core.Events;
using FracturedEchoes.UI;

namespace FracturedEchoes.Player
{
    /// <summary>
    /// Central sanity system. Attach to the Player GameObject.
    /// Other systems call <see cref="DrainSanity"/> to reduce sanity
    /// (jumpscares, horror events, darkness exposure, etc.).
    /// </summary>
    public class SanitySystem : MonoBehaviour, ISaveable
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Sanity Settings")]
        [Tooltip("Maximum sanity value.")]
        [SerializeField] private float _maxSanity = 100f;

        [Tooltip("Starting sanity value.")]
        [SerializeField] private float _startingSanity = 100f;

        [Tooltip("Passive sanity recovery per second (only when above threshold).")]
        [SerializeField] private float _regenRate = 0.5f;

        [Tooltip("Sanity below this value disables passive regen.")]
        [SerializeField] private float _regenThreshold = 20f;

        [Tooltip("Delay (seconds) after a drain before regen resumes.")]
        [SerializeField] private float _regenCooldown = 5f;

        [Header("Stress Integration")]
        [Tooltip("Sanity % below which camera stress starts ramping (0–1 of max).")]
        [SerializeField, Range(0f, 1f)] private float _stressOnsetPercent = 0.5f;

        [Header("Events")]
        [SerializeField] private GameEvent _onSanityLow;
        [SerializeField] private GameEvent _onSanityCritical;
        [SerializeField] private GameEvent _onSanityDepleted;

        [Header("Save")]
        [SerializeField] private string _saveID = "sanity";

        // =====================================================================
        // RUNTIME STATE
        // =====================================================================

        private float _currentSanity;
        private float _regenCooldownTimer;
        private bool _firedLow;
        private bool _firedCritical;
        private FirstPersonController _player;

        // =====================================================================
        // PROPERTIES
        // =====================================================================

        /// <summary>Current sanity value (0 – MaxSanity).</summary>
        public float CurrentSanity => _currentSanity;

        /// <summary>Maximum sanity.</summary>
        public float MaxSanity => _maxSanity;

        /// <summary>Normalized sanity (0–1).</summary>
        public float SanityNormalized => _currentSanity / _maxSanity;

        /// <summary>True when sanity is below 30 %.</summary>
        public bool IsLow => _currentSanity < _maxSanity * 0.3f;

        /// <summary>True when sanity is below 10 %.</summary>
        public bool IsCritical => _currentSanity < _maxSanity * 0.1f;

        // =====================================================================
        // C# EVENTS
        // =====================================================================

        /// <summary>Fired whenever sanity changes (newValue, maxValue).</summary>
        public event Action<float, float> SanityChanged;

        // =====================================================================
        // ISaveable
        // =====================================================================

        public string SaveID => _saveID;

        public object CaptureState() => _currentSanity;

        public void RestoreState(object state)
        {
            if (state is float saved)
            {
                _currentSanity = Mathf.Clamp(saved, 0f, _maxSanity);
                SanityChanged?.Invoke(_currentSanity, _maxSanity);
            }
        }

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            _currentSanity = _startingSanity;
            _player = GetComponent<FirstPersonController>();

            // Auto-add GameOverUI if not already present anywhere in the scene
            if (FindFirstObjectByType<GameOverUI>() == null)
            {
                gameObject.AddComponent<GameOverUI>();
            }
        }

        private void Update()
        {
            // Regen cooldown
            if (_regenCooldownTimer > 0f)
            {
                _regenCooldownTimer -= Time.deltaTime;
                return;
            }

            // Passive regen
            if (_currentSanity < _maxSanity && _currentSanity > _regenThreshold)
            {
                _currentSanity = Mathf.Min(_currentSanity + _regenRate * Time.deltaTime, _maxSanity);
                SanityChanged?.Invoke(_currentSanity, _maxSanity);

                // Reset event flags when recovering
                if (_currentSanity > _maxSanity * 0.3f) _firedLow = false;
                if (_currentSanity > _maxSanity * 0.1f) _firedCritical = false;
            }

            // Feed stress level into camera system
            ApplyStressToCamera();
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Reduces sanity by the given amount. Use for jumpscares, horror events, etc.
        /// </summary>
        public void DrainSanity(float amount)
        {
            if (amount <= 0f) return;

            _currentSanity = Mathf.Max(0f, _currentSanity - amount);
            _regenCooldownTimer = _regenCooldown;
            SanityChanged?.Invoke(_currentSanity, _maxSanity);

            // Threshold events
            if (IsLow && !_firedLow)
            {
                _firedLow = true;
                _onSanityLow?.Raise();
            }

            if (IsCritical && !_firedCritical)
            {
                _firedCritical = true;
                _onSanityCritical?.Raise();
            }

            if (_currentSanity <= 0f)
            {
                _onSanityDepleted?.Raise();
            }

            Debug.Log($"[Sanity] Drained {amount:F0} → {_currentSanity:F0}/{_maxSanity:F0}");
        }

        /// <summary>
        /// Restores sanity by the given amount (safe zones, items, etc.).
        /// </summary>
        public void RestoreSanity(float amount)
        {
            if (amount <= 0f) return;

            _currentSanity = Mathf.Min(_maxSanity, _currentSanity + amount);
            SanityChanged?.Invoke(_currentSanity, _maxSanity);

            if (_currentSanity > _maxSanity * 0.3f) _firedLow = false;
            if (_currentSanity > _maxSanity * 0.1f) _firedCritical = false;
        }

        /// <summary>
        /// Sets sanity to a specific value (for debugging / scripted moments).
        /// </summary>
        public void SetSanity(float value)
        {
            _currentSanity = Mathf.Clamp(value, 0f, _maxSanity);
            SanityChanged?.Invoke(_currentSanity, _maxSanity);
        }

        // =====================================================================
        // INTERNAL
        // =====================================================================

        private void ApplyStressToCamera()
        {
            if (_player == null) return;

            float stressOnset = _maxSanity * _stressOnsetPercent;
            if (_currentSanity < stressOnset)
            {
                // Ramp stress from 0 → 1 as sanity drops from onset → 0
                float stress = 1f - (_currentSanity / stressOnset);
                _player.SetStressLevel(stress);

                // Enable drift and instability at very low sanity
                bool severe = _currentSanity < _maxSanity * 0.2f;
                _player.SetPsychologicalEffects(severe, severe, severe);
            }
            else
            {
                _player.SetStressLevel(0f);
                _player.SetPsychologicalEffects(false, false, false);
            }
        }
    }
}
