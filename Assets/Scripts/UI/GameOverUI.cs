// ============================================================================
// GameOverUI.cs — Self-building Game Over screen
// Displays when sanity reaches 0. Offers Return to Main Menu,
// Load Save (opens save slot picker), and Load Latest Save options.
// Listens to SanitySystem.SanityChanged to trigger automatically.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FracturedEchoes.Player;
using FracturedEchoes.Core;
using FracturedEchoes.Core.SaveLoad;

namespace FracturedEchoes.UI
{
    /// <summary>
    /// Self-building Game Over screen that triggers when sanity depletes.
    /// Attach to the same GameObject as SanitySystem (the Player), or any
    /// persistent object in the scene.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("References (auto-found if null)")]
        [SerializeField] private SanitySystem _sanity;
        [SerializeField] private SaveSystem _saveSystem;

        [Header("Settings")]
        [Tooltip("Delay before showing the Game Over screen after sanity hits 0.")]
        [SerializeField] private float _showDelay = 1.5f;

        [Tooltip("Main Menu scene name.")]
        [SerializeField] private string _mainMenuScene = "MainMenu";

        // =====================================================================
        // RUNTIME UI
        // =====================================================================

        private Canvas _canvas;
        private GameObject _rootPanel;
        private GameObject _saveSlotPanel;
        private bool _isShowing;
        private bool _triggered;
        private float _triggerTime;
        private bool _initialized;
        private FirstPersonController _playerController;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            if (_sanity == null)
                _sanity = FindFirstObjectByType<SanitySystem>();
            if (_saveSystem == null)
                _saveSystem = FindFirstObjectByType<SaveSystem>();

            _playerController = FindFirstObjectByType<FirstPersonController>();

            _initialized = false;

            BuildUI();
            _rootPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (_sanity != null)
                _sanity.SanityChanged += OnSanityChanged;
        }

        private void OnDisable()
        {
            if (_sanity != null)
                _sanity.SanityChanged -= OnSanityChanged;
        }

        /// <summary>
        /// Wait a couple of frames before listening to sanity events,
        /// because the SanitySystem may fire events during initialization
        /// (RestoreState, Start, etc.) before sanity has its real value.
        /// </summary>
        private System.Collections.IEnumerator InitGuard()
        {
            // Wait 2 frames for all Awake/Start/RestoreState to complete
            yield return null;
            yield return null;

            // Now check the actual sanity value — if it's above 0, we're fine
            if (_sanity != null && _sanity.CurrentSanity > 0f)
            {
                _initialized = true;
            }
            else if (_sanity == null)
            {
                _initialized = true; // no sanity system means we can't trigger anyway
            }
            else
            {
                // Sanity is actually 0 after init — could be a loaded save with 0. 
                // Still allow it but with a sanity re-check.
                _initialized = true;
            }
        }

        private void Start()
        {
            StartCoroutine(InitGuard());
        }

        private void Update()
        {
            // Delayed show after sanity depletion
            if (_triggered && !_isShowing)
            {
                if (Time.unscaledTime - _triggerTime >= _showDelay)
                {
                    Show();
                }
            }
        }

        // =====================================================================
        // SANITY CALLBACK
        // =====================================================================

        private void OnSanityChanged(float current, float max)
        {
            // Don't react until initialization is complete (a few frames after scene load)
            if (!_initialized) return;

            if (current <= 0f && !_triggered && !_isShowing)
            {
                _triggered = true;
                _triggerTime = Time.unscaledTime;
            }
        }

        // =====================================================================
        // SHOW / HIDE
        // =====================================================================

        private void Show()
        {
            if (_isShowing) return;

            // Double-check sanity is actually at 0 before showing
            if (_sanity != null && _sanity.CurrentSanity > 0f)
            {
                _triggered = false;
                return;
            }

            _isShowing = true;
            _rootPanel.SetActive(true);

            if (_saveSlotPanel != null)
                _saveSlotPanel.SetActive(false);

            // Pause game, unlock cursor
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Lock player input
            if (_playerController != null)
                _playerController.SetInputLocked(true);

            // Set game state
            var gsm = FindFirstObjectByType<GameStateManager>();
            if (gsm != null)
                gsm.SetGameState(GameState.GameOver);

            Debug.Log("[GameOverUI] Game Over — sanity depleted.");
        }

        private void Hide()
        {
            _isShowing = false;
            _triggered = false;
            _rootPanel.SetActive(false);
        }

        // =====================================================================
        // BUTTON CALLBACKS
        // =====================================================================

        private void OnReturnToMainMenu()
        {
            Hide();
            Time.timeScale = 1f;

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadMainMenu();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(_mainMenuScene);
            }
        }

        private void OnLoadSave()
        {
            // Toggle the save slot picker sub-panel
            if (_saveSlotPanel != null)
            {
                _saveSlotPanel.SetActive(!_saveSlotPanel.activeSelf);
            }
        }

        private void OnLoadLatestSave()
        {
            if (_saveSystem == null)
            {
                Debug.LogWarning("[GameOverUI] No SaveSystem found — cannot load.");
                return;
            }

            // Find the most recent save across all slots
            int latestSlot = -1;
            string latestTimestamp = null;

            for (int i = 0; i < 3; i++)
            {
                var info = _saveSystem.GetSlotInfo(i);
                if (info != null)
                {
                    // Compare timestamp strings (ISO format sorts lexicographically)
                    if (latestTimestamp == null ||
                        string.Compare(info.timestamp, latestTimestamp, System.StringComparison.Ordinal) > 0)
                    {
                        latestTimestamp = info.timestamp;
                        latestSlot = i;
                    }
                }
            }

            if (latestSlot >= 0)
            {
                Hide();
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                _saveSystem.LoadFromSlot(latestSlot);

                // Reset sanity trigger so we don't re-trigger immediately
                _triggered = false;

                if (_playerController != null)
                    _playerController.SetInputLocked(false);

                Debug.Log($"[GameOverUI] Loading latest save from slot {latestSlot}");
            }
            else
            {
                Debug.LogWarning("[GameOverUI] No saves found to load.");
            }
        }

        private void OnLoadSlot(int slotIndex)
        {
            if (_saveSystem == null) return;

            var info = _saveSystem.GetSlotInfo(slotIndex);
            if (info == null)
            {
                Debug.LogWarning($"[GameOverUI] Slot {slotIndex} is empty.");
                return;
            }

            Hide();
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _saveSystem.LoadFromSlot(slotIndex);

            _triggered = false;

            if (_playerController != null)
                _playerController.SetInputLocked(false);

            Debug.Log($"[GameOverUI] Loading save from slot {slotIndex}");
        }

        // =====================================================================
        // UI CONSTRUCTION
        // =====================================================================

        private void BuildUI()
        {
            // --- Canvas ---
            var canvasGO = new GameObject("GameOverCanvas");
            canvasGO.transform.SetParent(transform, false);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200; // Above everything
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // --- Root panel (full-screen dark overlay) ---
            _rootPanel = new GameObject("GameOverRoot");
            _rootPanel.transform.SetParent(canvasGO.transform, false);
            var rootRT = _rootPanel.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;
            var rootImg = _rootPanel.AddComponent<Image>();
            rootImg.color = new Color(0.05f, 0f, 0f, 0.92f);

            // --- Center container ---
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(_rootPanel.transform, false);
            var containerRT = containerGO.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0.5f, 0.5f);
            containerRT.anchorMax = new Vector2(0.5f, 0.5f);
            containerRT.sizeDelta = new Vector2(500, 420);

            // --- "GAME OVER" title ---
            var titleGO = CreateTextElement(containerGO.transform, "Title", "GAME OVER",
                new Vector2(0, 140), new Vector2(500, 60), 48,
                new Color(0.85f, 0.15f, 0.15f), TextAlignmentOptions.Center,
                FontStyles.Bold);

            // --- Subtitle ---
            CreateTextElement(containerGO.transform, "Subtitle",
                "Your mind has shattered.\nThe echoes have consumed you.",
                new Vector2(0, 70), new Vector2(450, 50), 18,
                new Color(0.6f, 0.4f, 0.4f), TextAlignmentOptions.Center,
                FontStyles.Italic);

            // --- Divider line ---
            var dividerGO = new GameObject("Divider");
            dividerGO.transform.SetParent(containerGO.transform, false);
            var divRT = dividerGO.AddComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0.5f, 0.5f);
            divRT.anchorMax = new Vector2(0.5f, 0.5f);
            divRT.anchoredPosition = new Vector2(0, 30);
            divRT.sizeDelta = new Vector2(350, 2);
            var divImg = dividerGO.AddComponent<Image>();
            divImg.color = new Color(0.5f, 0.15f, 0.15f, 0.6f);

            // --- Buttons ---
            float btnW = 320f;
            float btnH = 44f;
            float btnGap = 10f;
            float startY = -20f;

            // Return to Main Menu
            var mainMenuBtn = CreateUIButton(containerGO.transform, "MainMenuBtn",
                "RETURN TO MAIN MENU",
                new Vector2(0, startY), new Vector2(btnW, btnH),
                new Color(0.5f, 0.15f, 0.15f));
            mainMenuBtn.onClick.AddListener(OnReturnToMainMenu);

            // Load Save
            startY -= (btnH + btnGap);
            var loadSaveBtn = CreateUIButton(containerGO.transform, "LoadSaveBtn",
                "LOAD SAVE",
                new Vector2(0, startY), new Vector2(btnW, btnH),
                new Color(0.2f, 0.3f, 0.5f));
            loadSaveBtn.onClick.AddListener(OnLoadSave);

            // Load Latest Save
            startY -= (btnH + btnGap);
            var loadLatestBtn = CreateUIButton(containerGO.transform, "LoadLatestBtn",
                "LOAD LATEST SAVE",
                new Vector2(0, startY), new Vector2(btnW, btnH),
                new Color(0.2f, 0.45f, 0.25f));
            loadLatestBtn.onClick.AddListener(OnLoadLatestSave);

            // --- Save slot picker sub-panel (hidden by default) ---
            BuildSaveSlotPanel(containerGO.transform, startY - (btnH + btnGap + 10));
        }

        private void BuildSaveSlotPanel(Transform parent, float yPos)
        {
            _saveSlotPanel = new GameObject("SaveSlotPanel");
            _saveSlotPanel.transform.SetParent(parent, false);
            var panelRT = _saveSlotPanel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = new Vector2(0, yPos);
            panelRT.sizeDelta = new Vector2(340, 140);

            var panelImg = _saveSlotPanel.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            // Title
            CreateTextElement(_saveSlotPanel.transform, "SlotTitle", "SELECT SAVE SLOT",
                new Vector2(0, 45), new Vector2(300, 24), 14,
                new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Center,
                FontStyles.Normal);

            // 3 slot buttons
            float slotBtnW = 90f;
            float slotBtnH = 36f;
            float totalW = 3 * slotBtnW + 2 * 10f;
            float slotStartX = -totalW / 2f + slotBtnW / 2f;

            for (int i = 0; i < 3; i++)
            {
                int captured = i;
                string label = $"Slot {i + 1}";

                // Check if slot has data
                if (_saveSystem != null)
                {
                    var info = _saveSystem.GetSlotInfo(i);
                    if (info == null)
                        label = $"Slot {i + 1}\n(Empty)";
                }

                var slotBtn = CreateUIButton(_saveSlotPanel.transform, $"Slot_{i}",
                    label,
                    new Vector2(slotStartX + i * (slotBtnW + 10f), -10),
                    new Vector2(slotBtnW, slotBtnH),
                    new Color(0.25f, 0.25f, 0.3f));

                // Make text smaller for slot buttons
                var slotLabel = slotBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (slotLabel != null) slotLabel.fontSize = 13;

                slotBtn.onClick.AddListener(() => OnLoadSlot(captured));
            }

            _saveSlotPanel.SetActive(false);
        }

        // =====================================================================
        // UI HELPERS
        // =====================================================================

        private static GameObject CreateTextElement(Transform parent, string name, string text,
            Vector2 position, Vector2 size, float fontSize, Color color,
            TextAlignmentOptions alignment, FontStyles fontStyle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = fontStyle;
            return go;
        }

        private static Button CreateUIButton(Transform parent, string name, string label,
            Vector2 position, Vector2 size, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.35f;
            colors.pressedColor = bgColor * 0.7f;
            colors.selectedColor = bgColor * 1.15f;
            btn.colors = colors;

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(8, 0);
            textRT.offsetMax = new Vector2(-8, 0);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }
    }
}
