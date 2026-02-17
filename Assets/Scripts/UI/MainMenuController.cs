// ============================================================================
// MainMenuController.cs — Main menu UI controller
// Drives the main menu: New Game, Load Game, Settings, Quit.
// Handles panel visibility, button wiring, and transitions.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FracturedEchoes.UI
{
    /// <summary>
    /// Controls the main menu screen.  Attach to the root Canvas of the
    /// MainMenu scene and wire buttons + panels via the Inspector.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED — PANELS
        // =====================================================================

        [Header("Panels")]
        [Tooltip("Root panel containing the main menu buttons.")]
        [SerializeField] private GameObject _mainPanel;

        [Tooltip("Load Game panel (contains save slot list).")]
        [SerializeField] private GameObject _loadGamePanel;

        [Tooltip("Settings panel.")]
        [SerializeField] private GameObject _settingsPanel;

        [Tooltip("Confirmation dialog (for quit / new game overwrite).")]
        [SerializeField] private GameObject _confirmDialog;

        // =====================================================================
        // SERIALIZED — BUTTONS
        // =====================================================================

        [Header("Main Buttons")]
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _loadGameButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        [Header("Sub-Panel Buttons")]
        [SerializeField] private Button _loadBackButton;
        [SerializeField] private Button _settingsBackButton;

        [Header("Confirm Dialog")]
        [SerializeField] private TextMeshProUGUI _confirmText;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;

        // =====================================================================
        // SERIALIZED — GAME SETTINGS
        // =====================================================================

        [Header("Game")]
        [Tooltip("Scene name to load when starting a new game.")]
        [SerializeField] private string _firstGameScene = "OutdoorsScene";

        [Header("Title")]
        [Tooltip("Optional title text (for animation / glow effects later).")]
        [SerializeField] private TextMeshProUGUI _titleText;

        [Header("Version")]
        [Tooltip("Optional build version label.")]
        [SerializeField] private TextMeshProUGUI _versionText;

        // =====================================================================
        // RUNTIME
        // =====================================================================

        private System.Action _pendingConfirmAction;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Start()
        {
            // Ensure time is running (in case we came from a paused game)
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Wire buttons
            _newGameButton?.onClick.AddListener(OnNewGame);
            _loadGameButton?.onClick.AddListener(OnLoadGame);
            _settingsButton?.onClick.AddListener(OnSettings);
            _quitButton?.onClick.AddListener(OnQuit);

            _loadBackButton?.onClick.AddListener(ShowMainPanel);
            _settingsBackButton?.onClick.AddListener(ShowMainPanel);

            _confirmYesButton?.onClick.AddListener(OnConfirmYes);
            _confirmNoButton?.onClick.AddListener(OnConfirmNo);

            // Set version text
            if (_versionText != null)
            {
                _versionText.text = $"v{Application.version}";
            }

            // Show main panel
            ShowMainPanel();
        }

        private void OnDestroy()
        {
            // Unsubscribe
            _newGameButton?.onClick.RemoveListener(OnNewGame);
            _loadGameButton?.onClick.RemoveListener(OnLoadGame);
            _settingsButton?.onClick.RemoveListener(OnSettings);
            _quitButton?.onClick.RemoveListener(OnQuit);

            _loadBackButton?.onClick.RemoveListener(ShowMainPanel);
            _settingsBackButton?.onClick.RemoveListener(ShowMainPanel);

            _confirmYesButton?.onClick.RemoveListener(OnConfirmYes);
            _confirmNoButton?.onClick.RemoveListener(OnConfirmNo);
        }

        // =====================================================================
        // PANEL MANAGEMENT
        // =====================================================================

        /// <summary>Show only the main menu panel.</summary>
        public void ShowMainPanel()
        {
            SetActivePanel(_mainPanel);
        }

        private void SetActivePanel(GameObject panel)
        {
            if (_mainPanel != null) _mainPanel.SetActive(panel == _mainPanel);
            if (_loadGamePanel != null) _loadGamePanel.SetActive(panel == _loadGamePanel);
            if (_settingsPanel != null) _settingsPanel.SetActive(panel == _settingsPanel);
            if (_confirmDialog != null) _confirmDialog.SetActive(false);
        }

        // =====================================================================
        // BUTTON HANDLERS
        // =====================================================================

        private void OnNewGame()
        {
            // Check if any save exists — if so, warn
            // For now, go straight to loading
            ShowConfirmDialog("Start a new game?", () =>
            {
                if (SceneLoader.Instance != null)
                {
                    SceneLoader.Instance.LoadScene(_firstGameScene);
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(_firstGameScene);
                }
            });
        }

        private void OnLoadGame()
        {
            SetActivePanel(_loadGamePanel);
        }

        private void OnSettings()
        {
            SetActivePanel(_settingsPanel);
        }

        private void OnQuit()
        {
            ShowConfirmDialog("Quit the game?", () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }

        // =====================================================================
        // CONFIRM DIALOG
        // =====================================================================

        private void ShowConfirmDialog(string message, System.Action onConfirm)
        {
            _pendingConfirmAction = onConfirm;

            if (_confirmText != null)
            {
                _confirmText.text = message;
            }

            if (_confirmDialog != null)
            {
                _confirmDialog.SetActive(true);
            }
        }

        private void OnConfirmYes()
        {
            if (_confirmDialog != null)
            {
                _confirmDialog.SetActive(false);
            }

            _pendingConfirmAction?.Invoke();
            _pendingConfirmAction = null;
        }

        private void OnConfirmNo()
        {
            if (_confirmDialog != null)
            {
                _confirmDialog.SetActive(false);
            }

            _pendingConfirmAction = null;
        }
    }
}
