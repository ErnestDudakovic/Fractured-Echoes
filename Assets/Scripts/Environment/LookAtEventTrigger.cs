// ============================================================================
// LookAtEventTrigger.cs — Fires a scripted event when the player looks at
// this object. Implements the previously-unwired TriggerType.LookAtObject.
// Uses view angle + distance + optional line-of-sight, with a configurable
// minimum look duration so a passing glance doesn't trigger the event.
// ============================================================================

using UnityEngine;

namespace FracturedEchoes.Environment
{
    /// <summary>
    /// Attach to any object that should fire a scripted event when the player
    /// looks at it. Wire the event via a specific event ID, or leave the ID
    /// empty to fire all LookAtObject-type events on the controller.
    /// </summary>
    public class LookAtEventTrigger : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Event")]
        [Tooltip("Specific event ID to trigger. Leave empty to trigger all LookAtObject events.")]
        [SerializeField] private string _eventID;

        [Tooltip("The scripted event controller to notify. Auto-found if null.")]
        [SerializeField] private ScriptedEventController _eventController;

        [Header("Detection")]
        [Tooltip("Maximum angle (degrees) between the camera forward and this object.")]
        [SerializeField, Range(1f, 45f)] private float _viewAngle = 10f;

        [Tooltip("Maximum distance from the camera for detection.")]
        [SerializeField] private float _maxDistance = 15f;

        [Tooltip("Seconds the player must keep looking before the event fires.")]
        [SerializeField] private float _requiredLookTime = 0.35f;

        [Tooltip("Require an unobstructed line of sight from the camera.")]
        [SerializeField] private bool _requireLineOfSight = true;

        [Tooltip("Layers that block line of sight.")]
        [SerializeField] private LayerMask _occlusionMask = ~0;

        [Header("Behaviour")]
        [Tooltip("Only fire once, then disable this component.")]
        [SerializeField] private bool _oneTimeOnly = true;

        // =====================================================================
        // RUNTIME STATE
        // =====================================================================

        private Camera _camera;
        private float _lookTimer;
        private bool _hasFired;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            if (_eventController == null)
                _eventController = FindFirstObjectByType<ScriptedEventController>();
        }

        private void Update()
        {
            if (_hasFired && _oneTimeOnly) return;
            if (_eventController == null) return;

            // Lazily resolve the camera (it may not exist during Awake)
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return;
            }

            if (IsPlayerLooking())
            {
                _lookTimer += Time.deltaTime;

                if (_lookTimer >= _requiredLookTime)
                {
                    Fire();
                }
            }
            else
            {
                _lookTimer = 0f;
            }
        }

        // =====================================================================
        // DETECTION
        // =====================================================================

        private bool IsPlayerLooking()
        {
            Vector3 camPos = _camera.transform.position;
            Vector3 toTarget = transform.position - camPos;
            float distance = toTarget.magnitude;

            if (distance > _maxDistance) return false;

            // Angle check: is this object near the centre of the view?
            float angle = Vector3.Angle(_camera.transform.forward, toTarget);
            if (angle > _viewAngle) return false;

            // Optional line-of-sight check
            if (_requireLineOfSight)
            {
                if (Physics.Raycast(camPos, toTarget.normalized, out RaycastHit hit,
                    distance, _occlusionMask, QueryTriggerInteraction.Ignore))
                {
                    // Blocked, unless the hit is this object (or a child of it)
                    if (hit.transform != transform && !hit.transform.IsChildOf(transform))
                        return false;
                }
            }

            return true;
        }

        // =====================================================================
        // FIRING
        // =====================================================================

        private void Fire()
        {
            _hasFired = true;
            _lookTimer = 0f;

            if (!string.IsNullOrEmpty(_eventID))
            {
                _eventController.TriggerByID(_eventID);
            }
            else
            {
                _eventController.TriggerByType(ScriptableObjects.TriggerType.LookAtObject);
            }

            Debug.Log($"[LookAtTrigger] Fired: {gameObject.name}");

            if (_oneTimeOnly)
            {
                enabled = false;
            }
        }

        /// <summary>Resets the trigger so it can fire again.</summary>
        public void ResetTrigger()
        {
            _hasFired = false;
            _lookTimer = 0f;
            enabled = true;
        }
    }
}
