// ============================================================================
// PuzzleData.cs — ScriptableObject definition for puzzle configurations
// Defines puzzle conditions, steps, and completion events.
// Create instances via: Create → Fractured Echoes → Data → Puzzle Data
// ============================================================================

using UnityEngine;

namespace FracturedEchoes.ScriptableObjects
{
    /// <summary>
    /// Data container for puzzle configuration.
    /// Defines the structure, conditions, and rewards for a puzzle.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPuzzle", menuName = "Fractured Echoes/Data/Puzzle Data")]
    public class PuzzleData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this puzzle.")]
        public string puzzleID;

        [Tooltip("Display name for debug/UI purposes.")]
        public string displayName;

        [TextArea(2, 4)]
        [Tooltip("Optional hint text for the player.")]
        public string hintText;

        [Header("Configuration")]
        [Tooltip("The ordered sequence of correct inputs to solve this puzzle.")]
        public string[] solutionSequence;

        [Tooltip("Whether the puzzle resets on incorrect input.")]
        public bool resetOnFailure = true;

        [Tooltip("Maximum number of attempts before lockout (0 = unlimited).")]
        public int maxAttempts = 0;

        [Header("Dependencies")]
        [Tooltip("Puzzles that must be solved before this one becomes available.")]
        public PuzzleData[] prerequisites;

        [Tooltip("Items required in inventory to attempt this puzzle.")]
        public ItemData[] requiredItems;

        [Header("Events")]
        [Tooltip("Event raised when this puzzle is completed.")]
        public Core.Events.GameEvent onCompletedEvent;

        [Tooltip("Event raised when the player fails this puzzle.")]
        public Core.Events.GameEvent onFailedEvent;

        [Header("Rewards")]
        [Tooltip("Item granted upon puzzle completion (if any).")]
        public ItemData rewardItem;

        [Tooltip("Environment phase to trigger on completion (−1 = none).")]
        public int triggerPhaseIndex = -1;

        [Header("Audio")]
        [Tooltip("Sound played on correct step.")]
        public AudioClip correctStepSound;

        [Tooltip("Sound played on incorrect input.")]
        public AudioClip incorrectSound;

        [Tooltip("Sound played on puzzle completion.")]
        public AudioClip completionSound;
    }
}
