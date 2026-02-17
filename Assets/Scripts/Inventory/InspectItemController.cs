// ============================================================================
// InspectItemController.cs — 3D item inspection system
// Handles spawning, rotating, and dismissing inspection-view objects.
// Extracted from InventoryManager so each system has a single responsibility.
// ============================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using FracturedEchoes.ScriptableObjects;

namespace FracturedEchoes.InventorySystem
{
    /// <summary>
    /// Controls item inspection mode. Spawns a 3D model at the inspection point
    /// and allows the player to rotate it with the mouse.
    /// Attach to the same GameObject as InventoryManager or a dedicated UI root.
    /// </summary>
    public class InspectItemController : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Inspection Setup")]
        [Tooltip("World-space transform where the inspection prefab is placed.")]
        [SerializeField] private Transform _inspectionPoint;

        [Tooltip("Camera used for inspection view (enabled/disabled on demand).")]
        [SerializeField] private Camera _inspectionCamera;

        [Tooltip("Mouse-drag rotation speed while inspecting.")]
        [SerializeField] private float _rotateSpeed = 100f;

        [Tooltip("Optional zoom speed via scroll wheel.")]
        [SerializeField] private float _zoomSpeed = 0.5f;

        [Tooltip("Minimum zoom distance from inspection point.")]
        [SerializeField] private float _minZoomDistance = 0.3f;

        [Tooltip("Maximum zoom distance from inspection point.")]
        [SerializeField] private float _maxZoomDistance = 2f;

        // =====================================================================
        // RUNTIME STATE
        // =====================================================================

        private GameObject _spawnedObject;
        private ItemData _currentItem;
        private bool _isInspecting;
        private float _currentZoom;

        // Input actions
        private const float LOOK_INPUT_SCALE = 0.05f;
        private InputAction _lookAction;
        private InputAction _attackAction;
        private InputAction _scrollAction;
        private InputAction _cancelAction;
        private InputAction _rightClickAction;

        // =====================================================================
        // PUBLIC PROPERTIES
        // =====================================================================

        /// <summary>True while the player is inspecting an item.</summary>
        public bool IsInspecting => _isInspecting;

        /// <summary>The item currently being inspected (null if none).</summary>
        public ItemData CurrentItem => _currentItem;

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Opens the inspection view for the given item.
        /// </summary>
        public void StartInspection(ItemData item)
        {
            if (item == null || item.inspectionPrefab == null)
            {
                Debug.LogWarning("[Inspect] Item or inspection prefab is null.");
                return;
            }

            if (_inspectionPoint == null)
            {
                Debug.LogError("[Inspect] Inspection point is not assigned!", this);
                return;
            }

            // Close any previous inspection first
            EndInspection();

            _currentItem = item;
            _isInspecting = true;

            // Spawn the 3D model
            _spawnedObject = Instantiate(
                item.inspectionPrefab,
                _inspectionPoint.position,
                Quaternion.identity,
                _inspectionPoint
            );

            // Reset zoom
            _currentZoom = Vector3.Distance(
                _inspectionCamera != null ? _inspectionCamera.transform.position : _inspectionPoint.position,
                _inspectionPoint.position
            );

            // Enable inspection camera
            if (_inspectionCamera != null)
            {
                _inspectionCamera.gameObject.SetActive(true);
            }

            // Unlock cursor so the player can drag
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log($"[Inspect] Started inspecting: {item.displayName}");
        }

        /// <summary>
        /// Closes the inspection view and cleans up.
        /// </summary>
        public void EndInspection()
        {
            if (_spawnedObject != null)
            {
                Destroy(_spawnedObject);
                _spawnedObject = null;
            }

            _currentItem = null;
            _isInspecting = false;

            // Disable inspection camera
            if (_inspectionCamera != null)
            {
                _inspectionCamera.gameObject.SetActive(false);
            }

            // Re-lock cursor for FPS control
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Update()
        {
            if (!_isInspecting || _spawnedObject == null) return;

            HandleRotation();
            HandleZoom();
            HandleCloseInput();
        }

        private void Awake()
        {
            _lookAction       = InputSystem.actions?.FindAction("Player/Look");
            _attackAction     = InputSystem.actions?.FindAction("Player/Attack");
            _scrollAction     = InputSystem.actions?.FindAction("UI/ScrollWheel");
            _cancelAction     = InputSystem.actions?.FindAction("UI/Cancel");
            _rightClickAction = InputSystem.actions?.FindAction("UI/RightClick");
        }

        // =====================================================================
        // INPUT HANDLING
        // =====================================================================

        private void HandleRotation()
        {
            if (!(_attackAction?.IsPressed() ?? false)) return;

            Vector2 lookDelta = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;
            float rotX = lookDelta.x * LOOK_INPUT_SCALE * _rotateSpeed * Time.deltaTime;
            float rotY = lookDelta.y * LOOK_INPUT_SCALE * _rotateSpeed * Time.deltaTime;

            // Rotate the object around world axes for intuitive control
            _spawnedObject.transform.Rotate(Vector3.up, -rotX, Space.World);
            _spawnedObject.transform.Rotate(Vector3.right, rotY, Space.World);
        }

        private void HandleZoom()
        {
            Vector2 scrollDelta = _scrollAction?.ReadValue<Vector2>() ?? Vector2.zero;
            float scroll = scrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f) return;
            if (_inspectionCamera == null) return;

            // Normalize scroll (raw delta can be large ~120 per notch)
            scroll = Mathf.Clamp(scroll / 120f, -1f, 1f);

            _currentZoom -= scroll * _zoomSpeed;
            _currentZoom = Mathf.Clamp(_currentZoom, _minZoomDistance, _maxZoomDistance);

            // Move the camera along its forward axis toward the inspection point
            Vector3 dir = (_inspectionPoint.position - _inspectionCamera.transform.position).normalized;
            _inspectionCamera.transform.position = _inspectionPoint.position - dir * _currentZoom;
        }

        private void HandleCloseInput()
        {
            bool rightClicked  = _rightClickAction?.WasPressedThisFrame() ?? false;
            bool cancelPressed = _cancelAction?.WasPressedThisFrame() ?? false;

            if (rightClicked || cancelPressed)
            {
                EndInspection();
            }
        }
    }
}
