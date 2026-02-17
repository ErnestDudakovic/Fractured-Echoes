// ============================================================================
// FirstPersonMotor.cs — Handles all player movement and physics
// Separated from camera logic for clean modularity.
// Uses CharacterController for smooth, physics-independent movement.
// ============================================================================

using UnityEngine;
using UnityEngine.InputSystem;

namespace FracturedEchoes.Player
{
    /// <summary>
    /// Handles ground movement, sprinting, crouching speed, gravity,
    /// and smooth velocity interpolation. Requires a CharacterController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonMotor : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Speed Settings")]
        [Tooltip("Normal walking speed.")]
        [SerializeField] private float _walkSpeed = 3.5f;

        [Tooltip("Speed while holding sprint key.")]
        [SerializeField] private float _runSpeed = 5.5f;

        [Tooltip("Speed while crouching.")]
        [SerializeField] private float _crouchSpeed = 1.5f;

        [Header("Crouch")]
        [Tooltip("CharacterController height while crouching.")]
        [SerializeField] private float _crouchHeight = 1.0f;

        [Tooltip("How fast the crouch transition happens.")]
        [SerializeField] private float _crouchTransitionSpeed = 8f;

        [Header("Physics")]
        [Tooltip("Gravity acceleration applied each frame.")]
        [SerializeField] private float _gravity = -9.81f;

        [Tooltip("Time (seconds) for velocity to reach target. Lower = snappier.")]
        [SerializeField, Range(0.01f, 0.3f)] private float _movementSmoothing = 0.1f;

        [Header("Micro-Delay (Psychological Enhancement)")]
        [Tooltip("Adds a subtle sluggishness to movement, controlled by stress.")]
        [SerializeField] private bool _enableMicroDelay = false;

        [Tooltip("How much input is dampened (0 = none, 1 = full delay).")]
        [SerializeField, Range(0f, 0.15f)] private float _microDelayAmount = 0.03f;

        [Header("Ceiling Check")]
        [Tooltip("Layer mask for ceiling detection when standing up from crouch.")]
        [SerializeField] private LayerMask _ceilingCheckMask = ~0;

        // =====================================================================
        // RUNTIME STATE
        // =====================================================================

        private CharacterController _controller;

        private InputAction _moveAction;
        private InputAction _sprintAction;
        private InputAction _crouchAction;

        private float _standingHeight;
        private Vector3 _standingCenter;
        private bool _isCrouching;
        private float _currentHeight;

        private Vector3 _verticalVelocity;
        private Vector3 _currentMoveVelocity;
        private Vector3 _moveSmoothVelocity;

        // =====================================================================
        // PUBLIC PROPERTIES — Read by controller and camera
        // =====================================================================

        /// <summary>True when the CharacterController is touching the ground.</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>True when the player is holding sprint and moving forward.</summary>
        public bool IsSprinting { get; private set; }

        /// <summary>True when the player is crouching.</summary>
        public bool IsCrouching => _isCrouching;

        /// <summary>Current normalized crouch amount (0 = standing, 1 = fully crouched).</summary>
        public float CrouchAmount => 1f - Mathf.InverseLerp(_crouchHeight, _standingHeight, _currentHeight);

        /// <summary>Current horizontal move velocity (world space).</summary>
        public Vector3 CurrentVelocity => _currentMoveVelocity;

        /// <summary>Squared magnitude of horizontal velocity — avoids sqrt.</summary>
        public float SpeedSqr => _currentMoveVelocity.sqrMagnitude;

        /// <summary>True if the player has meaningful horizontal velocity.</summary>
        public bool IsMoving => SpeedSqr > 0.1f;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            // Store default height for crouch transitions
            _standingHeight = _controller.height;
            _standingCenter = _controller.center;
            _currentHeight = _standingHeight;

            // Cache input actions from project-wide Input System
            _moveAction   = InputSystem.actions?.FindAction("Player/Move");
            _sprintAction = InputSystem.actions?.FindAction("Player/Sprint");
            _crouchAction = InputSystem.actions?.FindAction("Player/Crouch");
        }

        // =====================================================================
        // CORE TICK — Called by FirstPersonController.Update()
        // =====================================================================

        /// <summary>
        /// Processes one frame of movement. Call from the controller's Update().
        /// </summary>
        public void Tick()
        {
            UpdateGroundedState();
            ReadInput(out float horizontal, out float vertical);
            UpdateCrouch();
            ApplyHorizontalMovement(horizontal, vertical);
            ApplyGravity();
        }

        // =====================================================================
        // INTERNAL LOGIC
        // =====================================================================

        private void UpdateGroundedState()
        {
            IsGrounded = _controller.isGrounded;

            // Small downward nudge keeps the controller snapped to the ground
            if (IsGrounded && _verticalVelocity.y < 0f)
            {
                _verticalVelocity.y = -2f;
            }
        }

        private void ReadInput(out float horizontal, out float vertical)
        {
            Vector2 move = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            horizontal = move.x;
            vertical   = move.y;

            // Can't sprint while crouching
            IsSprinting = !_isCrouching && (_sprintAction?.IsPressed() ?? false) && vertical > 0f;

            // Toggle crouch
            bool wantsCrouch = _crouchAction?.IsPressed() ?? false;
            _isCrouching = wantsCrouch;
        }

        private void UpdateCrouch()
        {
            float targetHeight = _isCrouching ? _crouchHeight : _standingHeight;
            _currentHeight = Mathf.Lerp(_currentHeight, targetHeight, Time.deltaTime * _crouchTransitionSpeed);

            // If standing up, check for headroom
            if (!_isCrouching && _currentHeight < _standingHeight - 0.05f)
            {
                float checkDist = _standingHeight - _currentHeight;
                if (Physics.SphereCast(transform.position + Vector3.up * _currentHeight, _controller.radius * 0.8f,
                    Vector3.up, out _, checkDist, _ceilingCheckMask, QueryTriggerInteraction.Ignore))
                {
                    _isCrouching = true;
                    _currentHeight = Mathf.Lerp(_currentHeight, _crouchHeight, Time.deltaTime * _crouchTransitionSpeed);
                }
            }

            _controller.height = _currentHeight;

            // Only recalculate center when height is actually changing
            float newCenterY = _currentHeight / 2f;
            if (!Mathf.Approximately(_controller.center.y, newCenterY))
            {
                _controller.center = new Vector3(_standingCenter.x, newCenterY, _standingCenter.z);
            }
        }

        private void ApplyHorizontalMovement(float horizontal, float vertical)
        {
            // Build desired direction in world space relative to the player body
            Vector3 moveDir = (transform.right * horizontal + transform.forward * vertical).normalized;

            float targetSpeed = _isCrouching ? _crouchSpeed : (IsSprinting ? _runSpeed : _walkSpeed);
            Vector3 targetVelocity = moveDir * targetSpeed;

            // Smooth acceleration / deceleration for atmospheric, weighty feel
            _currentMoveVelocity = Vector3.SmoothDamp(
                _currentMoveVelocity,
                targetVelocity,
                ref _moveSmoothVelocity,
                _movementSmoothing
            );

            // Optional micro-delay for psychological tension
            Vector3 finalMove = _enableMicroDelay
                ? Vector3.Lerp(_currentMoveVelocity, targetVelocity, 1f - _microDelayAmount)
                : _currentMoveVelocity;

            _controller.Move(finalMove * Time.deltaTime);
        }

        private void ApplyGravity()
        {
            _verticalVelocity.y += _gravity * Time.deltaTime;
            _controller.Move(_verticalVelocity * Time.deltaTime);
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Immediately kills all velocity (used when locking input).
        /// </summary>
        public void StopImmediately()
        {
            _currentMoveVelocity = Vector3.zero;
            _moveSmoothVelocity  = Vector3.zero;
        }

        /// <summary>
        /// Enable or disable the micro-delay effect at runtime.
        /// </summary>
        public void SetMicroDelay(bool enabled, float amount = 0.03f)
        {
            _enableMicroDelay = enabled;
            _microDelayAmount = Mathf.Clamp(amount, 0f, 0.15f);
        }
    }
}
