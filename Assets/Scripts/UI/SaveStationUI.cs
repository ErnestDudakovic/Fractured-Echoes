// ============================================================================
// SaveStationUI.cs — Self-building save/load UI panel
// Opens when the player interacts with a SaveStation. Shows save slots
// with Save, Load, and Delete buttons. Pauses the game while open.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FracturedEchoes.Core.SaveLoad;
using FracturedEchoes.Player;
using UnityEngine.InputSystem;

namespace FracturedEchoes.UI
{
    /// <summary>
    /// In-game save/load UI that appears when interacting with a save station.
    /// Builds its own Canvas + UI hierarchy at Awake, hidden by default.
    /// </summary>
    public class SaveStationUI : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("References")]
        [SerializeField] private SaveSystem _saveSystem;

        [Header("Configuration")]
        [SerializeField] private int _maxSlots = 3;

        // =====================================================================
        // PRIVATE STATE — built at runtime
        // =====================================================================

        private Canvas _canvas;
        private GameObject _panel;
        private GameObject _slotContainer;
        private TextMeshProUGUI _statusText;
        private bool _isOpen;
        private FirstPersonController _cachedPlayer;

        // Colours
        private static readonly Color BG_COLOR = new Color(0.02f, 0.02f, 0.05f, 0.92f);
        private static readonly Color SLOT_BG = new Color(0.08f, 0.08f, 0.12f, 1f);
        private static readonly Color SLOT_HOVER = new Color(0.12f, 0.12f, 0.18f, 1f);
        private static readonly Color SAVE_BTN = new Color(0.15f, 0.55f, 0.25f, 1f);
        private static readonly Color LOAD_BTN = new Color(0.2f, 0.45f, 0.7f, 1f);
        private static readonly Color DELETE_BTN = new Color(0.6f, 0.15f, 0.15f, 1f);
        private static readonly Color CLOSE_BTN = new Color(0.35f, 0.35f, 0.4f, 1f);
        private static readonly Color TEXT_COLOR = new Color(0.85f, 0.85f, 0.9f, 1f);
        private static readonly Color DIM_TEXT = new Color(0.5f, 0.5f, 0.55f, 1f);

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            BuildUI();
            _panel.SetActive(false);
        }

        private void Update()
        {
            if (!_isOpen) return;

            // Close on Escape
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>Opens the save station panel, pauses the game, shows cursor.</summary>
        public void Open()
        {
            if (_isOpen) return;

            _isOpen = true;
            _panel.SetActive(true);

            // Pause + unlock cursor
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Lock player input
            _cachedPlayer ??= FindFirstObjectByType<FirstPersonController>();
            if (_cachedPlayer != null) _cachedPlayer.SetInputLocked(true);

            RefreshSlots();
            SetStatus("");
        }

        /// <summary>Closes the panel, resumes game.</summary>
        public void Close()
        {
            if (!_isOpen) return;

            _isOpen = false;
            _panel.SetActive(false);

            // Unpause + relock cursor
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Unlock player input
            _cachedPlayer ??= FindFirstObjectByType<FirstPersonController>();
            if (_cachedPlayer != null) _cachedPlayer.SetInputLocked(false);
        }

        // =====================================================================
        // UI BUILDING
        // =====================================================================

        private void BuildUI()
        {
            // ── Canvas ─────────────────────────────────────────────────────
            GameObject canvasGo = new GameObject("SaveStationCanvas");
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // ── Background panel ────────────────────────────────────────────
            _panel = CreatePanel(canvasGo.transform, "SavePanel", BG_COLOR);
            Stretch(_panel.GetComponent<RectTransform>());

            // ── Header ──────────────────────────────────────────────────────
            var headerTMP = CreateTMP(_panel.transform, "Header",
                "SAVE STATION", 36, TextAlignmentOptions.Center, TEXT_COLOR);
            var headerRT = headerTMP.rectTransform;
            headerRT.anchorMin = new Vector2(0.5f, 1f);
            headerRT.anchorMax = new Vector2(0.5f, 1f);
            headerRT.pivot = new Vector2(0.5f, 1f);
            headerRT.anchoredPosition = new Vector2(0, -30f);
            headerRT.sizeDelta = new Vector2(500, 50);

            // ── Subtitle ────────────────────────────────────────────────────
            var subtitleTMP = CreateTMP(_panel.transform, "Subtitle",
                "Select a slot to save or load", 18, TextAlignmentOptions.Center, DIM_TEXT);
            var subRT = subtitleTMP.rectTransform;
            subRT.anchorMin = new Vector2(0.5f, 1f);
            subRT.anchorMax = new Vector2(0.5f, 1f);
            subRT.pivot = new Vector2(0.5f, 1f);
            subRT.anchoredPosition = new Vector2(0, -85f);
            subRT.sizeDelta = new Vector2(500, 30);

            // ── Slot container ──────────────────────────────────────────────
            _slotContainer = new GameObject("SlotContainer");
            _slotContainer.transform.SetParent(_panel.transform, false);

            var slotContainerRT = _slotContainer.AddComponent<RectTransform>();
            slotContainerRT.anchorMin = new Vector2(0.5f, 0.5f);
            slotContainerRT.anchorMax = new Vector2(0.5f, 0.5f);
            slotContainerRT.pivot = new Vector2(0.5f, 0.5f);
            slotContainerRT.anchoredPosition = new Vector2(0, 20f);
            slotContainerRT.sizeDelta = new Vector2(700, 400);

            var layout = _slotContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.padding = new RectOffset(10, 10, 10, 10);

            // ── Status text ─────────────────────────────────────────────────
            _statusText = CreateTMP(_panel.transform, "StatusText",
                "", 20, TextAlignmentOptions.Center, new Color(0.3f, 0.9f, 0.4f));
            var statusRT = _statusText.rectTransform;
            statusRT.anchorMin = new Vector2(0.5f, 0f);
            statusRT.anchorMax = new Vector2(0.5f, 0f);
            statusRT.pivot = new Vector2(0.5f, 0f);
            statusRT.anchoredPosition = new Vector2(0, 100f);
            statusRT.sizeDelta = new Vector2(600, 35);

            // ── Close button ────────────────────────────────────────────────
            var closeBtn = CreateButton(_panel.transform, "CloseButton",
                "CLOSE [ESC]", CLOSE_BTN, 160, 44);
            var closeBtnRT = closeBtn.GetComponent<RectTransform>();
            closeBtnRT.anchorMin = new Vector2(0.5f, 0f);
            closeBtnRT.anchorMax = new Vector2(0.5f, 0f);
            closeBtnRT.pivot = new Vector2(0.5f, 0f);
            closeBtnRT.anchoredPosition = new Vector2(0, 40f);
            closeBtn.onClick.AddListener(Close);
        }

        // =====================================================================
        // SLOT REFRESH
        // =====================================================================

        private void RefreshSlots()
        {
            // Destroy existing slot rows
            for (int i = _slotContainer.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(_slotContainer.transform.GetChild(i).gameObject);
            }

            for (int i = 0; i < _maxSlots; i++)
            {
                BuildSlotRow(i);
            }
        }

        private void BuildSlotRow(int slotIndex)
        {
            // ── Row container ───────────────────────────────────────────────
            GameObject row = CreatePanel(_slotContainer.transform,
                $"Slot_{slotIndex}", SLOT_BG);
            var rowRT = row.GetComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(0, 90);

            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 90;
            rowLayout.preferredHeight = 90;

            var hGroup = row.AddComponent<HorizontalLayoutGroup>();
            hGroup.spacing = 10f;
            hGroup.padding = new RectOffset(16, 16, 10, 10);
            hGroup.childForceExpandWidth = false;
            hGroup.childForceExpandHeight = true;
            hGroup.childControlWidth = false;
            hGroup.childControlHeight = true;
            hGroup.childAlignment = TextAnchor.MiddleLeft;

            // ── Slot info ───────────────────────────────────────────────────
            bool exists = _saveSystem != null && _saveSystem.SaveExists(slotIndex);

            // Info text (slot number + metadata)
            string infoText;
            if (exists && _saveSystem != null)
            {
                SaveSlotInfo info = _saveSystem.GetSlotInfo(slotIndex);
                if (info != null)
                {
                    string playTime = FormatPlayTime(info.playTime);
                    infoText = $"<b>Slot {slotIndex + 1}</b>\n" +
                               $"<size=14>{info.locationName}  |  {info.timestamp}  |  {playTime}</size>";
                }
                else
                {
                    infoText = $"<b>Slot {slotIndex + 1}</b>\n<size=14>Save data</size>";
                }
            }
            else
            {
                infoText = $"<b>Slot {slotIndex + 1}</b>\n<size=14><color=#666>— Empty —</color></size>";
            }

            var infoTMP = CreateTMP(row.transform, "Info", infoText,
                18, TextAlignmentOptions.MidlineLeft, TEXT_COLOR);
            var infoLE = infoTMP.gameObject.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1f;
            infoLE.minWidth = 300;

            // ── Buttons ─────────────────────────────────────────────────────
            int idx = slotIndex; // capture for lambdas

            // Save button (always available)
            var saveBtn = CreateButton(row.transform, "SaveBtn", "SAVE", SAVE_BTN, 90, 36);
            saveBtn.onClick.AddListener(() => OnSave(idx));

            // Load button (only if save exists)
            var loadBtn = CreateButton(row.transform, "LoadBtn", "LOAD", LOAD_BTN, 90, 36);
            loadBtn.interactable = exists;
            if (exists) loadBtn.onClick.AddListener(() => OnLoad(idx));

            // Delete button (only if save exists)
            var deleteBtn = CreateButton(row.transform, "DeleteBtn", "DEL", DELETE_BTN, 64, 36);
            deleteBtn.interactable = exists;
            if (exists) deleteBtn.onClick.AddListener(() => OnDelete(idx));
        }

        // =====================================================================
        // BUTTON HANDLERS
        // =====================================================================

        private void OnSave(int slotIndex)
        {
            if (_saveSystem == null)
            {
                SetStatus("<color=#FF4444>No SaveSystem found!</color>");
                return;
            }

            // Temporarily unpause so the save captures real game state
            Time.timeScale = 1f;
            _saveSystem.SaveToSlot(slotIndex);
            Time.timeScale = 0f;

            // Also save to cloud if available
            if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.IsReady)
            {
                CloudSaveManager.Instance.SaveToCloud(slotIndex);
                SetStatus($"Saved to Slot {slotIndex + 1} (local + cloud)");
            }
            else
            {
                SetStatus($"Saved to Slot {slotIndex + 1}");
            }

            RefreshSlots();
        }

        private void OnLoad(int slotIndex)
        {
            if (_saveSystem == null)
            {
                SetStatus("<color=#FF4444>No SaveSystem found!</color>");
                return;
            }

            _saveSystem.LoadFromSlot(slotIndex);
            SetStatus($"Loaded Slot {slotIndex + 1}");

            // Close the UI after a short delay so the player sees the message
            Invoke(nameof(Close), 0.3f);
        }

        private void OnDelete(int slotIndex)
        {
            if (_saveSystem == null) return;

            _saveSystem.DeleteSave(slotIndex);
            SetStatus($"Deleted Slot {slotIndex + 1}");
            RefreshSlots();
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
        }

        // =====================================================================
        // UI HELPER METHODS
        // =====================================================================

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = color;

            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI CreateTMP(Transform parent, string name,
            string text, int fontSize, TextAlignmentOptions align, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.richText = true;
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

            return tmp;
        }

        private static Button CreateButton(Transform parent, string name,
            string label, Color bgColor, float width, float height)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            btn.colors = colors;

            // Add LayoutElement so it doesn't get stretched
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = width;
            le.preferredWidth = width;
            le.minHeight = height;
            le.preferredHeight = height;

            // Label
            var tmp = CreateTMP(go.transform, "Label", label,
                14, TextAlignmentOptions.Center, Color.white);
            Stretch(tmp.rectTransform);

            return btn;
        }

        private static string FormatPlayTime(float seconds)
        {
            int hrs = Mathf.FloorToInt(seconds / 3600f);
            int mins = Mathf.FloorToInt((seconds % 3600f) / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);

            if (hrs > 0)
                return $"{hrs}h {mins:D2}m";
            else
                return $"{mins}m {secs:D2}s";
        }
    }
}
