// ============================================================================
// InventoryManager.cs — Inventory system with item management
// Handles adding, removing, combining items. Supports item inspection mode.
// Implements ISaveable for save/load. Uses event-driven notifications.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.Core.Events;
using FracturedEchoes.ScriptableObjects;

namespace FracturedEchoes.InventorySystem
{
    /// <summary>
    /// Central inventory manager. Stores items, supports combination,
    /// and provides item inspection capabilities.
    /// </summary>
    public class InventoryManager : MonoBehaviour, ISaveable
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Configuration")]
        [Tooltip("Maximum number of items the player can carry.")]
        [SerializeField] private int _maxSlots = 12;

        [Header("Item Database")]
        [Tooltip("All item definitions in the game (for save/load lookup).")]
        [SerializeField] private ItemData[] _itemDatabase;

        [Header("Inspection")]
        [Tooltip("Reference to the dedicated inspection controller.")]
        [SerializeField] private InspectItemController _inspectController;

        [Header("Events")]
        [SerializeField] private GameEvent _onInventoryChanged;
        [SerializeField] private GameEventString _onItemAdded;
        [SerializeField] private GameEventString _onItemRemoved;
        [SerializeField] private GameEvent _onInventoryFull;

        [Header("Save")]
        [SerializeField] private string _saveID = "inventory";

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private List<ItemData> _items = new List<ItemData>();
        private Camera _mainCamera;

        // =====================================================================
        // PROPERTIES
        // =====================================================================

        public string SaveID => _saveID;
        public IReadOnlyList<ItemData> Items => _items.AsReadOnly();
        public int SlotCount => _maxSlots;
        public int ItemCount => _items.Count;
        public bool IsFull => _items.Count >= _maxSlots;
        public bool IsInspecting => _inspectController != null && _inspectController.IsInspecting;

        // =====================================================================
        // C# EVENTS (for direct subscribers)
        // =====================================================================

        public event Action<ItemData> ItemAdded;
        public event Action<ItemData> ItemRemoved;
        public event Action InventoryChanged;

        // =====================================================================
        // ITEM MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Adds an item to the inventory. Returns false if inventory is full.
        /// </summary>
        public bool AddItem(ItemData item)
        {
            if (item == null) return false;

            if (IsFull)
            {
                _onInventoryFull?.Raise();
                return false;
            }

            _items.Add(item);

            // Notify listeners
            ItemAdded?.Invoke(item);
            InventoryChanged?.Invoke();
            _onItemAdded?.Raise(item.itemID);
            _onInventoryChanged?.Raise();

            Debug.Log($"[Inventory] Added: {item.displayName}");
            return true;
        }

        /// <summary>
        /// Removes an item from the inventory. Returns false if not found.
        /// </summary>
        public bool RemoveItem(ItemData item)
        {
            if (item == null) return false;

            bool removed = _items.Remove(item);
            if (removed)
            {
                ItemRemoved?.Invoke(item);
                InventoryChanged?.Invoke();
                _onItemRemoved?.Raise(item.itemID);
                _onInventoryChanged?.Raise();

                Debug.Log($"[Inventory] Removed: {item.displayName}");
            }

            return removed;
        }

        /// <summary>
        /// Removes an item by its ID.
        /// </summary>
        public bool RemoveItemByID(string itemID)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].itemID == itemID)
                    return RemoveItem(_items[i]);
            }
            return false;
        }

        /// <summary>
        /// Checks if the inventory contains an item with the given ID.
        /// </summary>
        public bool HasItem(string itemID)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].itemID == itemID) return true;
            }
            return false;
        }

        /// <summary>
        /// Gets an item by its ID from the inventory.
        /// </summary>
        public ItemData GetItem(string itemID)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].itemID == itemID) return _items[i];
            }
            return null;
        }

        /// <summary>
        /// Attempts to combine two items. Returns true if successful.
        /// </summary>
        public bool TryCombineItems(ItemData itemA, ItemData itemB)
        {
            if (itemA == null || itemB == null) return false;
            if (!itemA.canCombine || !itemB.canCombine) return false;

            // Check if itemA can be combined with itemB
            if (itemA.combinableWith != null && itemA.combinableWith.Contains(itemB))
            {
                if (itemA.combinationResult != null)
                {
                    RemoveItem(itemA);
                    RemoveItem(itemB);
                    AddItem(itemA.combinationResult);

                    Debug.Log($"[Inventory] Combined {itemA.displayName} + {itemB.displayName} = {itemA.combinationResult.displayName}");
                    return true;
                }
            }

            // Check reverse combination
            if (itemB.combinableWith != null && itemB.combinableWith.Contains(itemA))
            {
                if (itemB.combinationResult != null)
                {
                    RemoveItem(itemA);
                    RemoveItem(itemB);
                    AddItem(itemB.combinationResult);

                    Debug.Log($"[Inventory] Combined {itemB.displayName} + {itemA.displayName} = {itemB.combinationResult.displayName}");
                    return true;
                }
            }

            return false;
        }

        // =====================================================================
        // ITEM INSPECTION (delegated to InspectItemController)
        // =====================================================================

        /// <summary>
        /// Opens the inspection view for the given item.
        /// Delegates to InspectItemController.
        /// </summary>
        public void StartInspection(ItemData item)
        {
            if (_inspectController == null)
            {
                Debug.LogWarning("[Inventory] No InspectItemController assigned.");
                return;
            }

            _inspectController.StartInspection(item);
        }

        /// <summary>
        /// Closes the inspection view.
        /// </summary>
        public void EndInspection()
        {
            _inspectController?.EndInspection();
        }

        // =====================================================================
        // ITEM USAGE ON WORLD OBJECTS
        // =====================================================================

        /// <summary>
        /// Attempts to use an inventory item on a target that implements IItemReceiver.
        /// Returns true if the item was accepted and consumed.
        /// </summary>
        public bool UseItemOn(ItemData item, IItemReceiver receiver)
        {
            if (item == null || receiver == null) return false;
            if (!receiver.CanReceiveItem(item)) return false;

            bool consumed = receiver.ReceiveItem(item);
            if (consumed)
            {
                RemoveItem(item);

                if (item.useSound != null)
                {
                    if (_mainCamera == null) _mainCamera = Camera.main;
                    if (_mainCamera != null)
                        AudioSource.PlayClipAtPoint(item.useSound, _mainCamera.transform.position);
                }

                Debug.Log($"[Inventory] Used {item.displayName} on target.");
            }

            return consumed;
        }

        /// <summary>
        /// Finds the first item matching a required ID from the inventory.
        /// Useful for auto-detecting if the player has the right item for a receiver.
        /// </summary>
        public ItemData FindItemForReceiver(IItemReceiver receiver)
        {
            if (receiver == null) return null;
            string requiredID = receiver.RequiredItemID;
            if (string.IsNullOrEmpty(requiredID))
            {
                // Receiver accepts anything — return first usable item
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i].canUseOnEnvironment) return _items[i];
                }
                return null;
            }
            return GetItem(requiredID);
        }

        // =====================================================================
        // ISaveable IMPLEMENTATION
        // =====================================================================

        public object CaptureState()
        {
            // Save item IDs for serialization
            var ids = new List<string>(_items.Count);
            for (int i = 0; i < _items.Count; i++)
                ids.Add(_items[i].itemID);
            return ids;
        }

        public void RestoreState(object state)
        {
            if (state is List<string> itemIDs && _itemDatabase != null)
            {
                _items.Clear();
                foreach (string id in itemIDs)
                {
                    ItemData item = null;
                    for (int i = 0; i < _itemDatabase.Length; i++)
                    {
                        if (_itemDatabase[i].itemID == id) { item = _itemDatabase[i]; break; }
                    }

                    if (item != null)
                        _items.Add(item);
                    else
                        Debug.LogWarning($"[Inventory] Could not find item with ID: {id} in database.");
                }

                InventoryChanged?.Invoke();
                _onInventoryChanged?.Raise();
            }
        }
    }
}
