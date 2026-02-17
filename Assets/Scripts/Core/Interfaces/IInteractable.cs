// ============================================================================
// IInteractable.cs — Core interaction interface
// All interactive objects in the game world implement this interface.
// Supports context-sensitive prompts, interaction locking, cooldowns,
// and categorisation via InteractionType.
// ============================================================================

using UnityEngine;

namespace FracturedEchoes.Core.Interfaces
{
    /// <summary>
    /// Categorises the kind of interaction for UI icon / prompt styling.
    /// </summary>
    public enum InteractionType
    {
        Generic,    // Default — "Interact"
        Pickup,     // Inventory items
        Inspect,    // Examine / read
        Activate,   // Doors, switches, levers
        Puzzle,     // Puzzle mechanisms
        Dialogue    // Notes, recordings, NPCs
    }

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
        /// Category of interaction — used by the UI to pick an icon / colour.
        /// </summary>
        InteractionType Type { get; }

        /// <summary>
        /// Whether this object can currently be interacted with.
        /// Used to lock interactions during events or when conditions aren't met.
        /// </summary>
        bool CanInteract { get; }

        /// <summary>
        /// Minimum seconds between repeated interactions (0 = no cooldown).
        /// Enforced by the InteractionSystem, not the object itself.
        /// </summary>
        float InteractionCooldown { get; }

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
