// ============================================================================
// InventoryUI.cs — Self-building inventory grid UI
// Builds its own Canvas, grid, slots, and item detail panel at runtime.
// Toggle with Tab key. Integrates with InventoryManager for live updates.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using FracturedEchoes.InventorySystem;
using FracturedEchoes.ScriptableObjects;

namespace FracturedEchoes.UI
{
    /// <summary>
    /// Self-building inventory UI. Creates a grid of item slots, a detail panel,
    /// and handles item selection, use, inspect, and drop actions.
    /// Attach to the same GameObject as InventoryManager or any persistent object.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("References")]
        [Tooltip("The InventoryManager to display. Auto-found if null.")]
        [SerializeField] private InventoryManager _inventory;

        [Header("Layout")]
        [SerializeField] private int _columns = 4;
        [SerializeField] private float _slotSize = 80f;
        [SerializeField] private float _slotSpacing = 8f;
        [SerializeField] private float _panelPadding = 16f;

        // =====================================================================
        // RUNTIME UI REFERENCES
        // =====================================================================

        private Canvas _canvas;
        private GameObject _rootPanel;
        private GameObject _detailPanel;
        private TextMeshProUGUI _detailName;
        private TextMeshProUGUI _detailDescription;
        private Button _useButton;
        private Button _inspectButton;
        private Button _dropButton;
        private Transform _slotContainer;
        private GameObject _slotPrefab;

        // =====================================================================
        // STATE
        // =====================================================================

        private bool _isOpen;
        private int _selectedIndex = -1;
        private InputAction _inventoryAction;
        private Button[] _slotButtons;
        private Image[] _slotIcons;
        private GameObject[] _slotHighlights;

        // =====================================================================
        // PROPERTIES
        // =====================================================================

        public bool IsOpen => _isOpen;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            if (_inventory == null)
                _inventory = FindFirstObjectByType<InventoryManager>();

            _inventoryAction = InputSystem.actions?.FindAction("Player/Inventory");

            BuildUI();
            _rootPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (_inventory != null)
            {
                _inventory.InventoryChanged += RefreshSlots;
            }
        }

        private void OnDisable()
        {
            if (_inventory != null)
            {
                _inventory.InventoryChanged -= RefreshSlots;
            }
        }

        private void Update()
        {
            // Toggle with Tab (or Player/Inventory action if bound)
            bool toggle = _inventoryAction?.WasPressedThisFrame() ?? false;
            if (!toggle && Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
                toggle = true;

            if (toggle)
            {
                if (_isOpen) Close();
                else Open();
            }
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;
            _rootPanel.SetActive(true);
            _selectedIndex = -1;
            UpdateDetailPanel();
            RefreshSlots();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            _rootPanel.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
        }

        // =====================================================================
        // UI CONSTRUCTION
        // =====================================================================

        private void BuildUI()
        {
            // Canvas
            GameObject canvasGO = new GameObject("InventoryCanvas");
            canvasGO.transform.SetParent(transform, false);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Root panel (dark semi-transparent background)
            _rootPanel = CreatePanel(canvasGO.transform, "InventoryRoot",
                Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.85f));
            var rootRT = _rootPanel.GetComponent<RectTransform>();
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;

            // Title
            CreateText(_rootPanel.transform, "Title", "INVENTORY",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -30), new Vector2(300, 40), 28, Color.white);

            // Grid area (left side)
            int rows = Mathf.CeilToInt((float)_inventory.SlotCount / _columns);
            float gridW = _columns * (_slotSize + _slotSpacing) + _panelPadding * 2;
            float gridH = rows * (_slotSize + _slotSpacing) + _panelPadding * 2;

            GameObject gridPanel = CreatePanel(_rootPanel.transform, "GridPanel",
                new Vector2(0.3f, 0.5f), new Vector2(0.3f, 0.5f),
                new Color(0.1f, 0.1f, 0.12f, 0.95f));
            var gridRT = gridPanel.GetComponent<RectTransform>();
            gridRT.sizeDelta = new Vector2(gridW, gridH);

            // Slot container with grid layout
            GameObject slotContainerGO = new GameObject("SlotContainer");
            slotContainerGO.transform.SetParent(gridPanel.transform, false);
            var scRT = slotContainerGO.AddComponent<RectTransform>();
            scRT.anchorMin = Vector2.zero;
            scRT.anchorMax = Vector2.one;
            scRT.offsetMin = new Vector2(_panelPadding, _panelPadding);
            scRT.offsetMax = new Vector2(-_panelPadding, -_panelPadding);

            var gridLayout = slotContainerGO.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(_slotSize, _slotSize);
            gridLayout.spacing = new Vector2(_slotSpacing, _slotSpacing);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = _columns;
            gridLayout.childAlignment = TextAnchor.UpperLeft;

            _slotContainer = slotContainerGO.transform;

            // Create slots
            int totalSlots = _inventory.SlotCount;
            _slotButtons = new Button[totalSlots];
            _slotIcons = new Image[totalSlots];
            _slotHighlights = new GameObject[totalSlots];

            for (int i = 0; i < totalSlots; i++)
            {
                CreateSlot(i);
            }

            // Detail panel (right side)
            _detailPanel = CreatePanel(_rootPanel.transform, "DetailPanel",
                new Vector2(0.7f, 0.5f), new Vector2(0.7f, 0.5f),
                new Color(0.1f, 0.1f, 0.12f, 0.95f));
            var detailRT = _detailPanel.GetComponent<RectTransform>();
            detailRT.sizeDelta = new Vector2(320, gridH);

            // Detail: Item name
            _detailName = CreateText(_detailPanel.transform, "ItemName", "",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -20), new Vector2(280, 36), 22, Color.white)
                .GetComponent<TextMeshProUGUI>();

            // Detail: Description
            _detailDescription = CreateText(_detailPanel.transform, "ItemDesc", "",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -70), new Vector2(280, 120), 16, new Color(0.7f, 0.7f, 0.7f))
                .GetComponent<TextMeshProUGUI>();
            _detailDescription.textWrappingMode = TextWrappingModes.Normal;
            _detailDescription.alignment = TextAlignmentOptions.TopLeft;

            // Action buttons
            float btnY = -210f;
            float btnH = 36f;
            float btnGap = 8f;

            _useButton = CreateButton(_detailPanel.transform, "UseBtn", "USE",
                new Vector2(0, btnY), new Vector2(280, btnH),
                new Color(0.2f, 0.5f, 0.2f));
            _useButton.onClick.AddListener(OnUseClicked);

            btnY -= (btnH + btnGap);
            _inspectButton = CreateButton(_detailPanel.transform, "InspectBtn", "INSPECT",
                new Vector2(0, btnY), new Vector2(280, btnH),
                new Color(0.3f, 0.3f, 0.5f));
            _inspectButton.onClick.AddListener(OnInspectClicked);

            btnY -= (btnH + btnGap);
            _dropButton = CreateButton(_detailPanel.transform, "DropBtn", "DROP",
                new Vector2(0, btnY), new Vector2(280, btnH),
                new Color(0.5f, 0.2f, 0.2f));
            _dropButton.onClick.AddListener(OnDropClicked);

            // Close button (top-right corner)
            var closeBtn = CreateButton(_rootPanel.transform, "CloseBtn", "X",
                new Vector2(0, 0), new Vector2(40, 40),
                new Color(0.5f, 0.15f, 0.15f));
            var closeBtnRT = closeBtn.GetComponent<RectTransform>();
            closeBtnRT.anchorMin = new Vector2(1, 1);
            closeBtnRT.anchorMax = new Vector2(1, 1);
            closeBtnRT.anchoredPosition = new Vector2(-30, -30);
            closeBtn.onClick.AddListener(Close);

            // Instructions text
            CreateText(_rootPanel.transform, "Instructions",
                "Tab — Close  |  Click Slot — Select  |  Use / Inspect / Drop",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 30), new Vector2(600, 30), 14, new Color(0.5f, 0.5f, 0.5f));
        }

        private void CreateSlot(int index)
        {
            GameObject slotGO = new GameObject($"Slot_{index}");
            slotGO.transform.SetParent(_slotContainer, false);

            var slotImg = slotGO.AddComponent<Image>();
            slotImg.color = new Color(0.18f, 0.18f, 0.2f);

            var btn = slotGO.AddComponent<Button>();
            int capturedIndex = index;
            btn.onClick.AddListener(() => OnSlotClicked(capturedIndex));
            _slotButtons[index] = btn;

            // Color block for hover/pressed
            var colors = btn.colors;
            colors.normalColor = new Color(0.18f, 0.18f, 0.2f);
            colors.highlightedColor = new Color(0.28f, 0.28f, 0.32f);
            colors.pressedColor = new Color(0.35f, 0.35f, 0.4f);
            colors.selectedColor = new Color(0.25f, 0.25f, 0.3f);
            btn.colors = colors;

            // Item icon
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(slotGO.transform, false);
            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.1f, 0.1f);
            iconRT.anchorMax = new Vector2(0.9f, 0.9f);
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.color = Color.white;
            iconGO.SetActive(false);
            _slotIcons[index] = iconImg;

            // Selection highlight border
            GameObject highlightGO = new GameObject("Highlight");
            highlightGO.transform.SetParent(slotGO.transform, false);
            var hlRT = highlightGO.AddComponent<RectTransform>();
            hlRT.anchorMin = Vector2.zero;
            hlRT.anchorMax = Vector2.one;
            hlRT.offsetMin = Vector2.zero;
            hlRT.offsetMax = Vector2.zero;
            var hlImg = highlightGO.AddComponent<Image>();
            hlImg.color = new Color(0.9f, 0.75f, 0.3f, 0.4f);
            // Make it an outline by setting fill to transparent, but Unity UI doesn't
            // support outline natively — we'll use a full border tint
            highlightGO.SetActive(false);
            _slotHighlights[index] = highlightGO;
        }

        // =====================================================================
        // SLOT / DETAIL UPDATES
        // =====================================================================

        private void RefreshSlots()
        {
            if (_slotIcons == null || _inventory == null) return;

            for (int i = 0; i < _inventory.SlotCount; i++)
            {
                if (i < _inventory.ItemCount)
                {
                    var item = _inventory.Items[i];
                    if (item.icon != null)
                    {
                        _slotIcons[i].sprite = item.icon;
                        _slotIcons[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        // No icon — show a placeholder via color
                        _slotIcons[i].sprite = null;
                        _slotIcons[i].color = new Color(0.5f, 0.7f, 0.5f, 0.6f);
                        _slotIcons[i].gameObject.SetActive(true);
                    }
                }
                else
                {
                    _slotIcons[i].gameObject.SetActive(false);
                }

                // Update highlight
                bool isSelected = (i == _selectedIndex);
                _slotHighlights[i].SetActive(isSelected);
            }

            UpdateDetailPanel();
        }

        private void UpdateDetailPanel()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _inventory.ItemCount)
            {
                var item = _inventory.Items[_selectedIndex];
                _detailName.text = item.displayName;
                _detailDescription.text = item.description;
                _useButton.gameObject.SetActive(item.canUseOnEnvironment);
                _inspectButton.gameObject.SetActive(item.inspectionPrefab != null);
                _dropButton.gameObject.SetActive(true);
            }
            else
            {
                _detailName.text = "No Item Selected";
                _detailDescription.text = "Click on an item slot to view details.";
                _useButton.gameObject.SetActive(false);
                _inspectButton.gameObject.SetActive(false);
                _dropButton.gameObject.SetActive(false);
            }
        }

        // =====================================================================
        // CALLBACKS
        // =====================================================================

        private void OnSlotClicked(int index)
        {
            if (index < _inventory.ItemCount)
            {
                _selectedIndex = index;
            }
            else
            {
                _selectedIndex = -1;
            }
            RefreshSlots();
        }

        private void OnUseClicked()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _inventory.ItemCount) return;
            var item = _inventory.Items[_selectedIndex];
            Debug.Log($"[InventoryUI] Use: {item.displayName} (select a target in the world)");
            // Close inventory to let player select a target in the world
            Close();
        }

        private void OnInspectClicked()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _inventory.ItemCount) return;
            var item = _inventory.Items[_selectedIndex];
            _inventory.StartInspection(item);
            Close();
        }

        private void OnDropClicked()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _inventory.ItemCount) return;
            var item = _inventory.Items[_selectedIndex];
            _inventory.RemoveItem(item);
            Debug.Log($"[InventoryUI] Dropped: {item.displayName}");
            _selectedIndex = -1;
            RefreshSlots();
        }

        // =====================================================================
        // UI FACTORY HELPERS
        // =====================================================================

        private static GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static GameObject CreateText(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size,
            float fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return go;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 position, Vector2 size, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.3f;
            colors.pressedColor = bgColor * 0.7f;
            btn.colors = colors;

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }
    }
}
