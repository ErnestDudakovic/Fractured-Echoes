// ============================================================================
// PickupInteractable.cs — Pickup interaction specialization
// Handles picking up items and adding them to the inventory.
// Uses a cached InventoryManager reference instead of FindObjectOfType.
// ============================================================================

using UnityEngine;
using FracturedEchoes.Core.Interfaces;
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

        // Cached reference — resolved once in Awake
        private InventorySystem.InventoryManager _inventory;

        /// <summary>
        /// The item data associated with this pickup.
        /// </summary>
        public ItemData ItemData => _itemData;

        protected override void Awake()
        {
            base.Awake();
            _inventory = FindFirstObjectByType<InventorySystem.InventoryManager>();
        }

        public override void OnInteract()
        {
            if (!CanInteract || _itemData == null) return;

            if (_inventory == null)
            {
                Debug.LogWarning("[PickupInteractable] No InventoryManager found in scene!", this);
                return;
            }

            bool added = _inventory.AddItem(_itemData);
            if (!added) return;

            // Play pickup sound from item data
            if (_itemData.pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(_itemData.pickupSound, transform.position);
            }

            // Trigger base class logic (event, single-use, sound)
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
