// ============================================================================
// IInteractable.cs — Core interaction interface
// All interactive objects in the game world implement this interface.
// Supports context-sensitive prompts and interaction locking.
// ============================================================================

using UnityEngine;

namespace FracturedEchoes.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for any object that can be interacted with
    /// via the raycast-based interaction system.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// The UI prompt shown when the player looks at this object.
        /// Example: "Pick up Key", "Inspect Photo", "Open Door"
        /// </summary>
        string InteractionPrompt { get; }

        /// <summary>
        /// Whether this object can currently be interacted with.
        /// Used to lock interactions during events or when conditions aren't met.
        /// </summary>
        bool CanInteract { get; }

        /// <summary>
        /// Called when the player activates the interaction.
        /// </summary>
        void OnInteract();

        /// <summary>
        /// Called every frame while the player is looking at this object.
        /// Useful for highlight effects, UI updates, etc.
        /// </summary>
        void OnFocus();

        /// <summary>
        /// Called when the player looks away from this object.
        /// Used to remove highlight effects, hide prompts, etc.
        /// </summary>
        void OnLoseFocus();
    }
}
