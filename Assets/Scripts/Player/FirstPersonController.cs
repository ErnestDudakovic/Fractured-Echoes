// ============================================================================
// FirstPersonController.cs — Lightweight coordinator for the FPS system
// Delegates movement to FirstPersonMotor and camera to FirstPersonCamera.
// Owns: cursor lock, footsteps, event broadcasting, and the public API
// that other game systems (events, puzzles, UI) call into.
// ============================================================================

using UnityEngine;
using FracturedEchoes.Core.Events;
using FracturedEchoes.Core.Interfaces;

namespace FracturedEchoes.Player
{
    /// <summary>
    /// Top-level coordinator for the first-person controller.
    /// Attach to the Player root GameObject alongside <see cref="FirstPersonMotor"/>
    /// and <see cref="FirstPersonCamera"/> (all on the same GameObject).
    /// Implements ISaveable to persist player position and rotation.
    /// </summary>
    [RequireComponent(typeof(FirstPersonMotor))]
    [RequireComponent(typeof(FirstPersonCamera))]
    public class FirstPersonController : MonoBehaviour, ISaveable
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Footsteps")]
        [Tooltip("Array of footstep clips — a random one is played each step.")]
        [SerializeField] private AudioClip[] _footstepClips;

        [Tooltip("Seconds between footstep sounds while walking.")]
        [SerializeField] private float _footstepInterval = 0.5f;

        [Tooltip("Footstep volume (0–1).")]
        [SerializeField, Range(0f, 1f)] private float _footstepVolume = 0.4f;

        [Header("Game Events")]
        [Tooltip("Raised the frame the player starts moving.")]
        [SerializeField] private GameEvent _onPlayerMoved;

        [Tooltip("Raised the frame the player stops moving.")]
        [SerializeField] private GameEvent _onPlayerStopped;

        // =====================================================================
        // COMPONENT REFERENCES
        // =====================================================================

        private FirstPersonMotor  _motor;
        private FirstPersonCamera _fpCamera;
        private AudioSource       _footstepSource;

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private bool _wasMoving;
        private float _footstepTimer;
        private bool _inputLocked;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            _motor    = GetComponent<FirstPersonMotor>();
            _fpCamera = GetComponent<FirstPersonCamera>();

            // Dedicated audio source for footsteps
            _footstepSource = gameObject.AddComponent<AudioSource>();
            _footstepSource.playOnAwake  = false;
            _footstepSource.spatialBlend = 0f;

            // Lock cursor for first-person control
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void Update()
        {
            if (_inputLocked) return;

            // Tick the two modules — order matters (movement before camera)
            _motor.Tick();
            _fpCamera.Tick(_motor);

            // Controller-owned features
            HandleFootsteps();
            BroadcastMovementEvents();
        }

        // =====================================================================
        // FOOTSTEPS
        // =====================================================================

        private void HandleFootsteps()
        {
            if (_footstepClips == null || _footstepClips.Length == 0) return;
            if (!_motor.IsGrounded || !_motor.IsMoving) return;

            float interval = _motor.IsSprinting
                ? _footstepInterval * 0.7f
                : _footstepInterval;

            _footstepTimer += Time.deltaTime;

            if (_footstepTimer >= interval)
            {
                _footstepTimer = 0f;
                PlayFootstep();
            }
        }

        private void PlayFootstep()
        {
            AudioClip clip = _footstepClips[Random.Range(0, _footstepClips.Length)];
            _footstepSource.pitch = Random.Range(0.9f, 1.1f);
            _footstepSource.PlayOneShot(clip, _footstepVolume);
        }

        // =====================================================================
        // EVENT BROADCASTING
        // =====================================================================

        private void BroadcastMovementEvents()
        {
            bool isMoving = _motor.IsMoving;

            if (isMoving && !_wasMoving) _onPlayerMoved?.Raise();
            if (!isMoving && _wasMoving) _onPlayerStopped?.Raise();

            _wasMoving = isMoving;
        }

        // =====================================================================
        // PUBLIC API — Called by other game systems
        // =====================================================================

        /// <summary>
        /// Sets stress level (0–1). Forwards to camera for FOV / drift / instability.
        /// </summary>
        public void SetStressLevel(float level)
        {
            _fpCamera.SetStressLevel(level);
        }

        /// <summary>
        /// Locks or unlocks all player input. Used during cutscenes and scripted events.
        /// </summary>
        public void SetInputLocked(bool locked)
        {
            _inputLocked = locked;

            if (locked)
            {
                _motor.StopImmediately();
            }
        }

        /// <summary>
        /// Enables or disables psychological enhancements at runtime.
        /// </summary>
        public void SetPsychologicalEffects(bool drift, bool microDelay, bool aimInstability)
        {
            _fpCamera.SetPsychologicalEffects(drift, aimInstability);
            _motor.SetMicroDelay(microDelay);
        }

        /// <summary>
        /// Updates mouse sensitivity (e.g. from a settings menu).
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            _fpCamera.SetSensitivity(sensitivity);
        }

        /// <summary>
        /// Enable or disable head bobbing.
        /// </summary>
        public void SetHeadBobEnabled(bool enabled)
        {
            _fpCamera.SetHeadBobEnabled(enabled);
        }

        /// <summary>
        /// Set head bob intensity.
        /// </summary>
        public void SetHeadBobIntensity(float amplitudeX, float amplitudeY)
        {
            _fpCamera.SetHeadBobIntensity(amplitudeX, amplitudeY);
        }

        /// <summary>
        /// Gets the FirstPersonCamera component for settings queries.
        /// </summary>
        public FirstPersonCamera GetCamera() => _fpCamera;

        /// <summary>
        /// Gets the FirstPersonMotor component.
        /// </summary>
        public FirstPersonMotor GetMotor() => _motor;

        /// <summary>
        /// Returns the camera Transform for raycast origin (used by InteractionSystem).
        /// </summary>
        public Transform GetCameraTransform()
        {
            return _fpCamera.CameraTransform;
        }

        // =====================================================================
        // ISAVEABLE IMPLEMENTATION
        // =====================================================================

        public string SaveID => "player_controller";

        [System.Serializable]
        private struct PlayerSaveData
        {
            public float posX, posY, posZ;
            public float rotY;       // horizontal yaw
            public float cameraPitch; // vertical look angle
        }

        public object CaptureState()
        {
            return new PlayerSaveData
            {
                posX = transform.position.x,
                posY = transform.position.y,
                posZ = transform.position.z,
                rotY = transform.eulerAngles.y,
                cameraPitch = _fpCamera != null ? _fpCamera.CameraPitch : 0f
            };
        }

        public void RestoreState(object state)
        {
            if (state is not PlayerSaveData data) return;

            // CharacterController overrides transform.position — must disable first
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            transform.position = new Vector3(data.posX, data.posY, data.posZ);
            transform.eulerAngles = new Vector3(0f, data.rotY, 0f);

            if (cc != null) cc.enabled = true;

            // Restore camera pitch
            if (_fpCamera != null)
                _fpCamera.CameraPitch = data.cameraPitch;
        }
    }
}
