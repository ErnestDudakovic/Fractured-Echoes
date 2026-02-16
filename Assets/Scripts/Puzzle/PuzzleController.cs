// ============================================================================
// PuzzleController.cs — Core puzzle state machine
// Implements IPuzzle and ISaveable. Drives puzzle logic through a state
// machine pattern, validates inputs against PuzzleData solution sequences,
// and emits completion/failure events.
// ============================================================================

using System;
using UnityEngine;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.Core.Events;
using FracturedEchoes.ScriptableObjects;

namespace FracturedEchoes.Puzzle
{
    /// <summary>
    /// Core puzzle controller using a state machine pattern.
    /// Attach to puzzle GameObjects. Configure via PuzzleData ScriptableObject.
    /// Supports multi-step solutions, prerequisites, and inventory requirements.
    /// </summary>
    public class PuzzleController : MonoBehaviour, IPuzzle, ISaveable
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Configuration")]
        [Tooltip("The puzzle data asset defining this puzzle's rules.")]
        [SerializeField] private PuzzleData _puzzleData;

        [Header("Events")]
        [Tooltip("Raised when this puzzle is completed (local event).")]
        [SerializeField] private GameEvent _onCompleted;

        [Tooltip("Raised when the player makes a correct step.")]
        [SerializeField] private GameEvent _onCorrectStep;

        [Tooltip("Raised when the player makes an incorrect input.")]
        [SerializeField] private GameEvent _onIncorrectInput;

        [Tooltip("Raised when the puzzle resets after failure.")]
        [SerializeField] private GameEvent _onReset;

        [Header("Save")]
        [SerializeField] private string _saveID;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private PuzzleState _currentState = PuzzleState.Locked;
        private int _currentStepIndex;
        private int _attemptCount;

        // =====================================================================
        // C# EVENTS
        // =====================================================================

        /// <summary>Fired when the puzzle state changes.</summary>
        public event Action<PuzzleState> OnStateChanged;

        /// <summary>Fired when a correct step is made (with step index).</summary>
        public event Action<int> OnStepCompleted;

        /// <summary>Fired on puzzle completion.</summary>
        public event Action OnPuzzleCompleted;

        // =====================================================================
        // IPuzzle IMPLEMENTATION
        // =====================================================================

        public string PuzzleID => _puzzleData != null ? _puzzleData.puzzleID : _saveID;

        public PuzzleState CurrentState => _currentState;

        /// <summary>
        /// Attempts to advance the puzzle with the given input.
        /// Validates against the solution sequence defined in PuzzleData.
        /// </summary>
        public bool TryAdvance(string input)
        {
            if (_currentState != PuzzleState.Available && _currentState != PuzzleState.InProgress)
            {
                Debug.Log($"[Puzzle] {PuzzleID}: Cannot advance — state is {_currentState}");
                return false;
            }

            if (_puzzleData == null || _puzzleData.solutionSequence == null) return false;

            // Check prerequisites
            if (!CheckPrerequisites()) return false;

            // Check inventory requirements
            if (!CheckInventoryRequirements()) return false;

            // Transition to InProgress if not already
            if (_currentState == PuzzleState.Available)
            {
                SetState(PuzzleState.InProgress);
            }

            // Validate input against current step
            string expectedInput = _puzzleData.solutionSequence[_currentStepIndex];

            if (string.Equals(input, expectedInput, StringComparison.OrdinalIgnoreCase))
            {
                // Correct input
                OnCorrectInput();
                return true;
            }
            else
            {
                // Incorrect input
                OnIncorrectInput();
                return false;
            }
        }

        public void ResetPuzzle()
        {
            _currentStepIndex = 0;
            _attemptCount = 0;
            SetState(PuzzleState.Available);
            _onReset?.Raise();

            Debug.Log($"[Puzzle] {PuzzleID}: Reset");
        }

        public void ForceComplete()
        {
            _currentStepIndex = _puzzleData.solutionSequence.Length;
            SetState(PuzzleState.Completed);
        }

        // =====================================================================
        // ISaveable IMPLEMENTATION
        // =====================================================================

        public string SaveID => string.IsNullOrEmpty(_saveID) ? PuzzleID : _saveID;

        public object CaptureState()
        {
            return new PuzzleSaveData
            {
                state = _currentState,
                stepIndex = _currentStepIndex,
                attempts = _attemptCount
            };
        }

        public void RestoreState(object state)
        {
            if (state is PuzzleSaveData saveData)
            {
                _currentState = saveData.state;
                _currentStepIndex = saveData.stepIndex;
                _attemptCount = saveData.attempts;
                OnStateChanged?.Invoke(_currentState);
            }
        }

        // =====================================================================
        // PRIVATE LOGIC
        // =====================================================================

        private void OnCorrectInput()
        {
            _currentStepIndex++;

            // Play correct step sound
            PlaySound(_puzzleData.correctStepSound);

            _onCorrectStep?.Raise();
            OnStepCompleted?.Invoke(_currentStepIndex);

            Debug.Log($"[Puzzle] {PuzzleID}: Correct step {_currentStepIndex}/{_puzzleData.solutionSequence.Length}");

            // Check if puzzle is complete
            if (_currentStepIndex >= _puzzleData.solutionSequence.Length)
            {
                CompletePuzzle();
            }
        }

        private void OnIncorrectInput()
        {
            _attemptCount++;

            // Play incorrect sound
            PlaySound(_puzzleData.incorrectSound);

            _onIncorrectInput?.Raise();

            Debug.Log($"[Puzzle] {PuzzleID}: Incorrect input (attempt {_attemptCount})");

            // Check max attempts
            if (_puzzleData.maxAttempts > 0 && _attemptCount >= _puzzleData.maxAttempts)
            {
                SetState(PuzzleState.Failed);
                _puzzleData.onFailedEvent?.Raise();
                return;
            }

            // Reset progress if configured
            if (_puzzleData.resetOnFailure)
            {
                _currentStepIndex = 0;
                _onReset?.Raise();
            }
        }

        private void CompletePuzzle()
        {
            SetState(PuzzleState.Completed);

            // Play completion sound
            PlaySound(_puzzleData.completionSound);

            // Raise events
            _onCompleted?.Raise();
            _puzzleData.onCompletedEvent?.Raise();
            OnPuzzleCompleted?.Invoke();

            Debug.Log($"[Puzzle] {PuzzleID}: COMPLETED!");
        }

        private void SetState(PuzzleState newState)
        {
            if (_currentState == newState) return;

            PuzzleState oldState = _currentState;
            _currentState = newState;
            OnStateChanged?.Invoke(newState);

            Debug.Log($"[Puzzle] {PuzzleID}: {oldState} → {newState}");
        }

        private bool CheckPrerequisites()
        {
            if (_puzzleData.prerequisites == null) return true;

            // This would need a reference to a puzzle manager to check other puzzle states
            // For now, prerequisites are checked via the PuzzleManager
            return true;
        }

        private bool CheckInventoryRequirements()
        {
            if (_puzzleData.requiredItems == null || _puzzleData.requiredItems.Length == 0) return true;

            InventorySystem.InventoryManager inventory = FindObjectOfType<InventorySystem.InventoryManager>();
            if (inventory == null) return true;

            foreach (ItemData item in _puzzleData.requiredItems)
            {
                if (!inventory.HasItem(item.itemID))
                {
                    Debug.Log($"[Puzzle] {PuzzleID}: Missing required item: {item.displayName}");
                    return false;
                }
            }

            return true;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null || _audioSource == null) return;
            _audioSource.PlayOneShot(clip);
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Unlocks this puzzle, making it available for interaction.
        /// </summary>
        public void Unlock()
        {
            if (_currentState == PuzzleState.Locked)
            {
                SetState(PuzzleState.Available);
            }
        }

        /// <summary>
        /// Locks this puzzle, preventing interaction.
        /// </summary>
        public void Lock()
        {
            if (_currentState != PuzzleState.Completed)
            {
                SetState(PuzzleState.Locked);
            }
        }

        /// <summary>
        /// Gets the current progress as a normalized value (0–1).
        /// </summary>
        public float GetProgress()
        {
            if (_puzzleData == null || _puzzleData.solutionSequence == null || _puzzleData.solutionSequence.Length == 0)
                return 0f;

            return (float)_currentStepIndex / _puzzleData.solutionSequence.Length;
        }

        /// <summary>
        /// Gets the hint text from the puzzle data.
        /// </summary>
        public string GetHint()
        {
            return _puzzleData?.hintText ?? string.Empty;
        }

        // =====================================================================
        // AWAKE
        // =====================================================================

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            // Auto-generate save ID if not set
            if (string.IsNullOrEmpty(_saveID) && _puzzleData != null)
            {
                _saveID = _puzzleData.puzzleID;
            }
        }
    }

    // =========================================================================
    // Save data structure
    // =========================================================================

    /// <summary>
    /// Serializable data structure for puzzle save state.
    /// </summary>
    [Serializable]
    public class PuzzleSaveData
    {
        public PuzzleState state;
        public int stepIndex;
        public int attempts;
    }
}
