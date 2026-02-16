// ============================================================================
// FirstPersonController.cs — Player movement and camera system
// Uses CharacterController for smooth, physics-independent movement.
// Includes: head bob, camera sway, breathing effect, dynamic FOV,
// psychological enhancements (drift, micro delay, aim instability).
// ============================================================================

using UnityEngine;
using FracturedEchoes.Core.Events;

namespace FracturedEchoes.Player
{
    /// <summary>
    /// First-person controller with atmospheric enhancements for psychological horror.
    /// Attach to the Player GameObject with a CharacterController component.
    /// Camera should be a child object.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 3.5f;
        [SerializeField] private float _runSpeed = 5.5f;
        [SerializeField] private float _crouchSpeed = 1.5f;
        [SerializeField] private float _gravity = -9.81f;
        [SerializeField] private float _movementSmoothing = 0.1f;

        [Header("Camera")]
        [SerializeField] private Transform _cameraHolder;
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _maxLookAngle = 80f;
        [SerializeField] private float _cameraSmoothing = 0.05f;

        [Header("Head Bob")]
        [SerializeField] private bool _enableHeadBob = true;
        [SerializeField] private float _bobFrequency = 1.8f;
        [SerializeField] private float _bobAmplitudeX = 0.05f;
        [SerializeField] private float _bobAmplitudeY = 0.08f;

        [Header("Camera Sway")]
        [SerializeField] private bool _enableCameraSway = true;
        [SerializeField] private float _swayAmount = 0.02f;
        [SerializeField] private float _swaySmoothing = 4f;

        [Header("Breathing Effect")]
        [SerializeField] private bool _enableBreathing = true;
        [SerializeField] private float _breathFrequency = 0.3f;
        [SerializeField] private float _breathAmplitude = 0.003f;

        [Header("Dynamic FOV")]
        [SerializeField] private float _baseFOV = 60f;
        [SerializeField] private float _sprintFOVIncrease = 8f;
        [SerializeField] private float _stressFOVDecrease = 5f;
        [SerializeField] private float _fovTransitionSpeed = 4f;

        [Header("Psychological Enhancements")]
        [SerializeField] private bool _enableDrift = false;
        [SerializeField] private float _driftIntensity = 0.1f;
        [SerializeField] private bool _enableMicroDelay = false;
        [SerializeField] private float _microDelayAmount = 0.03f;
        [SerializeField] private bool _enableAimInstability = false;
        [SerializeField] private float _aimInstabilityAmount = 0.2f;

        [Header("Footsteps")]
        [SerializeField] private AudioClip[] _footstepClips;
        [SerializeField] private float _footstepInterval = 0.5f;
        [SerializeField] private float _footstepVolume = 0.4f;

        [Header("Events")]
        [SerializeField] private GameEvent _onPlayerMoved;
        [SerializeField] private GameEvent _onPlayerStopped;

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private CharacterController _controller;
        private Camera _camera;
        private AudioSource _footstepAudioSource;

        // Movement
        private Vector3 _velocity;
        private Vector3 _currentMoveVelocity;
        private Vector3 _moveSmoothVelocity;
        private bool _isGrounded;
        private bool _isSprinting;
        private bool _wasMoving;

        // Camera
        private float _cameraPitch;
        private float _currentCameraPitch;
        private float _pitchSmoothVelocity;
        private Vector3 _defaultCameraPosition;

        // Head bob
        private float _bobTimer;

        // Breathing
        private float _breathTimer;

        // Sway
        private Vector3 _swayOffset;

        // Drift
        private float _driftAngle;

        // FOV
        private float _targetFOV;
        private float _stressLevel; // 0–1, controlled externally

        // Footsteps
        private float _footstepTimer;

        // Input lock
        private bool _inputLocked;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _camera = _cameraHolder.GetComponentInChildren<Camera>();

            // Create audio source for footsteps
            _footstepAudioSource = gameObject.AddComponent<AudioSource>();
            _footstepAudioSource.playOnAwake = false;
            _footstepAudioSource.spatialBlend = 0f;

            _defaultCameraPosition = _cameraHolder.localPosition;
            _targetFOV = _baseFOV;

            // Lock cursor for first-person control
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (_inputLocked) return;

            HandleMovement();
            HandleCamera();
            HandleHeadBob();
            HandleBreathing();
            HandleCameraSway();
            HandleDrift();
            HandleFOV();
            HandleFootsteps();
        }

        // =====================================================================
        // MOVEMENT
        // =====================================================================

        private void HandleMovement()
        {
            _isGrounded = _controller.isGrounded;

            if (_isGrounded && _velocity.y < 0f)
            {
                _velocity.y = -2f; // Small downward force to stay grounded
            }

            // Read input
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            _isSprinting = Input.GetKey(KeyCode.LeftShift) && vertical > 0;

            // Calculate desired direction
            Vector3 moveDirection = (transform.right * horizontal + transform.forward * vertical).normalized;

            // Select speed
            float targetSpeed = _isSprinting ? _runSpeed : _walkSpeed;

            // Smooth movement for atmospheric feel
            Vector3 targetVelocity = moveDirection * targetSpeed;
            _currentMoveVelocity = Vector3.SmoothDamp(
                _currentMoveVelocity,
                targetVelocity,
                ref _moveSmoothVelocity,
                _movementSmoothing
            );

            // Apply micro-delay if enabled (psychological enhancement)
            Vector3 finalMove = _enableMicroDelay
                ? Vector3.Lerp(_currentMoveVelocity, targetVelocity, 1f - _microDelayAmount)
                : _currentMoveVelocity;

            _controller.Move(finalMove * Time.deltaTime);

            // Gravity
            _velocity.y += _gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);

            // Track movement state for events
            bool isMoving = moveDirection.sqrMagnitude > 0.01f;
            if (isMoving && !_wasMoving) _onPlayerMoved?.Raise();
            if (!isMoving && _wasMoving) _onPlayerStopped?.Raise();
            _wasMoving = isMoving;
        }

        // =====================================================================
        // CAMERA
        // =====================================================================

        private void HandleCamera()
        {
            float mouseX = Input.GetAxis("Mouse X") * _mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivity;

            // Apply aim instability if enabled
            if (_enableAimInstability)
            {
                float instability = _aimInstabilityAmount * _stressLevel;
                mouseX += Mathf.PerlinNoise(Time.time * 3f, 0f) * instability - instability * 0.5f;
                mouseY += Mathf.PerlinNoise(0f, Time.time * 3f) * instability - instability * 0.5f;
            }

            // Horizontal rotation (rotate player body)
            transform.Rotate(Vector3.up * mouseX);

            // Vertical rotation (rotate camera holder)
            _cameraPitch -= mouseY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, -_maxLookAngle, _maxLookAngle);

            // Smooth camera pitch
            _currentCameraPitch = Mathf.SmoothDamp(
                _currentCameraPitch,
                _cameraPitch,
                ref _pitchSmoothVelocity,
                _cameraSmoothing
            );

            _cameraHolder.localEulerAngles = Vector3.right * _currentCameraPitch;
        }

        // =====================================================================
        // HEAD BOB
        // =====================================================================

        private void HandleHeadBob()
        {
            if (!_enableHeadBob) return;

            bool isMoving = _currentMoveVelocity.sqrMagnitude > 0.1f && _isGrounded;

            if (isMoving)
            {
                float speed = _isSprinting ? _bobFrequency * 1.5f : _bobFrequency;
                _bobTimer += Time.deltaTime * speed;

                float bobX = Mathf.Sin(_bobTimer) * _bobAmplitudeX;
                float bobY = Mathf.Sin(_bobTimer * 2f) * _bobAmplitudeY;

                Vector3 bobOffset = new Vector3(bobX, bobY, 0f);
                _cameraHolder.localPosition = _defaultCameraPosition + bobOffset;
            }
            else
            {
                // Smoothly return to default position when stationary
                _bobTimer = 0f;
                _cameraHolder.localPosition = Vector3.Lerp(
                    _cameraHolder.localPosition,
                    _defaultCameraPosition,
                    Time.deltaTime * 5f
                );
            }
        }

        // =====================================================================
        // BREATHING EFFECT
        // =====================================================================

        private void HandleBreathing()
        {
            if (!_enableBreathing) return;

            _breathTimer += Time.deltaTime * _breathFrequency;
            float breathOffset = Mathf.Sin(_breathTimer * Mathf.PI * 2f) * _breathAmplitude;

            // Apply breathing as subtle vertical camera offset
            Vector3 pos = _cameraHolder.localPosition;
            pos.y += breathOffset;
            _cameraHolder.localPosition = pos;
        }

        // =====================================================================
        // CAMERA SWAY
        // =====================================================================

        private void HandleCameraSway()
        {
            if (!_enableCameraSway) return;

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Calculate sway based on mouse movement
            Vector3 targetSway = new Vector3(-mouseY * _swayAmount, mouseX * _swayAmount, mouseX * _swayAmount);
            _swayOffset = Vector3.Lerp(_swayOffset, targetSway, Time.deltaTime * _swaySmoothing);

            _cameraHolder.localRotation *= Quaternion.Euler(_swayOffset);
        }

        // =====================================================================
        // CAMERA DRIFT (Psychological Enhancement)
        // =====================================================================

        private void HandleDrift()
        {
            if (!_enableDrift) return;

            // Slow, Perlin-noise-driven camera drift to create unease
            _driftAngle += Time.deltaTime * 0.1f;
            float driftX = (Mathf.PerlinNoise(_driftAngle, 0f) - 0.5f) * _driftIntensity * _stressLevel;
            float driftY = (Mathf.PerlinNoise(0f, _driftAngle) - 0.5f) * _driftIntensity * _stressLevel;

            _cameraHolder.localRotation *= Quaternion.Euler(driftX, driftY, 0f);
        }

        // =====================================================================
        // DYNAMIC FOV
        // =====================================================================

        private void HandleFOV()
        {
            if (_camera == null) return;

            // Calculate target FOV based on state
            _targetFOV = _baseFOV;

            if (_isSprinting)
            {
                _targetFOV += _sprintFOVIncrease;
            }

            // Stress narrows FOV (tunnel vision effect)
            _targetFOV -= _stressFOVDecrease * _stressLevel;

            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _targetFOV, Time.deltaTime * _fovTransitionSpeed);
        }

        // =====================================================================
        // FOOTSTEPS
        // =====================================================================

        private void HandleFootsteps()
        {
            if (_footstepClips == null || _footstepClips.Length == 0) return;
            if (!_isGrounded || _currentMoveVelocity.sqrMagnitude < 0.1f) return;

            float interval = _isSprinting ? _footstepInterval * 0.7f : _footstepInterval;
            _footstepTimer += Time.deltaTime;

            if (_footstepTimer >= interval)
            {
                _footstepTimer = 0f;
                PlayFootstep();
            }
        }

        private void PlayFootstep()
        {
            // Random clip selection for variation
            AudioClip clip = _footstepClips[Random.Range(0, _footstepClips.Length)];

            // Slight random pitch for variety
            _footstepAudioSource.pitch = Random.Range(0.9f, 1.1f);
            _footstepAudioSource.PlayOneShot(clip, _footstepVolume);
        }

        // =====================================================================
        // PUBLIC API — Called by other systems
        // =====================================================================

        /// <summary>
        /// Sets the stress level (0–1). Affects FOV, drift, and instability.
        /// Called by the Environment State Manager or event system.
        /// </summary>
        public void SetStressLevel(float level)
        {
            _stressLevel = Mathf.Clamp01(level);
        }

        /// <summary>
        /// Locks or unlocks player input. Used during cutscenes and events.
        /// </summary>
        public void SetInputLocked(bool locked)
        {
            _inputLocked = locked;

            if (locked)
            {
                _currentMoveVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Enables or disables psychological enhancements at runtime.
        /// Called by the scripted event system to escalate tension.
        /// </summary>
        public void SetPsychologicalEffects(bool drift, bool microDelay, bool aimInstability)
        {
            _enableDrift = drift;
            _enableMicroDelay = microDelay;
            _enableAimInstability = aimInstability;
        }

        /// <summary>
        /// Updates mouse sensitivity. Useful for settings menu.
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            _mouseSensitivity = sensitivity;
        }

        /// <summary>
        /// Gets the camera transform for raycast origin.
        /// </summary>
        public Transform GetCameraTransform()
        {
            return _camera != null ? _camera.transform : _cameraHolder;
        }
    }
}
