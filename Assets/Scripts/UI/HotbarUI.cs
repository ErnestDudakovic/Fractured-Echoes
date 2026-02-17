// ============================================================================
// HotbarUI.cs — On-screen hotbar HUD with assignable quickslots
// Shows N quickslots at the bottom-center of the screen.
// Items are assigned from the inventory (not auto-filled).
// Press number keys 1–5 to select/use a slot.
// Self-builds its own UI elements (no prefab required).
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using FracturedEchoes.InventorySystem;
using FracturedEchoes.ScriptableObjects;
using FracturedEchoes.Player;

namespace FracturedEchoes.UI
{
    /// <summary>
    /// Renders assignable quickslots at the bottom of the screen.
    /// Items are placed into slots via <see cref="AssignItem"/>.
    /// Press 1–5 to select; press the active slot key again to use the item.
    /// </summary>
    public class HotbarUI : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Hotbar Settings")]
        [Tooltip("Number of visible quickslots.")]
        [SerializeField] private int _slotCount = 5;

        [Tooltip("Size of each slot in pixels.")]
        [SerializeField] private float _slotSize = 64f;

        [Tooltip("Gap between slots in pixels.")]
        [SerializeField] private float _slotGap = 6f;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = new Color(0.15f, 0.15f, 0.15f, 0.7f);
        [SerializeField] private Color _selectedColor = new Color(0.9f, 0.6f, 0.1f, 0.85f);
        [SerializeField] private Color _emptyIconColor = new Color(1f, 1f, 1f, 0.15f);

        // =====================================================================
        // RUNTIME STATE
        // =====================================================================

        private InventoryManager _inventory;
        private SanitySystem _sanity;
        private Canvas _canvas;
        private RectTransform _barRoot;
        private readonly List<SlotView> _slots = new List<SlotView>();
        private ItemData[] _assignedItems;
        private int _selectedIndex;

        // =====================================================================
        // SLOT VIEW
        // =====================================================================

        private struct SlotView
        {
            public RectTransform Root;
            public Image Background;
            public Image Icon;
            public TextMeshProUGUI KeyLabel;
        }

        // =====================================================================
        // PROPERTIES
        // =====================================================================

        /// <summary>Currently selected hotbar index (0-based).</summary>
        public int SelectedIndex => _selectedIndex;

        /// <summary>Number of quickslots.</summary>
        public int SlotCount => _slotCount;

        /// <summary>Item in the currently selected slot, or null.</summary>
        public ItemData SelectedItem => GetAssignedItem(_selectedIndex);

        /// <summary>Get the item assigned to a specific slot.</summary>
        public ItemData GetAssignedItem(int index)
        {
            if (_assignedItems == null || index < 0 || index >= _assignedItems.Length)
                return null;
            // Verify it's still in inventory
            var item = _assignedItems[index];
            if (item != null && _inventory != null && !_inventory.HasItem(item.itemID))
            {
                _assignedItems[index] = null;
                RefreshSlots();
                return null;
            }
            return item;
        }

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            _inventory = GetComponent<InventoryManager>();
            if (_inventory == null)
                _inventory = GetComponentInParent<InventoryManager>();

            _sanity = GetComponent<SanitySystem>();
            if (_sanity == null)
                _sanity = GetComponentInParent<SanitySystem>();

            _assignedItems = new ItemData[_slotCount];
        }

        private void Start()
        {
            BuildUI();
            RefreshSlots();
        }

        private void OnEnable()
        {
            if (_inventory != null)
            {
                _inventory.ItemRemoved += OnItemRemoved;
                _inventory.InventoryChanged += OnInventoryChanged;
            }
        }

        private void OnDisable()
        {
            if (_inventory != null)
            {
                _inventory.ItemRemoved -= OnItemRemoved;
                _inventory.InventoryChanged -= OnInventoryChanged;
            }
        }

        private void Update()
        {
            HandleNumberKeys();
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Assigns an item to a specific quickslot. Called from InventoryUI.
        /// </summary>
        public void AssignItem(int slotIndex, ItemData item)
        {
            if (slotIndex < 0 || slotIndex >= _slotCount) return;

            // Remove from any other slot first
            for (int i = 0; i < _assignedItems.Length; i++)
            {
                if (_assignedItems[i] == item)
                    _assignedItems[i] = null;
            }

            _assignedItems[slotIndex] = item;
            RefreshSlots();
            Debug.Log($"[Hotbar] Assigned '{item.displayName}' to slot {slotIndex + 1}");
        }

        /// <summary>
        /// Removes an item from all quickslots.
        /// </summary>
        public void UnassignItem(ItemData item)
        {
            if (item == null) return;
            for (int i = 0; i < _assignedItems.Length; i++)
            {
                if (_assignedItems[i] == item)
                    _assignedItems[i] = null;
            }
            RefreshSlots();
        }

        /// <summary>
        /// Uses the item in the currently selected quickslot.
        /// </summary>
        public void UseSelectedItem()
        {
            var item = GetAssignedItem(_selectedIndex);
            if (item == null) return;

            // Consumable items (e.g., sanity pills)
            if (item.itemType == ItemType.Consumable)
            {
                UseConsumable(item);
                return;
            }

            Debug.Log($"[Hotbar] Used: {item.displayName}");
        }

        /// <summary>Select a hotbar slot by index (0-based).</summary>
        public void SelectSlot(int index)
        {
            if (index < 0 || index >= _slotCount) return;
            _selectedIndex = index;
            UpdateHighlight();
        }

        // =====================================================================
        // INPUT
        // =====================================================================

        private void HandleNumberKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            for (int i = 0; i < _slotCount && i < 5; i++)
            {
                Key key = Key.Digit1 + i;
                if (kb[key].wasPressedThisFrame)
                {
                    if (_selectedIndex == i)
                    {
                        // Press same key again = use item
                        UseSelectedItem();
                    }
                    else
                    {
                        SelectSlot(i);
                    }
                    return;
                }
            }
        }

        // =====================================================================
        // CONSUMABLE LOGIC
        // =====================================================================

        /// <summary>
        /// Called externally (e.g., from InventoryUI) to use a consumable item directly.
        /// </summary>
        public void UseConsumableDirectly(ItemData item) => UseConsumable(item);

        private void UseConsumable(ItemData item)
        {
            if (item.itemID.Contains("medicine") || item.itemID.Contains("pill") ||
                item.itemID.Contains("sedative"))
            {
                // Restore sanity
                if (_sanity != null)
                {
                    _sanity.RestoreSanity(30f);
                    Debug.Log($"[Hotbar] Used {item.displayName} — restored 30 sanity");
                }
            }
            else
            {
                Debug.Log($"[Hotbar] Used consumable: {item.displayName}");
            }

            // Remove from inventory and quickslot
            _inventory.RemoveItem(item);
            UnassignItem(item);
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private void OnItemRemoved(ItemData item) => UnassignItem(item);
        private void OnInventoryChanged() => RefreshSlots();

        // =====================================================================
        // UI BUILDING (runtime, no prefab)
        // =====================================================================

        private void BuildUI()
        {
            // --- Canvas ---
            var canvasGO = new GameObject("HotbarCanvas");
            canvasGO.transform.SetParent(transform, false);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 15;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // --- Bar root (centered bottom) ---
            var barGO = new GameObject("HotbarRoot");
            barGO.transform.SetParent(canvasGO.transform, false);
            _barRoot = barGO.AddComponent<RectTransform>();
            _barRoot.anchorMin = new Vector2(0.5f, 0f);
            _barRoot.anchorMax = new Vector2(0.5f, 0f);
            _barRoot.pivot = new Vector2(0.5f, 0f);
            _barRoot.anchoredPosition = new Vector2(0f, 20f);

            float totalWidth = _slotCount * _slotSize + (_slotCount - 1) * _slotGap;
            _barRoot.sizeDelta = new Vector2(totalWidth, _slotSize + 20f);

            // --- Slots ---
            float startX = -totalWidth / 2f + _slotSize / 2f;

            for (int i = 0; i < _slotCount; i++)
            {
                var slot = CreateSlot(i, startX + i * (_slotSize + _slotGap));
                _slots.Add(slot);
            }
        }

        private SlotView CreateSlot(int index, float xPos)
        {
            SlotView view;

            // Slot root
            var slotGO = new GameObject($"Slot_{index + 1}");
            slotGO.transform.SetParent(_barRoot, false);
            view.Root = slotGO.AddComponent<RectTransform>();
            view.Root.anchorMin = new Vector2(0.5f, 0.5f);
            view.Root.anchorMax = new Vector2(0.5f, 0.5f);
            view.Root.pivot = new Vector2(0.5f, 0.5f);
            view.Root.anchoredPosition = new Vector2(xPos, 4f);
            view.Root.sizeDelta = new Vector2(_slotSize, _slotSize);

            // Background image
            view.Background = slotGO.AddComponent<Image>();
            view.Background.color = _normalColor;

            // Border
            var borderGO = new GameObject("Border");
            borderGO.transform.SetParent(view.Root, false);
            var borderRT = borderGO.AddComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.offsetMin = new Vector2(-2, -2);
            borderRT.offsetMax = new Vector2(2, 2);
            borderRT.SetAsFirstSibling();
            var borderImg = borderGO.AddComponent<Image>();
            borderImg.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);

            // Icon
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(view.Root, false);
            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.1f, 0.1f);
            iconRT.anchorMax = new Vector2(0.9f, 0.9f);
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;
            view.Icon = iconGO.AddComponent<Image>();
            view.Icon.color = _emptyIconColor;
            view.Icon.preserveAspect = true;

            // Key label
            var labelGO = new GameObject("KeyLabel");
            labelGO.transform.SetParent(view.Root, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 1f);
            labelRT.anchorMax = new Vector2(0f, 1f);
            labelRT.pivot = new Vector2(0f, 1f);
            labelRT.anchoredPosition = new Vector2(3f, -1f);
            labelRT.sizeDelta = new Vector2(20f, 16f);
            view.KeyLabel = labelGO.AddComponent<TextMeshProUGUI>();
            view.KeyLabel.text = (index + 1).ToString();
            view.KeyLabel.fontSize = 11;
            view.KeyLabel.color = new Color(1f, 1f, 1f, 0.55f);
            view.KeyLabel.alignment = TextAlignmentOptions.TopLeft;

            return view;
        }

        // =====================================================================
        // REFRESH
        // =====================================================================

        private void RefreshSlots()
        {
            if (_assignedItems == null) return;

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                var item = GetAssignedItem(i);

                if (item != null && item.icon != null)
                {
                    slot.Icon.sprite = item.icon;
                    slot.Icon.color = Color.white;
                }
                else
                {
                    slot.Icon.sprite = null;
                    slot.Icon.color = _emptyIconColor;
                }
            }

            UpdateHighlight();
        }

        private void UpdateHighlight()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].Background.color = (i == _selectedIndex) ? _selectedColor : _normalColor;
            }
        }
    }
}
