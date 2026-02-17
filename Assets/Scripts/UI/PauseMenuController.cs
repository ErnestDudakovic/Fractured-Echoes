// ============================================================================
// PauseMenuController.cs — In-game pause menu with settings
// Handles: Resume, Settings (sensitivity, head bob, audio), Main Menu, Quit.
// Builds its own UI at runtime — no manual setup needed.
// Reads/writes PlayerPrefs and applies settings to player systems live.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using FracturedEchoes.Player;

namespace FracturedEchoes.UI
{
    /// <summary>
    /// In-game pause menu. Attach to any GameObject in the scene.
    /// Automatically builds its canvas on Awake and toggles on Escape.
    /// Finds the player's FirstPersonController to apply settings live.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        // =====================================================================
        // PREFS KEYS (shared with SettingsMenuController)
        // =====================================================================

        private const string KEY_MASTER_VOL  = "Settings_MasterVolume";
        private const string KEY_SENSITIVITY = "Settings_MouseSensitivity";
        private const string KEY_HEADBOB_ON  = "Settings_HeadBobEnabled";
        private const string KEY_HEADBOB_INT = "Settings_HeadBobIntensity";

        // =====================================================================
        // RUNTIME REFERENCES
        // =====================================================================

        private Canvas _canvas;
        private GameObject _pausePanel;
        private GameObject _settingsPanel;
        private GameObject _confirmPanel;

        // Settings controls
        private Slider _sensitivitySlider;
        private TextMeshProUGUI _sensitivityValueText;
        private Slider _headBobSlider;
        private TextMeshProUGUI _headBobValueText;
        private Toggle _headBobToggle;
        private Slider _masterVolSlider;
        private TextMeshProUGUI _masterVolValueText;

        private bool _isPaused;
        private FirstPersonController _playerController;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            BuildUI();
            _pausePanel.SetActive(false);
            _settingsPanel.SetActive(false);
            _confirmPanel.SetActive(false);
        }

        private void Start()
        {
            // Find player controller
            _playerController = FindFirstObjectByType<FirstPersonController>();
            LoadAndApplySettings();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                // If confirm dialog is open, close it
                if (_confirmPanel.activeSelf)
                {
                    _confirmPanel.SetActive(false);
                    return;
                }

                // If settings panel is open, go back to pause
                if (_settingsPanel.activeSelf)
                {
                    CloseSettings();
                    return;
                }

                // Toggle pause
                TogglePause();
            }
        }

        // =====================================================================
        // PAUSE CONTROL
        // =====================================================================

        public void TogglePause()
        {
            _isPaused = !_isPaused;

            _pausePanel.SetActive(_isPaused);
            _settingsPanel.SetActive(false);
            _confirmPanel.SetActive(false);

            Time.timeScale = _isPaused ? 0f : 1f;
            Cursor.lockState = _isPaused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = _isPaused;

            // Lock/unlock player input
            if (_playerController != null)
            {
                _playerController.SetInputLocked(_isPaused);
            }
        }

        private void Resume()
        {
            if (_isPaused) TogglePause();
        }

        // =====================================================================
        // SETTINGS
        // =====================================================================

        private void OpenSettings()
        {
            _pausePanel.SetActive(false);
            _settingsPanel.SetActive(true);
            LoadSettingsValues();
        }

        private void CloseSettings()
        {
            SaveAndApplySettings();
            _settingsPanel.SetActive(false);
            _pausePanel.SetActive(true);
        }

        private void LoadSettingsValues()
        {
            float sens = PlayerPrefs.GetFloat(KEY_SENSITIVITY, 2f);
            _sensitivitySlider.value = sens;
            UpdateSensLabel(sens);

            bool headBobOn = PlayerPrefs.GetInt(KEY_HEADBOB_ON, 1) == 1;
            _headBobToggle.isOn = headBobOn;

            float headBobInt = PlayerPrefs.GetFloat(KEY_HEADBOB_INT, 0.3f);
            _headBobSlider.value = headBobInt;
            UpdateHeadBobLabel(headBobInt);

            float masterVol = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
            _masterVolSlider.value = masterVol;
            UpdateMasterVolLabel(masterVol);
        }

        private void SaveAndApplySettings()
        {
            float sens = _sensitivitySlider.value;
            bool headBobOn = _headBobToggle.isOn;
            float headBobInt = _headBobSlider.value;
            float masterVol = _masterVolSlider.value;

            PlayerPrefs.SetFloat(KEY_SENSITIVITY, sens);
            PlayerPrefs.SetInt(KEY_HEADBOB_ON, headBobOn ? 1 : 0);
            PlayerPrefs.SetFloat(KEY_HEADBOB_INT, headBobInt);
            PlayerPrefs.SetFloat(KEY_MASTER_VOL, masterVol);
            PlayerPrefs.Save();

            ApplyToPlayer(sens, headBobOn, headBobInt, masterVol);
        }

        private void LoadAndApplySettings()
        {
            float sens = PlayerPrefs.GetFloat(KEY_SENSITIVITY, 2f);
            bool headBobOn = PlayerPrefs.GetInt(KEY_HEADBOB_ON, 1) == 1;
            float headBobInt = PlayerPrefs.GetFloat(KEY_HEADBOB_INT, 0.3f);
            float masterVol = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);

            ApplyToPlayer(sens, headBobOn, headBobInt, masterVol);
        }

        private void ApplyToPlayer(float sens, bool headBobOn, float headBobInt, float masterVol)
        {
            if (_playerController != null)
            {
                _playerController.SetMouseSensitivity(sens);
                _playerController.SetHeadBobEnabled(headBobOn);
                // Map 0–1 slider to amplitude range
                _playerController.SetHeadBobIntensity(headBobInt * 0.06f, headBobInt * 0.1f);
            }

            AudioListener.volume = masterVol;
        }

        // =====================================================================
        // MAIN MENU / QUIT
        // =====================================================================

        private void ShowMainMenuConfirm()
        {
            _confirmPanel.SetActive(true);
        }

        private void GoToMainMenu()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Try SceneLoader first, fallback to direct load
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadMainMenu();
            }
            else
            {
                SceneManager.LoadScene("MainMenu");
            }
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // =====================================================================
        // LABEL UPDATERS
        // =====================================================================

        private void UpdateSensLabel(float val)
        {
            if (_sensitivityValueText != null)
                _sensitivityValueText.text = val.ToString("F1");
        }

        private void UpdateHeadBobLabel(float val)
        {
            if (_headBobValueText != null)
                _headBobValueText.text = Mathf.RoundToInt(val * 100f) + "%";
        }

        private void UpdateMasterVolLabel(float val)
        {
            if (_masterVolValueText != null)
                _masterVolValueText.text = Mathf.RoundToInt(val * 100f) + "%";
        }

        // =====================================================================
        // UI BUILDER — Creates the entire pause menu canvas at runtime
        // =====================================================================

        private void BuildUI()
        {
            // --- Root canvas ---
            GameObject canvasGo = new GameObject("PauseMenuCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            // ======================== PAUSE PANEL ========================
            _pausePanel = CreatePanel(canvasGo.transform, "PausePanel", new Color(0, 0, 0, 0.85f));

            // Title
            CreateLabel(_pausePanel.transform, "Title", "PAUSED",
                new Vector2(0, 200), 48, FontStyle.Bold, Color.white);

            // Buttons
            CreateButton(_pausePanel.transform, "ResumeBtn", "Resume",
                new Vector2(0, 80), Resume);
            CreateButton(_pausePanel.transform, "SettingsBtn", "Settings",
                new Vector2(0, 10), OpenSettings);
            CreateButton(_pausePanel.transform, "MainMenuBtn", "Main Menu",
                new Vector2(0, -60), ShowMainMenuConfirm);
            CreateButton(_pausePanel.transform, "QuitBtn", "Quit Game",
                new Vector2(0, -130), QuitGame);

            // ======================== SETTINGS PANEL ========================
            _settingsPanel = CreatePanel(canvasGo.transform, "SettingsPanel", new Color(0, 0, 0, 0.9f));

            CreateLabel(_settingsPanel.transform, "SettingsTitle", "SETTINGS",
                new Vector2(0, 280), 42, FontStyle.Bold, Color.white);

            // --- Mouse Sensitivity ---
            CreateLabel(_settingsPanel.transform, "SensLabel", "Mouse Sensitivity",
                new Vector2(-120, 190), 22, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            _sensitivitySlider = CreateSlider(_settingsPanel.transform, "SensSlider",
                new Vector2(0, 160), 0.1f, 10f, 2f);
            _sensitivityValueText = CreateLabel(_settingsPanel.transform, "SensValue", "2.0",
                new Vector2(240, 160), 20, FontStyle.Normal, Color.white);
            _sensitivitySlider.onValueChanged.AddListener(val => {
                UpdateSensLabel(val);
            });

            // --- Head Bob Toggle ---
            CreateLabel(_settingsPanel.transform, "HeadBobLabel", "Head Bobbing",
                new Vector2(-120, 100), 22, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            _headBobToggle = CreateToggle(_settingsPanel.transform, "HeadBobToggle",
                new Vector2(150, 100), true);

            // --- Head Bob Intensity ---
            CreateLabel(_settingsPanel.transform, "HeadBobIntLabel", "Head Bob Intensity",
                new Vector2(-120, 50), 22, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            _headBobSlider = CreateSlider(_settingsPanel.transform, "HeadBobSlider",
                new Vector2(0, 20), 0f, 1f, 0.3f);
            _headBobValueText = CreateLabel(_settingsPanel.transform, "HeadBobValue", "30%",
                new Vector2(240, 20), 20, FontStyle.Normal, Color.white);
            _headBobSlider.onValueChanged.AddListener(val => {
                UpdateHeadBobLabel(val);
            });

            // --- Master Volume ---
            CreateLabel(_settingsPanel.transform, "VolLabel", "Master Volume",
                new Vector2(-120, -50), 22, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            _masterVolSlider = CreateSlider(_settingsPanel.transform, "VolSlider",
                new Vector2(0, -80), 0f, 1f, 1f);
            _masterVolValueText = CreateLabel(_settingsPanel.transform, "VolValue", "100%",
                new Vector2(240, -80), 20, FontStyle.Normal, Color.white);
            _masterVolSlider.onValueChanged.AddListener(val => {
                UpdateMasterVolLabel(val);
            });

            // --- Back button ---
            CreateButton(_settingsPanel.transform, "BackBtn", "Back",
                new Vector2(0, -200), CloseSettings);

            // ======================== CONFIRM DIALOG ========================
            _confirmPanel = CreatePanel(canvasGo.transform, "ConfirmPanel", new Color(0, 0, 0, 0.95f));

            CreateLabel(_confirmPanel.transform, "ConfirmText",
                "Return to Main Menu?\nUnsaved progress will be lost.",
                new Vector2(0, 60), 26, FontStyle.Normal, Color.white);
            CreateButton(_confirmPanel.transform, "ConfirmYes", "Yes, Leave",
                new Vector2(-100, -40), GoToMainMenu);
            CreateButton(_confirmPanel.transform, "ConfirmNo", "Cancel",
                new Vector2(100, -40), () => _confirmPanel.SetActive(false));
        }

        // =====================================================================
        // UI ELEMENT FACTORIES
        // =====================================================================

        private static GameObject CreatePanel(Transform parent, string name, Color bgColor)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = panel.AddComponent<Image>();
            img.color = bgColor;

            return panel;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
            Vector2 anchoredPos, float fontSize, FontStyle style, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(500, 50);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style == FontStyle.Bold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(280, 50);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.15f, 0.15f, 0.2f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.4f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.15f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            // Label
            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRT = labelGo.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 24;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private static Slider CreateSlider(Transform parent, string name,
            Vector2 anchoredPos, float min, float max, float defaultVal)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(350, 30);

            var slider = go.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultVal;
            slider.wholeNumbers = false;

            // Background
            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 0.25f);
            bgRT.anchorMax = new Vector2(1, 0.75f);
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.25f);

            // Fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0, 0.25f);
            fillAreaRT.anchorMax = new Vector2(1, 0.75f);
            fillAreaRT.offsetMin = new Vector2(5, 0);
            fillAreaRT.offsetMax = new Vector2(-5, 0);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRT = fill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.4f, 0.6f, 0.8f);

            slider.fillRect = fillRT;

            // Handle area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRT = handleArea.AddComponent<RectTransform>();
            handleAreaRT.anchorMin = new Vector2(0, 0);
            handleAreaRT.anchorMax = new Vector2(1, 1);
            handleAreaRT.offsetMin = new Vector2(10, 0);
            handleAreaRT.offsetMax = new Vector2(-10, 0);

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleRT = handle.AddComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(20, 0);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;

            slider.handleRect = handleRT;
            slider.targetGraphic = handleImg;

            return slider;
        }

        private static Toggle CreateToggle(Transform parent, string name,
            Vector2 anchoredPos, bool defaultVal)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(40, 40);

            // Background box
            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.25f);

            // Checkmark
            GameObject checkGo = new GameObject("Checkmark");
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkRT = checkGo.AddComponent<RectTransform>();
            checkRT.anchorMin = new Vector2(0.15f, 0.15f);
            checkRT.anchorMax = new Vector2(0.85f, 0.85f);
            checkRT.offsetMin = Vector2.zero;
            checkRT.offsetMax = Vector2.zero;
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = new Color(0.4f, 0.8f, 0.4f);

            var toggle = go.AddComponent<Toggle>();
            toggle.isOn = defaultVal;
            toggle.graphic = checkImg;
            toggle.targetGraphic = bgImg;

            return toggle;
        }
    }
}
