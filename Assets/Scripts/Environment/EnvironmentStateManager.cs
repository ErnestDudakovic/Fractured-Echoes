// ============================================================================
// EnvironmentStateManager.cs — Room phase transition controller
// Central controller for environment corruption/transformation phases.
// Tracks progression state, applies environmental variations,
// controls room versions, and handles transformation phases.
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.Core.Events;
using FracturedEchoes.ScriptableObjects;

namespace FracturedEchoes.Environment
{
    /// <summary>
    /// Central controller for environment phase transitions.
    /// Manages the corruption/transformation state of rooms.
    /// State 0 = Clean, higher states = more corrupted/distorted.
    /// </summary>
    public class EnvironmentStateManager : MonoBehaviour, ISaveable
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Phase Configuration")]
        [Tooltip("Phase data assets in order (index 0 = clean, last = fully corrupted).")]
        [SerializeField] private EnvironmentPhaseData[] _phases;

        [Tooltip("Starting phase index.")]
        [SerializeField] private int _startingPhase = 0;

        [Header("Phase Objects")]
        [Tooltip("All objects that implement IEnvironmentPhase in this location.")]
        [SerializeField] private GameObject[] _phaseAwareObjects;

        [Header("Fog")]
        [SerializeField] private bool _controlFog = true;

        [Header("Events")]
        [Tooltip("Raised when the environment phase changes.")]
        [SerializeField] private GameEventInt _onPhaseChanged;

        [Tooltip("Raised when a phase transition begins.")]
        [SerializeField] private GameEvent _onTransitionStarted;

        [Tooltip("Raised when a phase transition completes.")]
        [SerializeField] private GameEvent _onTransitionCompleted;

        [Header("Scripted Events")]
        [Tooltip("Reference to scripted event controller for phase-triggered events.")]
        [SerializeField] private ScriptedEventController _scriptedEventController;

        [Header("Save")]
        [SerializeField] private string _saveID = "environment_state";

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private int _currentPhase;
        private bool _isTransitioning;
        private Coroutine _transitionCoroutine;
        private List<IEnvironmentPhase> _phaseReceivers = new List<IEnvironmentPhase>();

        // =====================================================================
        // PROPERTIES
        // =====================================================================

        public string SaveID => _saveID;
        public int CurrentPhase => _currentPhase;
        public int PhaseCount => _phases != null ? _phases.Length : 0;
        public bool IsTransitioning => _isTransitioning;
        public EnvironmentPhaseData CurrentPhaseData => 
            _phases != null && _currentPhase < _phases.Length ? _phases[_currentPhase] : null;

        // =====================================================================
        // C# EVENTS
        // =====================================================================

        /// <summary>Fired when phase changes (oldPhase, newPhase).</summary>
        public event Action<int, int> PhaseChanged;

        /// <summary>Fired during transition with progress (0–1).</summary>
        public event Action<float> TransitionProgress;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            // Collect all IEnvironmentPhase components from registered objects
            if (_phaseAwareObjects != null)
            {
                foreach (GameObject obj in _phaseAwareObjects)
                {
                    if (obj != null)
                    {
                        // GetComponentsInChildren includes the root object itself
                        IEnvironmentPhase[] phases = obj.GetComponentsInChildren<IEnvironmentPhase>();
                        _phaseReceivers.AddRange(phases);
                    }
                }
            }
        }

        private void Start()
        {
            // Apply starting phase immediately (no transition)
            ApplyPhaseImmediate(_startingPhase);
        }

        // =====================================================================
        // PUBLIC API — PHASE CONTROL
        // =====================================================================

        /// <summary>
        /// Transitions to a specific phase with smooth animation.
        /// </summary>
        public void TransitionToPhase(int targetPhase)
        {
            if (_phases == null || targetPhase < 0 || targetPhase >= _phases.Length)
            {
                Debug.LogWarning($"[Environment] Invalid phase index: {targetPhase}");
                return;
            }

            if (targetPhase == _currentPhase)
            {
                Debug.Log($"[Environment] Already at phase {targetPhase}");
                return;
            }

            if (_isTransitioning)
            {
                Debug.LogWarning("[Environment] Transition already in progress — cancelling current.");
                if (_transitionCoroutine != null)
                {
                    StopCoroutine(_transitionCoroutine);
                }
            }

            _transitionCoroutine = StartCoroutine(PerformTransition(targetPhase));
        }

        /// <summary>
        /// Advances to the next phase. Wraps around if at max.
        /// </summary>
        public void AdvancePhase()
        {
            int nextPhase = Mathf.Min(_currentPhase + 1, _phases.Length - 1);
            TransitionToPhase(nextPhase);
        }

        /// <summary>
        /// Immediately applies a phase without transition animation.
        /// Used for loading saves and initial setup.
        /// </summary>
        public void ApplyPhaseImmediate(int phaseIndex)
        {
            if (_phases == null || phaseIndex < 0 || phaseIndex >= _phases.Length) return;

            int oldPhase = _currentPhase;
            _currentPhase = phaseIndex;

            EnvironmentPhaseData phaseData = _phases[phaseIndex];

            // Apply lighting
            RenderSettings.ambientLight = phaseData.ambientColor;
            RenderSettings.ambientIntensity = phaseData.ambientIntensity;

            // Apply fog
            if (_controlFog)
            {
                RenderSettings.fog = phaseData.fogDensity > 0f;
                RenderSettings.fogColor = phaseData.fogColor;
                RenderSettings.fogDensity = phaseData.fogDensity;
            }

            // Notify all phase-aware objects
            foreach (var receiver in _phaseReceivers)
            {
                if (phaseIndex < receiver.PhaseCount)
                {
                    receiver.ApplyPhase(phaseIndex);
                }
            }

            // Raise events
            PhaseChanged?.Invoke(oldPhase, _currentPhase);
            _onPhaseChanged?.Raise(_currentPhase);

            // Trigger phase-based scripted events
            _scriptedEventController?.TriggerPhaseEvents(_currentPhase);

            Debug.Log($"[Environment] Phase applied immediately: {phaseData.phaseName} (index {phaseIndex})");
        }

        // =====================================================================
        // TRANSITION COROUTINE
        // =====================================================================

        private IEnumerator PerformTransition(int targetPhase)
        {
            _isTransitioning = true;
            _onTransitionStarted?.Raise();

            int oldPhase = _currentPhase;
            EnvironmentPhaseData fromPhase = _phases[_currentPhase];
            EnvironmentPhaseData toPhase = _phases[targetPhase];
            float duration = toPhase.transitionDuration;
            float elapsed = 0f;

            Debug.Log($"[Environment] Transitioning: {fromPhase.phaseName} → {toPhase.phaseName} ({duration}s)");

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth step interpolation
                float smoothT = t * t * (3f - 2f * t);

                // Interpolate lighting
                RenderSettings.ambientLight = Color.Lerp(fromPhase.ambientColor, toPhase.ambientColor, smoothT);
                RenderSettings.ambientIntensity = Mathf.Lerp(fromPhase.ambientIntensity, toPhase.ambientIntensity, smoothT);

                // Interpolate fog
                if (_controlFog)
                {
                    RenderSettings.fogColor = Color.Lerp(fromPhase.fogColor, toPhase.fogColor, smoothT);
                    RenderSettings.fogDensity = Mathf.Lerp(fromPhase.fogDensity, toPhase.fogDensity, smoothT);
                }

                // Report progress
                TransitionProgress?.Invoke(smoothT);

                yield return null;
            }

            // Finalize phase
            _currentPhase = targetPhase;

            // Enable fog if density > 0
            if (_controlFog)
            {
                RenderSettings.fog = toPhase.fogDensity > 0f;
            }

            // Notify phase-aware objects
            foreach (var receiver in _phaseReceivers)
            {
                if (targetPhase < receiver.PhaseCount)
                {
                    receiver.ApplyPhase(targetPhase);
                }
            }

            // Raise events
            _isTransitioning = false;
            PhaseChanged?.Invoke(oldPhase, _currentPhase);
            _onPhaseChanged?.Raise(_currentPhase);
            _onTransitionCompleted?.Raise();

            // Trigger phase-based scripted events
            _scriptedEventController?.TriggerPhaseEvents(_currentPhase);

            Debug.Log($"[Environment] Transition complete: {toPhase.phaseName}");
        }

        // =====================================================================
        // ISaveable IMPLEMENTATION
        // =====================================================================

        public object CaptureState()
        {
            return _currentPhase;
        }

        public void RestoreState(object state)
        {
            if (state is int savedPhase)
            {
                ApplyPhaseImmediate(savedPhase);
            }
        }
    }
}
