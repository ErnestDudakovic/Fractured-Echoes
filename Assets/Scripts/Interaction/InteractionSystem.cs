// ============================================================================
// InteractionSystem.cs — Raycast-based interaction manager
// Casts a ray from the camera center each frame, detects IInteractable objects,
// manages focus/unfocus callbacks and interaction triggering.
// Supports interaction locking during scripted events.
// ============================================================================

using UnityEngine;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.Core.Events;

namespace FracturedEchoes.Interaction
{
    /// <summary>
    /// Raycast-based interaction system. Attach to the Player.
    /// Detects IInteractable objects within range and manages interaction flow.
    /// </summary>
    public class InteractionSystem : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Raycast Settings")]
        [Tooltip("Maximum distance for interaction raycast.")]
        [SerializeField] private float _interactionRange = 3f;

        [Tooltip("Layer mask for interactable objects.")]
        [SerializeField] private LayerMask _interactableLayer;

        [Tooltip("Reference to the player camera transform (raycast origin).")]
        [SerializeField] private Transform _cameraTransform;

        [Header("Input")]
        [Tooltip("Key to trigger interaction.")]
        [SerializeField] private KeyCode _interactKey = KeyCode.E;

        [Header("Events")]
        [Tooltip("Raised when a new object is focused.")]
        [SerializeField] private GameEventString _onFocusChanged;

        [Tooltip("Raised when focus is lost (no interactable in view).")]
        [SerializeField] private GameEvent _onFocusLost;

        [Tooltip("Raised when an interaction occurs.")]
        [SerializeField] private GameEvent _onInteraction;

        [Header("Debug")]
        [SerializeField] private bool _showDebugRay = true;

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private IInteractable _currentTarget;
        private GameObject _currentTargetObject;
        private bool _isLocked;

        // =====================================================================
        // PROPERTIES
        // =====================================================================

        /// <summary>
        /// The currently focused interactable object (null if none).
        /// </summary>
        public IInteractable CurrentTarget => _currentTarget;

        /// <summary>
        /// The current interaction prompt to display in the UI.
        /// </summary>
        public string CurrentPrompt => _currentTarget?.InteractionPrompt ?? string.Empty;

        /// <summary>
        /// Whether the interaction system is currently locked.
        /// </summary>
        public bool IsLocked => _isLocked;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Update()
        {
            if (_isLocked)
            {
                ClearFocus();
                return;
            }

            PerformRaycast();
            HandleInput();
        }

        // =====================================================================
        // RAYCAST LOGIC
        // =====================================================================

        private void PerformRaycast()
        {
            Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);

            if (_showDebugRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * _interactionRange, Color.green);
            }

            if (Physics.Raycast(ray, out RaycastHit hit, _interactionRange, _interactableLayer))
            {
                // Check if the hit object implements IInteractable
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    // New target detected
                    if (interactable != _currentTarget)
                    {
                        ClearFocus();
                        SetFocus(interactable, hit.collider.gameObject);
                    }
                    else
                    {
                        // Same target — keep calling OnFocus
                        _currentTarget.OnFocus();
                    }
                    return;
                }
            }

            // Nothing hit or no IInteractable found
            ClearFocus();
        }

        private void SetFocus(IInteractable interactable, GameObject targetObject)
        {
            _currentTarget = interactable;
            _currentTargetObject = targetObject;
            _currentTarget.OnFocus();

            // Notify UI of new focus
            _onFocusChanged?.Raise(interactable.InteractionPrompt);
        }

        private void ClearFocus()
        {
            if (_currentTarget != null)
            {
                _currentTarget.OnLoseFocus();
                _currentTarget = null;
                _currentTargetObject = null;
                _onFocusLost?.Raise();
            }
        }

        // =====================================================================
        // INPUT HANDLING
        // =====================================================================

        private void HandleInput()
        {
            if (Input.GetKeyDown(_interactKey) && _currentTarget != null && _currentTarget.CanInteract)
            {
                _currentTarget.OnInteract();
                _onInteraction?.Raise();
            }
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Locks the interaction system (during cutscenes, events, etc.).
        /// </summary>
        public void Lock()
        {
            _isLocked = true;
            ClearFocus();
        }

        /// <summary>
        /// Unlocks the interaction system.
        /// </summary>
        public void Unlock()
        {
            _isLocked = false;
        }

        /// <summary>
        /// Forces interaction with a specific object (for scripted events).
        /// </summary>
        public void ForceInteract(IInteractable target)
        {
            if (target != null && target.CanInteract)
            {
                target.OnInteract();
                _onInteraction?.Raise();
            }
        }
    }
}
