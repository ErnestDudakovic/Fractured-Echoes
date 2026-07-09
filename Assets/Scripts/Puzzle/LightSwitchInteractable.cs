// ============================================================================
// LightSwitchInteractable.cs — Toggleable light switch for light puzzles
// Extends InteractableObject. Toggles a Light (and optional emissive object)
// on/off and notifies listeners (LightPatternPuzzle) of state changes.
// ============================================================================

using System;
using UnityEngine;
using FracturedEchoes.Interaction;

namespace FracturedEchoes.Puzzle
{
    /// <summary>
    /// A switch the player can toggle to turn a light on or off.
    /// Used as a building block for <see cref="LightPatternPuzzle"/>,
    /// but works standalone for ambience (lamps, candles, fuse boxes).
    /// </summary>
    public class LightSwitchInteractable : InteractableObject
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Switch Settings")]
        [Tooltip("The light this switch controls.")]
        [SerializeField] private Light _controlledLight;

        [Tooltip("Optional object toggled with the light (e.g. glowing bulb mesh).")]
        [SerializeField] private GameObject _emissiveObject;

        [Tooltip("Starting state of the switch.")]
        [SerializeField] private bool _startsOn;

        [Header("Switch Audio")]
        [Tooltip("Sound played when toggled on.")]
        [SerializeField] private AudioClip _switchOnSound;

        [Tooltip("Sound played when toggled off.")]
        [SerializeField] private AudioClip _switchOffSound;

        [Header("Switch Visual")]
        [Tooltip("Optional handle transform rotated when toggled.")]
        [SerializeField] private Transform _switchHandle;

        [Tooltip("Local X rotation applied to the handle when ON.")]
        [SerializeField] private float _onAngle = -25f;

        [Tooltip("Local X rotation applied to the handle when OFF.")]
        [SerializeField] private float _offAngle = 25f;

        // =====================================================================
        // RUNTIME STATE
        // =====================================================================

        private bool _isOn;
        private AudioSource _switchAudio;

        // =====================================================================
        // PROPERTIES / EVENTS
        // =====================================================================

        /// <summary>Current on/off state of the switch.</summary>
        public bool IsOn => _isOn;

        /// <summary>Fired whenever the switch is toggled (switch, newState).</summary>
        public event Action<LightSwitchInteractable, bool> Toggled;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        protected override void Awake()
        {
            base.Awake();

            _switchAudio = GetComponent<AudioSource>();
            if (_switchAudio == null)
            {
                _switchAudio = gameObject.AddComponent<AudioSource>();
                _switchAudio.playOnAwake = false;
                _switchAudio.spatialBlend = 1f;
            }

            _isOn = _startsOn;
            ApplyState(instant: true);
        }

        // =====================================================================
        // INTERACTION
        // =====================================================================

        public override void OnInteract()
        {
            if (!CanInteract) return;

            _isOn = !_isOn;
            ApplyState(instant: false);

            // Notify puzzle / listeners
            Toggled?.Invoke(this, _isOn);

            // Base handles interaction event / single-use / generic sound
            base.OnInteract();
        }

        // =====================================================================
        // STATE
        // =====================================================================

        /// <summary>Sets the switch state directly (scripted events, loading).</summary>
        public void SetState(bool on, bool notify = false)
        {
            if (_isOn == on) return;

            _isOn = on;
            ApplyState(instant: true);

            if (notify)
                Toggled?.Invoke(this, _isOn);
        }

        private void ApplyState(bool instant)
        {
            if (_controlledLight != null)
                _controlledLight.enabled = _isOn;

            if (_emissiveObject != null)
                _emissiveObject.SetActive(_isOn);

            if (_switchHandle != null)
            {
                float angle = _isOn ? _onAngle : _offAngle;
                _switchHandle.localRotation = Quaternion.Euler(angle, 0f, 0f);
            }

            if (!instant)
            {
                AudioClip clip = _isOn ? _switchOnSound : _switchOffSound;
                if (clip != null)
                    _switchAudio.PlayOneShot(clip);
            }
        }
    }
}
