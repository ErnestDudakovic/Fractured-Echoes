// ============================================================================
// PuzzleInteractable.cs — Bridge between interaction and puzzle systems
// An interactable object that sends input to a PuzzleController.
// ============================================================================

using UnityEngine;
using FracturedEchoes.Interaction;
using FracturedEchoes.Core.Interfaces;

namespace FracturedEchoes.Puzzle
{
    /// <summary>
    /// An interactable object that provides input to a puzzle.
    /// Attach to puzzle elements (buttons, levers, dials, etc.).
    /// </summary>
    public class PuzzleInteractable : InteractableObject
    {
        [Header("Puzzle Link")]
        [Tooltip("The puzzle controller this interactable feeds into.")]
        [SerializeField] private PuzzleController _puzzleController;

        [Tooltip("The input string this interaction sends to the puzzle.")]
        [SerializeField] private string _puzzleInput;

        [Tooltip("Whether to unlock the puzzle on first interaction.")]
        [SerializeField] private bool _autoUnlock = false;

        public override void OnInteract()
        {
            if (!CanInteract || _puzzleController == null) return;

            // Auto-unlock if configured
            if (_autoUnlock && _puzzleController.CurrentState == PuzzleState.Locked)
            {
                _puzzleController.Unlock();
            }

            // Send input to puzzle
            if (_puzzleController.CurrentState == PuzzleState.Available ||
                _puzzleController.CurrentState == PuzzleState.InProgress)
            {
                _puzzleController.TryAdvance(_puzzleInput);
            }

            base.OnInteract();
        }
    }
}
