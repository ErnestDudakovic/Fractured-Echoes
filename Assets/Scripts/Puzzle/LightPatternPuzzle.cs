// ============================================================================
// LightPatternPuzzle.cs — Light-based puzzle (Documentation §Stage 4)
// The player toggles a set of light switches; the puzzle is solved when the
// on/off pattern of the lights matches the configured target pattern.
// Example: "Only the candles that appear in the painting may burn."
//
// Setup:
//   1. Add this component to an empty "LightPuzzle" root object.
//   2. Add LightSwitchInteractable components to the switch objects
//      and register them in the _switches array (order matters).
//   3. Set _targetPattern to the correct on/off combination.
//   4. Wire _onSolved and/or _doorToUnlock for the reward.
// ============================================================================

using System;
using UnityEngine;
using FracturedEchoes.Core;
using FracturedEchoes.Core.Events;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.Core.SaveLoad;

namespace FracturedEchoes.Puzzle
{
    /// <summary>
    /// A puzzle solved by matching a target on/off pattern across a group of
    /// light switches. Checks the pattern every time a switch is toggled.
    /// </summary>
    public class LightPatternPuzzle : MonoBehaviour, ISaveable
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Identity")]
        [Tooltip("Unique puzzle ID (registered with GameStateManager on solve).")]
        [SerializeField] private string _puzzleID = "light_pattern_puzzle";

        [Header("Switches")]
        [Tooltip("The light switches that make up this puzzle (order matters).")]
        [SerializeField] private LightSwitchInteractable[] _switches;

        [Tooltip("The correct on/off state for each switch (same order as above).")]
        [SerializeField] private bool[] _targetPattern;

        [Header("Rewards")]
        [Tooltip("Raised when the pattern is matched.")]
        [SerializeField] private GameEvent _onSolved;

        [Tooltip("Optional door to force-unlock on solve.")]
        [SerializeField] private Interaction.LockedDoor _doorToUnlock;

        [Tooltip("Environment phase to transition to on solve (-1 = none).")]
        [SerializeField] private int _triggerPhaseIndex = -1;

        [Header("Audio")]
        [Tooltip("Sound played when the puzzle is solved.")]
        [SerializeField] private AudioClip _solvedSound;

        [Header("Behaviour")]
        [Tooltip("Lock all switches once the puzzle is solved.")]
        [SerializeField] private bool _lockSwitchesOnSolve = true;

        // =====================================================================
        // RUNTIME STATE
        // =====================================================================

        private bool _isSolved;
        private AudioSource _audioSource;
        private GameStateManager _cachedGameState;
        private Environment.EnvironmentStateManager _cachedEnvManager;
        private Environment.ScriptedEventController _cachedEventController;

        // =====================================================================
        // PROPERTIES / EVENTS
        // =====================================================================

        /// <summary>True once the pattern has been matched.</summary>
        public bool IsSolved => _isSolved;

        /// <summary>Fired when the puzzle is solved.</summary>
        public event Action Solved;

        // =====================================================================
        // ISaveable
        // =====================================================================

        public string SaveID => _puzzleID;

        public object CaptureState() => new SaveInt { value = _isSolved ? 1 : 0 };

        public void RestoreState(object state)
        {
            bool solved = state switch
            {
                SaveInt wrapper => wrapper.value == 1,
                int legacy => legacy == 1,
                _ => _isSolved
            };

            if (solved && !_isSolved)
            {
                // Restore solved state silently (no sounds / phase changes)
                _isSolved = true;
                if (_lockSwitchesOnSolve) LockAllSwitches();
            }
        }

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f;
            }

            _cachedGameState = FindFirstObjectByType<GameStateManager>();
            _cachedEnvManager = FindFirstObjectByType<Environment.EnvironmentStateManager>();
            _cachedEventController = FindFirstObjectByType<Environment.ScriptedEventController>();

            if (_switches != null && _targetPattern != null &&
                _switches.Length != _targetPattern.Length)
            {
                Debug.LogWarning($"[LightPuzzle] {_puzzleID}: switch count ({_switches.Length}) " +
                                 $"does not match target pattern length ({_targetPattern.Length}).", this);
            }
        }

        private void OnEnable()
        {
            if (_switches == null) return;

            foreach (var sw in _switches)
            {
                if (sw != null) sw.Toggled += HandleSwitchToggled;
            }
        }

        private void OnDisable()
        {
            if (_switches == null) return;

            foreach (var sw in _switches)
            {
                if (sw != null) sw.Toggled -= HandleSwitchToggled;
            }
        }

        // =====================================================================
        // PUZZLE LOGIC
        // =====================================================================

        private void HandleSwitchToggled(LightSwitchInteractable sw, bool isOn)
        {
            if (_isSolved) return;
            CheckPattern();
        }

        /// <summary>
        /// Compares the current switch states against the target pattern.
        /// </summary>
        public void CheckPattern()
        {
            if (_switches == null || _targetPattern == null) return;
            int count = Mathf.Min(_switches.Length, _targetPattern.Length);

            for (int i = 0; i < count; i++)
            {
                if (_switches[i] == null) return;
                if (_switches[i].IsOn != _targetPattern[i]) return;
            }

            Solve();
        }

        private void Solve()
        {
            if (_isSolved) return;
            _isSolved = true;

            if (_solvedSound != null)
                _audioSource.PlayOneShot(_solvedSound);

            if (_lockSwitchesOnSolve)
                LockAllSwitches();

            // Rewards / progression
            _onSolved?.Raise();
            Solved?.Invoke();
            _doorToUnlock?.ForceUnlock();
            _cachedGameState?.MarkPuzzleCompleted(_puzzleID);

            if (_triggerPhaseIndex >= 0)
                _cachedEnvManager?.TransitionToPhase(_triggerPhaseIndex);

            _cachedEventController?.TriggerByType(ScriptableObjects.TriggerType.SolvePuzzle);

            Debug.Log($"[LightPuzzle] {_puzzleID}: SOLVED!");
        }

        private void LockAllSwitches()
        {
            foreach (var sw in _switches)
            {
                if (sw != null) sw.SetInteractable(false);
            }
        }

        /// <summary>Resets the puzzle and unlocks the switches (debug / retry).</summary>
        public void ResetPuzzle()
        {
            _isSolved = false;
            foreach (var sw in _switches)
            {
                if (sw != null) sw.SetInteractable(true);
            }
        }
    }
}
