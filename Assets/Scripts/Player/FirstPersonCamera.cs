// ============================================================================
// FirstPersonCamera.cs — All camera behaviour for the first-person controller
// Handles: mouse look, head bob, breathing, sway, drift, dynamic FOV.
// Separated from movement logic for clean modularity.
// ============================================================================

using UnityEngine;
using UnityEngine.InputSystem;

namespace FracturedEchoes.Player
{
    /// <summary>
    /// Manages every aspect of the first-person camera:
    /// mouse look (with vertical clamp), head bob, camera sway,
    /// breathing effect, Perlin-noise drift, and dynamic FOV changes.
    /// Reads motor state (sprinting, grounded, velocity) but never modifies it.
    /// </summary>
    public class FirstPersonCamera : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("References")]
        [Tooltip("The transform that holds the camera (child of player).")]
        [SerializeField] private Transform _cameraHolder;

        [Header("Mouse Look")]
        [Tooltip("Mouse look sensitivity (scaled by Time.deltaTime internally).")]
        [SerializeField] private float _mouseSensitivity = 2f;

        [Tooltip("Maximum vertical look angle in degrees.")]
        [SerializeField, Range(60f, 89f)] private float _maxLookAngle = 80f;

        [Tooltip("Smoothing applied to vertical pitch. Lower = snappier.")]
        [SerializeField, Range(0f, 0.15f)] private float _cameraSmoothing = 0.05f;

        [Header("Head Bob")]
        [Tooltip("Enable head bob while moving.")]
        [SerializeField] private bool _enableHeadBob = true;

        [Tooltip("Bob cycles per second while walking.")]
        [SerializeField] private float _bobFrequency = 1.8f;

        [Tooltip("Horizontal bob amplitude.")]
        [SerializeField] private float _bobAmplitudeX = 0.005f;

        [Tooltip("Vertical bob amplitude (metres).")]
        [SerializeField] private float _bobAmplitudeY = 0.008f;

        [Tooltip("Speed multiplier for bob frequency while sprinting.")]
        [SerializeField] private float _sprintBobMultiplier = 1.5f;

        [Header("Camera Sway")]
        [Tooltip("Enable subtle rotation sway based on mouse movement.")]
        [SerializeField] private bool _enableCameraSway = true;

        [Tooltip("Sway rotation intensity.")]
        [SerializeField] private float _swayAmount = 0.02f;

        [Tooltip("Sway interpolation speed.")]
        [SerializeField] private float _swaySmoothing = 4f;

        [Header("Breathing Effect")]
        [Tooltip("Enable slow vertical oscillation simulating breathing.")]
        [SerializeField] private bool _enableBreathing = true;

        [Tooltip("Breathing cycle frequency.")]
        [SerializeField] private float _breathFrequency = 0.3f;

        [Tooltip("Breathing positional amplitude.")]
        [SerializeField] private float _breathAmplitude = 0.003f;

        [Header("Dynamic FOV")]
        [Tooltip("Base field of view at rest.")]
        [SerializeField] private float _baseFOV = 60f;

        [Tooltip("FOV increase while sprinting.")]
        [SerializeField] private float _sprintFOVIncrease = 8f;

        [Tooltip("FOV decrease at max stress (tunnel vision).")]
        [SerializeField] private float _stressFOVDecrease = 5f;

        [Tooltip("Speed of FOV interpolation.")]
        [SerializeField] private float _fovTransitionSpeed = 4f;

        [Header("Drift (Psychological Enhancement)")]
        [Tooltip("Enable slow Perlin-noise camera drift to create unease.")]
        [SerializeField] private bool _enableDrift = false;

        [Tooltip("Maximum drift rotation intensity.")]
        [SerializeField] private float _driftIntensity = 0.1f;

        [Header("Aim Instability (Psychological Enhancement)")]
        [Tooltip("Enable Perlin-noise jitter on look input.")]
        [SerializeField] private bool _enableAimInstability = false;

        [Tooltip("Maximum instability rotation at full stress.")]
        [SerializeField] private float _aimInstabilityAmount = 0.2f;

        // =====================================================================
        // RUNTIME STATE
        // =====================================================================

        /// <summary>Scale factor to convert raw pixel delta to values similar to old Input.GetAxis.</summary>
        private const float LOOK_INPUT_SCALE = 0.05f;

        private Camera _camera;
        private InputAction _lookAction;
        private Vector3 _defaultCameraPos;
        private Vector3 _standingCameraPos;

        // Mouse look
        private float _cameraPitch;
        private float _smoothedPitch;
        private float _pitchSmoothVel;

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

        // Stress (0–1), controlled externally
        private float _stressLevel;

        // =====================================================================
        // PUBLIC PROPERTIES
        // =====================================================================

        /// <summary>Returns the camera Transform for raycasting.</summary>
        public Transform CameraTransform => _camera != null ? _camera.transform : _cameraHolder;

        /// <summary>Gets or sets the camera pitch angle (for save/restore).</summary>
        public float CameraPitch
        {
            get => _cameraPitch;
            set
            {
                _cameraPitch = value;
                _smoothedPitch = value;
                if (_cameraHolder != null)
                    _cameraHolder.localEulerAngles = Vector3.right * value;
            }
        }

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        private void Awake()
        {
            if (_cameraHolder == null)
            {
                Debug.LogError("[FirstPersonCamera] _cameraHolder is not assigned!", this);
                enabled = false;
                return;
            }

            _camera = _cameraHolder.GetComponentInChildren<Camera>();
            _standingCameraPos = _cameraHolder.localPosition;
            _defaultCameraPos = _standingCameraPos;
            _targetFOV = _baseFOV;

            // Cache input action from project-wide Input System
            _lookAction = InputSystem.actions?.FindAction("Player/Look");
        }

        // =====================================================================
        // CORE TICK — Called by FirstPersonController.Update()
        // =====================================================================

        /// <summary>
        /// Processes one frame of camera logic.
        /// <paramref name="motor"/> is read-only — we only query its state.
        /// </summary>
        public void Tick(FirstPersonMotor motor)
        {
            Vector2 lookInput = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;
            HandleMouseLook(lookInput);
            HandleCrouchCamera(motor);
            HandleHeadBob(motor);
            HandleBreathing();
            HandleCameraSway(lookInput);
            HandleDrift();
            HandleFOV(motor);
        }

        // =====================================================================
        // CROUCH CAMERA OFFSET
        // =====================================================================

        private void HandleCrouchCamera(FirstPersonMotor motor)
        {
            // Smoothly lower/raise the default camera position based on crouch amount
            float standingY = _standingCameraPos.y;
            float crouchY = standingY * 0.55f;  // ~55% of standing height
            float targetY = Mathf.Lerp(standingY, crouchY, motor.CrouchAmount);

            _defaultCameraPos = new Vector3(_standingCameraPos.x, targetY, _standingCameraPos.z);
        }

        // =====================================================================
        // MOUSE LOOK
        // =====================================================================

        private void HandleMouseLook(Vector2 lookDelta)
        {
            float mouseX = lookDelta.x * LOOK_INPUT_SCALE * _mouseSensitivity;
            float mouseY = lookDelta.y * LOOK_INPUT_SCALE * _mouseSensitivity;

            // Apply aim instability when stressed
            if (_enableAimInstability && _stressLevel > 0f)
            {
                float instability = _aimInstabilityAmount * _stressLevel;
                mouseX += (Mathf.PerlinNoise(Time.time * 3f, 0f) - 0.5f) * instability;
                mouseY += (Mathf.PerlinNoise(0f, Time.time * 3f) - 0.5f) * instability;
            }

            // Horizontal: rotate the entire player body
            transform.Rotate(Vector3.up * mouseX);

            // Vertical: pitch the camera holder with clamp
            _cameraPitch -= mouseY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, -_maxLookAngle, _maxLookAngle);

            // Smooth vertical pitch to soften hard mouse movements
            _smoothedPitch = Mathf.SmoothDamp(
                _smoothedPitch,
                _cameraPitch,
                ref _pitchSmoothVel,
                _cameraSmoothing
            );

            _cameraHolder.localEulerAngles = Vector3.right * _smoothedPitch;
        }

        // =====================================================================
        // HEAD BOB
        // =====================================================================

        private void HandleHeadBob(FirstPersonMotor motor)
        {
            if (!_enableHeadBob) return;

            bool shouldBob = motor.IsMoving && motor.IsGrounded;

            if (shouldBob)
            {
                float freq = motor.IsSprinting ? _bobFrequency * _sprintBobMultiplier : _bobFrequency;
                _bobTimer += Time.deltaTime * freq;

                // Horizontal bob on sin, vertical on sin(2x) for a natural gait
                float bobX = Mathf.Sin(_bobTimer) * _bobAmplitudeX;
                float bobY = Mathf.Sin(_bobTimer * 2f) * _bobAmplitudeY;

                _cameraHolder.localPosition = _defaultCameraPos + new Vector3(bobX, bobY, 0f);
            }
            else
            {
                // Smoothly settle back to default when stationary
                _bobTimer = 0f;
                _cameraHolder.localPosition = Vector3.Lerp(
                    _cameraHolder.localPosition,
                    _defaultCameraPos,
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
            float breathY = Mathf.Sin(_breathTimer * Mathf.PI * 2f) * _breathAmplitude;

            // Layer on top of current camera holder position
            Vector3 pos = _cameraHolder.localPosition;
            pos.y += breathY;
            _cameraHolder.localPosition = pos;
        }

        // =====================================================================
        // CAMERA SWAY
        // =====================================================================

        private void HandleCameraSway(Vector2 swayDelta)
        {
            if (!_enableCameraSway) return;

            float mouseX = swayDelta.x * LOOK_INPUT_SCALE;
            float mouseY = swayDelta.y * LOOK_INPUT_SCALE;

            Vector3 targetSway = new Vector3(
                -mouseY * _swayAmount,
                 mouseX * _swayAmount,
                 mouseX * _swayAmount
            );

            _swayOffset = Vector3.Lerp(_swayOffset, targetSway, Time.deltaTime * _swaySmoothing);
            _cameraHolder.localRotation *= Quaternion.Euler(_swayOffset);
        }

        // =====================================================================
        // DRIFT (Psychological Enhancement)
        // =====================================================================

        private void HandleDrift()
        {
            if (!_enableDrift || _stressLevel <= 0f) return;

            _driftAngle += Time.deltaTime * 0.1f;
            float driftX = (Mathf.PerlinNoise(_driftAngle, 0f) - 0.5f) * _driftIntensity * _stressLevel;
            float driftY = (Mathf.PerlinNoise(0f, _driftAngle) - 0.5f) * _driftIntensity * _stressLevel;

            _cameraHolder.localRotation *= Quaternion.Euler(driftX, driftY, 0f);
        }

        // =====================================================================
        // DYNAMIC FOV
        // =====================================================================

        private void HandleFOV(FirstPersonMotor motor)
        {
            if (_camera == null) return;

            _targetFOV = _baseFOV;

            // Sprint widens FOV for a rush effect
            if (motor.IsSprinting)
            {
                _targetFOV += _sprintFOVIncrease;
            }

            // Stress narrows FOV (tunnel vision)
            _targetFOV -= _stressFOVDecrease * _stressLevel;

            _camera.fieldOfView = Mathf.Lerp(
                _camera.fieldOfView,
                _targetFOV,
                Time.deltaTime * _fovTransitionSpeed
            );
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>Sets the stress level (0–1), used by drift, instability, and FOV.</summary>
        public void SetStressLevel(float level)
        {
            _stressLevel = Mathf.Clamp01(level);
        }

        /// <summary>Adjustable mouse sensitivity for in-game settings.</summary>
        public void SetSensitivity(float sensitivity)
        {
            _mouseSensitivity = Mathf.Max(0.1f, sensitivity);
        }

        /// <summary>Enable or disable psychological camera effects at runtime.</summary>
        public void SetPsychologicalEffects(bool drift, bool aimInstability)
        {
            _enableDrift = drift;
            _enableAimInstability = aimInstability;
        }

        /// <summary>Enable or disable head bobbing at runtime.</summary>
        public void SetHeadBobEnabled(bool enabled)
        {
            _enableHeadBob = enabled;
        }

        /// <summary>Set head bob intensity (amplitudes) at runtime. Values are clamped 0–0.2.</summary>
        public void SetHeadBobIntensity(float amplitudeX, float amplitudeY)
        {
            _bobAmplitudeX = Mathf.Clamp(amplitudeX, 0f, 0.2f);
            _bobAmplitudeY = Mathf.Clamp(amplitudeY, 0f, 0.2f);
        }

        /// <summary>Returns current mouse sensitivity value.</summary>
        public float GetSensitivity() => _mouseSensitivity;

        /// <summary>Returns current head bob X amplitude.</summary>
        public float GetHeadBobAmplitudeX() => _bobAmplitudeX;

        /// <summary>Returns current head bob Y amplitude.</summary>
        public float GetHeadBobAmplitudeY() => _bobAmplitudeY;

        /// <summary>Returns whether head bob is enabled.</summary>
        public bool IsHeadBobEnabled() => _enableHeadBob;
    }
}
