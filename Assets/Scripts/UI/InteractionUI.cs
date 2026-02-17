// ============================================================================
// InteractionUI.cs — HUD for interaction prompts and crosshair
// Displays context-sensitive prompts when the player looks at interactables.
// Listens to InteractionSystem events to update UI text and crosshair.
// Supports smooth fade, crosshair scale pulse, and interaction-type colouring.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using FracturedEchoes.Core.Interfaces;

namespace FracturedEchoes.UI
{
    /// <summary>
    /// Displays interaction prompts and crosshair feedback.
    /// Connect to InteractionSystem via GameEvent listeners or direct reference.
    /// </summary>
    public class InteractionUI : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("UI References")]
        [Tooltip("Text element displaying the interaction prompt.")]
        [SerializeField] private Text _promptText;

        [Tooltip("Crosshair image (changes colour / scale when targeting).")]
        [SerializeField] private Image _crosshair;

        [Header("Crosshair Colours")]
        [SerializeField] private Color _defaultCrosshairColor = Color.white;
        [SerializeField] private Color _interactableCrosshairColor = Color.green;
        [SerializeField] private Color _lockedCrosshairColor = new Color(1f, 0.3f, 0.3f, 0.6f);

        [Header("Crosshair Scale")]
        [Tooltip("Scale multiplier when hovering an interactable.")]
        [SerializeField] private float _focusScaleMultiplier = 1.3f;

        [Tooltip("Speed of crosshair scale interpolation.")]
        [SerializeField] private float _scaleSpeed = 10f;

        [Header("Animation")]
        [Tooltip("Speed of prompt fade in / fade out.")]
        [SerializeField] private float _fadeSpeed = 8f;

        [Header("Key Label")]
        [Tooltip("Key name shown in the prompt (e.g. 'E').")]
        [SerializeField] private string _interactKeyLabel = "E";

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private CanvasGroup _promptCanvasGroup;
        private bool _showPrompt;
        private bool _targetCanInteract = true;
        private Vector3 _crosshairBaseScale;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            // Add CanvasGroup for fading if not present
            if (_promptText != null)
            {
                _promptCanvasGroup = _promptText.GetComponentInParent<CanvasGroup>();
                if (_promptCanvasGroup == null)
                {
                    _promptCanvasGroup = _promptText.gameObject.AddComponent<CanvasGroup>();
                }
                _promptCanvasGroup.alpha = 0f;
            }

            if (_crosshair != null)
            {
                _crosshairBaseScale = _crosshair.rectTransform.localScale;
            }
        }

        private void Update()
        {
            // Smooth prompt fade
            if (_promptCanvasGroup != null)
            {
                float targetAlpha = _showPrompt ? 1f : 0f;
                _promptCanvasGroup.alpha = Mathf.MoveTowards(
                    _promptCanvasGroup.alpha,
                    targetAlpha,
                    Time.deltaTime * _fadeSpeed
                );
            }

            // Smooth crosshair scale
            if (_crosshair != null)
            {
                Vector3 targetScale = _showPrompt
                    ? _crosshairBaseScale * _focusScaleMultiplier
                    : _crosshairBaseScale;

                _crosshair.rectTransform.localScale = Vector3.MoveTowards(
                    _crosshair.rectTransform.localScale,
                    targetScale,
                    Time.deltaTime * _scaleSpeed
                );
            }
        }

        // =====================================================================
        // PUBLIC API — Called via GameEventListeners or direct reference
        // =====================================================================

        /// <summary>
        /// Shows the interaction prompt with key label prefix.
        /// Called when the InteractionSystem focuses an object.
        /// </summary>
        public void ShowPrompt(string prompt)
        {
            _showPrompt = true;
            _targetCanInteract = true;

            if (_promptText != null)
            {
                _promptText.text = $"[{_interactKeyLabel}] {prompt}";
            }

            if (_crosshair != null)
            {
                _crosshair.color = _interactableCrosshairColor;
            }
        }

        /// <summary>
        /// Overload that also considers whether the target can be interacted with.
        /// Shows the prompt but uses a locked colour if CanInteract is false.
        /// </summary>
        public void ShowPrompt(string prompt, bool canInteract)
        {
            _showPrompt = true;
            _targetCanInteract = canInteract;

            if (_promptText != null)
            {
                _promptText.text = canInteract
                    ? $"[{_interactKeyLabel}] {prompt}"
                    : prompt; // No key hint when locked
            }

            if (_crosshair != null)
            {
                _crosshair.color = canInteract
                    ? _interactableCrosshairColor
                    : _lockedCrosshairColor;
            }
        }

        /// <summary>
        /// Hides the interaction prompt. Called when focus is lost.
        /// </summary>
        public void HidePrompt()
        {
            _showPrompt = false;

            if (_crosshair != null)
            {
                _crosshair.color = _defaultCrosshairColor;
            }
        }
    }
}
