// ============================================================================
// IItemReceiver.cs — Interface for objects that accept inventory items
// Doors, locks, machines, etc. that need a specific item to activate.
// ============================================================================

using FracturedEchoes.ScriptableObjects;

namespace FracturedEchoes.Core.Interfaces
{
    /// <summary>
    /// Implement on any interactable that can receive an inventory item.
    /// The InteractionSystem checks for this when the player uses an item.
    /// </summary>
    public interface IItemReceiver
    {
        /// <summary>The item ID this receiver requires, or null if it accepts anything.</summary>
        string RequiredItemID { get; }

        /// <summary>Whether this receiver can currently accept an item.</summary>
        bool CanReceiveItem(ItemData item);

        /// <summary>Called when the correct item is given. Returns true if consumed.</summary>
        bool ReceiveItem(ItemData item);
    }
}
