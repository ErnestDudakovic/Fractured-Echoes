// ============================================================================
// IEnvironmentPhase.cs — Environment phase transition interface
// Defines phases of room corruption/transformation.
// ============================================================================

namespace FracturedEchoes.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for environment objects that change
    /// based on the current room phase (clean → corrupted).
    /// </summary>
    public interface IEnvironmentPhase
    {
        /// <summary>
        /// Applies the visual/behavioral changes for the given phase index.
        /// Phase 0 = clean, higher phases = more corrupted.
        /// </summary>
        void ApplyPhase(int phaseIndex);

        /// <summary>
        /// The total number of phases this object supports.
        /// </summary>
        int PhaseCount { get; }
    }
}
