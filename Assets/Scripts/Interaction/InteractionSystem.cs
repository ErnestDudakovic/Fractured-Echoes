// ============================================================================
// InteractionSystem.cs — Raycast-based interaction manager
// Casts a ray from the camera center each frame, detects IInteractable objects,
// manages focus/unfocus callbacks, interaction triggering, cooldowns,
// and system-level locking. Fully event-driven via ScriptableObject channels.
// ============================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.Core.Events;

namespace FracturedEchoes.Interaction
{
    /// <summary>
    /// Raycast-based interaction system. Attach to the Player.
    /// Detects IInteractable objects within range and manages interaction flow.
    /// Uses a center-screen ray with configurable distance and layer mask.
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

        [Header("Spherecast Fallback")]
        [Tooltip("Enable a wider spherecast when the thin ray misses, for forgiving detection.")]
        [SerializeField] private bool _useSphereCastFallback = true;

        [Tooltip("Radius of the fallback sphere cast.")]
        [SerializeField, Range(0.05f, 0.3f)] private float _sphereCastRadius = 0.1f;

        [Header("Input")]
        [Tooltip("Interaction action is read from project-wide Input System actions (Player/Interact).")]
        #pragma warning disable CS0414
        [SerializeField] private bool _showInputDebug = false;
        #pragma warning restore CS0414

        [Header("Events")]
        [Tooltip("Raised when a new object is focused (carries prompt text).")]
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
        private Collider _lastResolvedCollider;
        private bool _isLocked;
        private float _cooldownTimer;
        private InputAction _interactAction;

        // =====================================================================
        // PUBLIC PROPERTIES
        // =====================================================================

        /// <summary>Currently focused interactable (null if none).</summary>
        public IInteractable CurrentTarget => _currentTarget;

        /// <summary>GameObject of the current target (null if none).</summary>
        public GameObject CurrentTargetObject => _currentTargetObject;

        /// <summary>Current prompt text to show in UI.</summary>
        public string CurrentPrompt => _currentTarget?.InteractionPrompt ?? string.Empty;

        /// <summary>Whether the system is locked (cutscenes, events).</summary>
        public bool IsLocked => _isLocked;

        /// <summary>Configured interaction range.</summary>
        public float InteractionRange => _interactionRange;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            _interactAction = InputSystem.actions?.FindAction("Player/Interact");
        }

        private void Update()
        {
            // Tick cooldown regardless of lock state
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }

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

            // Primary thin raycast — precise
            if (Physics.Raycast(ray, out RaycastHit hit, _interactionRange, _interactableLayer))
            {
                if (TrySetTarget(hit.collider)) return;
            }

            // Fallback spherecast — forgiving for small objects
            if (_useSphereCastFallback)
            {
                if (Physics.SphereCast(ray, _sphereCastRadius, out hit, _interactionRange, _interactableLayer))
                {
                    if (TrySetTarget(hit.collider)) return;
                }
            }

            // Nothing found
            ClearFocus();
        }

        /// <summary>
        /// Attempts to resolve an IInteractable from the hit collider
        /// and sets or maintains focus. Returns true if a target was found.
        /// </summary>
        private bool TrySetTarget(Collider col)
        {
            // Skip re-resolve if same collider as last frame
            if (col == _lastResolvedCollider && _currentTarget != null)
            {
                _currentTarget.OnFocus();
                return true;
            }

            IInteractable interactable = col.GetComponent<IInteractable>()
                                      ?? col.GetComponentInParent<IInteractable>();

            if (interactable == null) return false;

            _lastResolvedCollider = col;

            if (interactable != _currentTarget)
            {
                ClearFocus();
                SetFocus(interactable, col.gameObject);
            }
            else
            {
                _currentTarget.OnFocus();
            }

            return true;
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
                _lastResolvedCollider = null;
                _onFocusLost?.Raise();
            }
        }

        // =====================================================================
        // INPUT HANDLING
        // =====================================================================

        private void HandleInput()
        {
            if (!(_interactAction?.WasPressedThisFrame() ?? false)) return;
            if (_currentTarget == null) return;
            if (!_currentTarget.CanInteract) return;
            if (_cooldownTimer > 0f) return;

            // Execute interaction
            _currentTarget.OnInteract();
            _onInteraction?.Raise();

            // Start cooldown based on the object's setting
            _cooldownTimer = _currentTarget.InteractionCooldown;
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
        /// Bypasses cooldown and lock checks.
        /// </summary>
        public void ForceInteract(IInteractable target)
        {
            if (target != null && target.CanInteract)
            {
                target.OnInteract();
                _onInteraction?.Raise();
            }
        }

        /// <summary>
        /// Sets the interaction range at runtime (e.g. items that glow from far away).
        /// </summary>
        public void SetInteractionRange(float range)
        {
            _interactionRange = Mathf.Max(0.5f, range);
        }
    }
}
