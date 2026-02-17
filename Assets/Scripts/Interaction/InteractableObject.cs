// ============================================================================
// InteractableObject.cs — Base class for interactable world objects
// Implements IInteractable with common functionality: highlight, audio,
// cooldown, interaction type, and single-use support.
// Extend this for specific interaction types (pickup, inspect, activate).
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
        [Tooltip("Prompt text displayed in the HUD when focused.")]
        [SerializeField] private string _promptText = "Interact";

        [Tooltip("Category — drives UI icon / prompt colour.")]
        [SerializeField] private InteractionType _interactionType = InteractionType.Generic;

        [Tooltip("Whether this object can currently be interacted with.")]
        [SerializeField] private bool _canInteract = true;

        [Tooltip("If true, the object can only be used once.")]
        [SerializeField] private bool _singleUse = false;

        [Tooltip("Seconds before the object can be interacted with again (0 = instant).")]
        [SerializeField, Range(0f, 5f)] private float _cooldown = 0f;

        [Header("Visual Feedback")]
        [Tooltip("Highlight color when the player looks at this object.")]
        [SerializeField] private Color _highlightColor = new Color(1f, 1f, 1f, 0.3f);

        [Tooltip("Enable material tint highlight on focus.")]
        [SerializeField] private bool _enableHighlight = true;

        [Header("Audio")]
        [Tooltip("Sound played on interaction.")]
        [SerializeField] private AudioClip _interactSound;

        [Tooltip("Volume of the interaction sound.")]
        [SerializeField, Range(0f, 1f)] private float _soundVolume = 1f;

        [Header("Events")]
        [Tooltip("Event raised when this specific object is interacted with.")]
        [SerializeField] private GameEvent _onInteracted;

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private Renderer _renderer;
        private bool _hasBeenUsed;
        private AudioSource _audioSource;
        private bool _isFocused;
        private MaterialPropertyBlock _propBlock;
        private static readonly int ColorID = Shader.PropertyToID("_Color");

        // =====================================================================
        // IInteractable IMPLEMENTATION
        // =====================================================================

        public string InteractionPrompt => _promptText;

        public InteractionType Type => _interactionType;

        public bool CanInteract => _canInteract && !(_singleUse && _hasBeenUsed);

        public float InteractionCooldown => _cooldown;

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

            // Raise ScriptableObject event
            _onInteracted?.Raise();
        }

        public virtual void OnFocus()
        {
            if (_isFocused) return;
            _isFocused = true;

            if (_enableHighlight && _renderer != null)
            {
                _propBlock.SetColor(ColorID, _highlightColor);
                _renderer.SetPropertyBlock(_propBlock);
            }
        }

        public virtual void OnLoseFocus()
        {
            if (!_isFocused) return;
            _isFocused = false;

            if (_enableHighlight && _renderer != null)
            {
                _renderer.SetPropertyBlock(null);
            }
        }

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        protected virtual void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();

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
        /// Enables or disables interaction on this object at runtime.
        /// </summary>
        public void SetInteractable(bool canInteract)
        {
            _canInteract = canInteract;
        }

        /// <summary>
        /// Updates the interaction prompt text at runtime.
        /// </summary>
        public void SetPrompt(string prompt)
        {
            _promptText = prompt;
        }

        /// <summary>
        /// Resets single-use state so the object can be used again.
        /// </summary>
        public void ResetUsage()
        {
            _hasBeenUsed = false;
        }
    }
}
