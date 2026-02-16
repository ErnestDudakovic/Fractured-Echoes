// ============================================================================
// ISaveable.cs — Save system interface
// Any component that needs to persist its state implements this interface.
// Provides unique identification and serialization contract.
// ============================================================================

namespace FracturedEchoes.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for components that can save and load their state.
    /// Each saveable component provides a unique ID and serializable data.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// A unique identifier for this saveable component.
        /// Must be consistent across save/load cycles.
        /// Typically set in the Inspector or generated from a GUID.
        /// </summary>
        string SaveID { get; }

        /// <summary>
        /// Captures the current state as a serializable object.
        /// </summary>
        object CaptureState();

        /// <summary>
        /// Restores state from a previously captured serializable object.
        /// </summary>
        void RestoreState(object state);
    }
}
