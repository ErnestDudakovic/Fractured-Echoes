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
        [Tooltip("Transform where inspection objects are spawned.")]
        [SerializeField] private Transform _inspectionPoint;

        [Tooltip("Camera used for item inspection view.")]
        [SerializeField] private Camera _inspectionCamera;

        [Tooltip("Rotation speed when inspecting an item.")]
        [SerializeField] private float _inspectionRotateSpeed = 100f;

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
        private bool _isInspecting;
        private GameObject _inspectionObject;
        private ItemData _inspectedItem;

        // =====================================================================
        // PROPERTIES
        // =====================================================================

        public string SaveID => _saveID;
        public IReadOnlyList<ItemData> Items => _items.AsReadOnly();
        public int SlotCount => _maxSlots;
        public int ItemCount => _items.Count;
        public bool IsFull => _items.Count >= _maxSlots;
        public bool IsInspecting => _isInspecting;

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
            ItemData item = _items.FirstOrDefault(i => i.itemID == itemID);
            return item != null && RemoveItem(item);
        }

        /// <summary>
        /// Checks if the inventory contains an item with the given ID.
        /// </summary>
        public bool HasItem(string itemID)
        {
            return _items.Any(i => i.itemID == itemID);
        }

        /// <summary>
        /// Gets an item by its ID from the inventory.
        /// </summary>
        public ItemData GetItem(string itemID)
        {
            return _items.FirstOrDefault(i => i.itemID == itemID);
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
        // ITEM INSPECTION
        // =====================================================================

        /// <summary>
        /// Enters item inspection mode. Spawns the 3D model for rotation.
        /// </summary>
        public void StartInspection(ItemData item)
        {
            if (item == null || item.inspectionPrefab == null) return;
            if (_inspectionPoint == null) return;

            // End any current inspection
            EndInspection();

            _inspectedItem = item;
            _isInspecting = true;

            // Spawn inspection model
            _inspectionObject = Instantiate(item.inspectionPrefab, _inspectionPoint.position, Quaternion.identity, _inspectionPoint);

            // Enable inspection camera
            if (_inspectionCamera != null)
            {
                _inspectionCamera.gameObject.SetActive(true);
            }

            Debug.Log($"[Inventory] Inspecting: {item.displayName}");
        }

        /// <summary>
        /// Ends item inspection mode and cleans up.
        /// </summary>
        public void EndInspection()
        {
            if (_inspectionObject != null)
            {
                Destroy(_inspectionObject);
                _inspectionObject = null;
            }

            _inspectedItem = null;
            _isInspecting = false;

            // Disable inspection camera
            if (_inspectionCamera != null)
            {
                _inspectionCamera.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // Handle inspection rotation
            if (_isInspecting && _inspectionObject != null)
            {
                HandleInspectionRotation();

                // Exit inspection on right-click or Escape
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                {
                    EndInspection();
                }
            }
        }

        private void HandleInspectionRotation()
        {
            if (Input.GetMouseButton(0))
            {
                float rotX = Input.GetAxis("Mouse X") * _inspectionRotateSpeed * Time.deltaTime;
                float rotY = Input.GetAxis("Mouse Y") * _inspectionRotateSpeed * Time.deltaTime;

                _inspectionObject.transform.Rotate(Vector3.up, -rotX, Space.World);
                _inspectionObject.transform.Rotate(Vector3.right, rotY, Space.World);
            }
        }

        // =====================================================================
        // ISaveable IMPLEMENTATION
        // =====================================================================

        public object CaptureState()
        {
            // Save item IDs for serialization
            return _items.Select(i => i.itemID).ToList();
        }

        public void RestoreState(object state)
        {
            if (state is List<string> itemIDs && _itemDatabase != null)
            {
                _items.Clear();
                foreach (string id in itemIDs)
                {
                    ItemData item = _itemDatabase.FirstOrDefault(i => i.itemID == id);
                    if (item != null)
                    {
                        _items.Add(item);
                    }
                    else
                    {
                        Debug.LogWarning($"[Inventory] Could not find item with ID: {id} in database.");
                    }
                }

                InventoryChanged?.Invoke();
                _onInventoryChanged?.Raise();
            }
        }
    }
}
