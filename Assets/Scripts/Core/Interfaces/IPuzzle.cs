// ============================================================================
// IPuzzle.cs — Puzzle system interface
// All puzzles implement this interface for consistent state management.
// ============================================================================

namespace FracturedEchoes.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for puzzle components.
    /// Puzzles use state machines and emit events on completion.
    /// </summary>
    public interface IPuzzle
    {
        /// <summary>
        /// Unique identifier for this puzzle instance.
        /// Used for save system and event references.
        /// </summary>
        string PuzzleID { get; }

        /// <summary>
        /// The current state of this puzzle.
        /// </summary>
        PuzzleState CurrentState { get; }

        /// <summary>
        /// Attempts to advance the puzzle with a given input/action.
        /// Returns true if the input was valid and caused a state change.
        /// </summary>
        bool TryAdvance(string input);

        /// <summary>
        /// Resets the puzzle to its initial state.
        /// </summary>
        void ResetPuzzle();

        /// <summary>
        /// Forces the puzzle to a completed state (for loading saves).
        /// </summary>
        void ForceComplete();
    }

    /// <summary>
    /// Represents the possible states of a puzzle.
    /// </summary>
    public enum PuzzleState
    {
        Locked,       // Not yet accessible
        Available,    // Player can begin attempting
        InProgress,   // Player has started but not completed
        Completed,    // Successfully solved
        Failed        // Failed (optional — some puzzles may not fail)
    }
}
