// ============================================================================
// PickupInteractable.cs — Pickup interaction specialization
// Handles picking up items and adding them to the inventory.
// ============================================================================

using UnityEngine;
using FracturedEchoes.ScriptableObjects;

namespace FracturedEchoes.Interaction
{
    /// <summary>
    /// An interactable object that can be picked up and added to inventory.
    /// Destroys (or deactivates) the world object after pickup.
    /// </summary>
    public class PickupInteractable : InteractableObject
    {
        [Header("Pickup Settings")]
        [Tooltip("The item data for this pickup.")]
        [SerializeField] private ItemData _itemData;

        [Tooltip("Whether to destroy or deactivate the object after pickup.")]
        [SerializeField] private bool _destroyOnPickup = false;

        /// <summary>
        /// The item data associated with this pickup.
        /// </summary>
        public ItemData ItemData => _itemData;

        public override void OnInteract()
        {
            if (!CanInteract || _itemData == null) return;

            // Find inventory and add item
            InventorySystem.InventoryManager inventory = FindObjectOfType<InventorySystem.InventoryManager>();
            if (inventory != null)
            {
                bool added = inventory.AddItem(_itemData);
                if (added)
                {
                    // Play pickup sound from item data
                    if (_itemData.pickupSound != null)
                    {
                        AudioSource.PlayClipAtPoint(_itemData.pickupSound, transform.position);
                    }

                    base.OnInteract();

                    // Remove from world
                    if (_destroyOnPickup)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
