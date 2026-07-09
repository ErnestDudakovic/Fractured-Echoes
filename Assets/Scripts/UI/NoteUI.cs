// ============================================================================
// NoteUI.cs — Self-building full-screen note reading overlay
// Shared by all NoteInteractable objects. Shows title + body text on a
// parchment-style panel. Closes with E / Escape / the Close button.
// Pauses the game and locks player input while reading.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using FracturedEchoes.Player;

namespace FracturedEchoes.UI
{
    /// <summary>
    /// Full-screen note reading view. One instance is shared scene-wide;
    /// use <see cref="Show(string, string)"/> to display a note.
    /// Created automatically by <see cref="NoteUI.EnsureExists"/>.
    /// </summary>
    public class NoteUI : MonoBehaviour
    {
        // =====================================================================
        // STATIC ACCESS
        // =====================================================================

        private static NoteUI _instance;

        /// <summary>Makes sure a NoteUI exists in the scene (creates one if needed).</summary>
        public static NoteUI EnsureExists()
        {
            if (_instance != null) return _instance;

            _instance = FindFirstObjectByType<NoteUI>();
            if (_instance == null)
            {
                var go = new GameObject("NoteUI");
                _instance = go.AddComponent<NoteUI>();
            }

            return _instance;
        }

        /// <summary>Opens the note view with the given title and body text.</summary>
        public static void Show(string title, string body)
        {
            EnsureExists().Open(title, body);
        }

        // =====================================================================
        // RUNTIME UI
        // =====================================================================

        private Canvas _canvas;
        private GameObject _rootPanel;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _bodyText;
        private bool _isOpen;
        private int _openedFrame = -1;
        private FirstPersonController _playerController;
        private InputAction _interactAction;

        /// <summary>True while a note is being displayed.</summary>
        public bool IsOpen => _isOpen;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            _playerController = FindFirstObjectByType<FirstPersonController>();
            _interactAction = InputSystem.actions?.FindAction("Player/Interact");

            BuildUI();
            _rootPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void Update()
        {
            if (!_isOpen) return;

            // Ignore input during the frame the note was opened, otherwise the
            // same E press that opened it would instantly close it again.
            if (Time.frameCount <= _openedFrame) return;

            var kb = Keyboard.current;
            bool closePressed = (_interactAction?.WasPressedThisFrame() ?? false);

            if (kb != null)
            {
                if (kb.eKey.wasPressedThisFrame) closePressed = true;

                if (kb.escapeKey.wasPressedThisFrame)
                {
                    UIFocus.ConsumeEscape();
                    closePressed = true;
                }
            }

            if (closePressed)
            {
                UIFocus.ConsumeInteract();
                Close();
            }
        }

        // =====================================================================
        // OPEN / CLOSE
        // =====================================================================

        private void Open(string title, string body)
        {
            if (_titleText != null) _titleText.text = title;
            if (_bodyText != null) _bodyText.text = body;

            if (_isOpen) return; // content updated, already open

            _isOpen = true;
            _openedFrame = Time.frameCount;
            _rootPanel.SetActive(true);
            UIFocus.RegisterModalOpen();

            // Pause + free cursor + lock player
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_playerController == null)
                _playerController = FindFirstObjectByType<FirstPersonController>();
            if (_playerController != null)
                _playerController.SetInputLocked(true);
        }

        /// <summary>Closes the note view and resumes the game.</summary>
        public void Close()
        {
            if (!_isOpen) return;

            _isOpen = false;
            _rootPanel.SetActive(false);
            UIFocus.RegisterModalClosed();

            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (_playerController != null)
                _playerController.SetInputLocked(false);
        }

        // =====================================================================
        // UI CONSTRUCTION
        // =====================================================================

        private void BuildUI()
        {
            // --- Canvas ---
            var canvasGO = new GameObject("NoteCanvas");
            canvasGO.transform.SetParent(transform, false);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 150;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // --- Dark overlay ---
            _rootPanel = new GameObject("NoteRoot");
            _rootPanel.transform.SetParent(canvasGO.transform, false);
            var rootRT = _rootPanel.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;
            var rootImg = _rootPanel.AddComponent<Image>();
            rootImg.color = new Color(0f, 0f, 0f, 0.85f);

            // --- Paper panel ---
            var paperGO = new GameObject("Paper");
            paperGO.transform.SetParent(_rootPanel.transform, false);
            var paperRT = paperGO.AddComponent<RectTransform>();
            paperRT.anchorMin = new Vector2(0.5f, 0.5f);
            paperRT.anchorMax = new Vector2(0.5f, 0.5f);
            paperRT.sizeDelta = new Vector2(620, 720);
            var paperImg = paperGO.AddComponent<Image>();
            paperImg.color = new Color(0.87f, 0.82f, 0.7f, 0.97f); // aged paper

            // --- Title ---
            _titleText = CreateTMP(paperGO.transform, "Title",
                new Vector2(0, -40), new Vector2(540, 50), 30,
                new Color(0.15f, 0.1f, 0.08f), TextAlignmentOptions.Center);
            _titleText.fontStyle = FontStyles.Bold;

            // --- Divider ---
            var dividerGO = new GameObject("Divider");
            dividerGO.transform.SetParent(paperGO.transform, false);
            var divRT = dividerGO.AddComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0.5f, 1f);
            divRT.anchorMax = new Vector2(0.5f, 1f);
            divRT.anchoredPosition = new Vector2(0, -80);
            divRT.sizeDelta = new Vector2(480, 2);
            var divImg = dividerGO.AddComponent<Image>();
            divImg.color = new Color(0.3f, 0.2f, 0.15f, 0.6f);

            // --- Body text ---
            _bodyText = CreateTMP(paperGO.transform, "Body",
                new Vector2(0, -105), new Vector2(520, 540), 20,
                new Color(0.18f, 0.13f, 0.1f), TextAlignmentOptions.TopLeft);
            _bodyText.fontStyle = FontStyles.Italic;
            _bodyText.lineSpacing = 8f;

            // --- Close hint ---
            var hint = CreateTMP(paperGO.transform, "Hint",
                new Vector2(0, -680), new Vector2(540, 30), 15,
                new Color(0.35f, 0.28f, 0.22f, 0.9f), TextAlignmentOptions.Center);
            hint.text = "[E] or [Esc] — Put the note down";

            // --- Invisible full-screen close button (click anywhere) ---
            var closeBtn = _rootPanel.AddComponent<Button>();
            closeBtn.transition = Selectable.Transition.None;
            closeBtn.onClick.AddListener(Close);
        }

        private static TextMeshProUGUI CreateTMP(Transform parent, string name,
            Vector2 position, Vector2 size, float fontSize, Color color,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }
    }
}
