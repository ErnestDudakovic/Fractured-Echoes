// ============================================================================
// InteractableObject.cs — Base class for interactable world objects
// Implements IInteractable with common functionality. Extend this for
// specific interaction types (pickup, inspect, activate, etc.).
// ============================================================================

using UnityEngine;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.Core.Events;

namespace FracturedEchoes.Interaction
{
    /// <summary>
    /// Base class for all interactable objects in the game world.
    /// Provides visual feedback (outline/highlight) and common interaction logic.
    /// Extend this class for specific behaviors: pickup, inspect, activate, etc.
    /// </summary>
    public class InteractableObject : MonoBehaviour, IInteractable
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Interaction Settings")]
        [SerializeField] private string _promptText = "Interact";
        [SerializeField] private bool _canInteract = true;
        [SerializeField] private bool _singleUse = false;

        [Header("Visual Feedback")]
        [Tooltip("Highlight color when the player looks at this object.")]
        [SerializeField] private Color _highlightColor = new Color(1f, 1f, 1f, 0.3f);
        [SerializeField] private bool _enableHighlight = true;

        [Header("Audio")]
        [SerializeField] private AudioClip _interactSound;
        [SerializeField] private float _soundVolume = 1f;

        [Header("Events")]
        [Tooltip("Event raised when this specific object is interacted with.")]
        [SerializeField] private GameEvent _onInteracted;

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private Renderer _renderer;
        private Color _originalColor;
        private bool _hasBeenUsed;
        private AudioSource _audioSource;

        // =====================================================================
        // IInteractable IMPLEMENTATION
        // =====================================================================

        public string InteractionPrompt => _promptText;

        public bool CanInteract => _canInteract && !(_singleUse && _hasBeenUsed);

        public virtual void OnInteract()
        {
            if (!CanInteract) return;

            // Play interaction sound
            if (_interactSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_interactSound, _soundVolume);
            }

            // Mark as used for single-use objects
            if (_singleUse)
            {
                _hasBeenUsed = true;
            }

            // Raise event
            _onInteracted?.Raise();
        }

        public virtual void OnFocus()
        {
            if (_enableHighlight && _renderer != null)
            {
                // Simple color tint highlight
                _renderer.material.color = _highlightColor;
            }
        }

        public virtual void OnLoseFocus()
        {
            if (_enableHighlight && _renderer != null)
            {
                _renderer.material.color = _originalColor;
            }
        }

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        protected virtual void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _originalColor = _renderer.material.color;
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null && _interactSound != null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f; // 3D sound
            }
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Enables or disables interaction on this object.
        /// </summary>
        public void SetInteractable(bool canInteract)
        {
            _canInteract = canInteract;
        }

        /// <summary>
        /// Updates the interaction prompt text.
        /// </summary>
        public void SetPrompt(string prompt)
        {
            _promptText = prompt;
        }
    }
}
