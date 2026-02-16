// ============================================================================
// InteractionUI.cs — Simple HUD for interaction prompts
// Displays context-sensitive prompts when the player looks at interactables.
// Listens to InteractionSystem events to update UI text.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;

namespace FracturedEchoes.UI
{
    /// <summary>
    /// Displays interaction prompts and crosshair.
    /// Connect to InteractionSystem via GameEvent listeners or direct reference.
    /// </summary>
    public class InteractionUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text element displaying the interaction prompt.")]
        [SerializeField] private Text _promptText;

        [Tooltip("Crosshair image (changes color when targeting interactable).")]
        [SerializeField] private Image _crosshair;

        [Header("Colors")]
        [SerializeField] private Color _defaultCrosshairColor = Color.white;
        [SerializeField] private Color _interactableCrosshairColor = Color.green;

        [Header("Animation")]
        [SerializeField] private float _fadeSpeed = 8f;

        private CanvasGroup _promptCanvasGroup;
        private bool _showPrompt;
        private string _currentPrompt;

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
        }

        private void Update()
        {
            // Smooth fade transitions
            if (_promptCanvasGroup != null)
            {
                float targetAlpha = _showPrompt ? 1f : 0f;
                _promptCanvasGroup.alpha = Mathf.Lerp(_promptCanvasGroup.alpha, targetAlpha, Time.deltaTime * _fadeSpeed);
            }
        }

        /// <summary>
        /// Shows the interaction prompt. Called by GameEventListener.
        /// </summary>
        public void ShowPrompt(string prompt)
        {
            _currentPrompt = prompt;
            _showPrompt = true;

            if (_promptText != null)
            {
                _promptText.text = $"[E] {prompt}";
            }

            if (_crosshair != null)
            {
                _crosshair.color = _interactableCrosshairColor;
            }
        }

        /// <summary>
        /// Hides the interaction prompt. Called by GameEventListener.
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
