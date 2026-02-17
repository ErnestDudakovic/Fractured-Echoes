// ============================================================================
// SimpleLeverPuzzle.cs — Example derived puzzle: multi-lever sequence
// Demonstrates how to build a custom puzzle on top of PuzzleController.
// The player must pull N levers in the correct order to solve the puzzle.
// ============================================================================

using UnityEngine;
using FracturedEchoes.Core.Events;
using FracturedEchoes.Core.Interfaces;

namespace FracturedEchoes.Puzzle
{
    /// <summary>
    /// A concrete puzzle where the player must activate levers in the correct
    /// order. Each lever sends its ID to <see cref="PuzzleController.TryAdvance"/>.
    /// Attach this alongside a PuzzleController on the puzzle root object and
    /// wire each lever's PuzzleInteractable to this controller.
    /// </summary>
    [RequireComponent(typeof(PuzzleController))]
    public class SimpleLeverPuzzle : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Lever References")]
        [Tooltip("Transforms of the lever handles — rotated when pulled.")]
        [SerializeField] private Transform[] _leverHandles;

        [Header("Animation")]
        [Tooltip("Rotation angle when a lever is pulled (degrees around X).")]
        [SerializeField] private float _pullAngle = 45f;

        [Tooltip("Speed of lever rotation animation.")]
        [SerializeField] private float _animSpeed = 4f;

        [Header("Audio")]
        [Tooltip("Sound played when a lever is pulled.")]
        [SerializeField] private AudioClip _leverPullSound;

        [Tooltip("Sound played when all levers reset after a mistake.")]
        [SerializeField] private AudioClip _resetSound;

        [Header("Events")]
        [Tooltip("Raised when the lever puzzle is solved.")]
        [SerializeField] private GameEvent _onSolved;

        // =====================================================================
        // RUNTIME STATE
        // =====================================================================

        private PuzzleController _controller;
        private AudioSource _audioSource;
        private bool[] _leverStates;           // true = pulled
        private Quaternion[] _defaultRotations; // stored on Awake

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            _controller = GetComponent<PuzzleController>();

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f;
            }

            // Cache default rotations
            if (_leverHandles != null)
            {
                _leverStates = new bool[_leverHandles.Length];
                _defaultRotations = new Quaternion[_leverHandles.Length];

                for (int i = 0; i < _leverHandles.Length; i++)
                {
                    if (_leverHandles[i] != null)
                    {
                        _defaultRotations[i] = _leverHandles[i].localRotation;
                    }
                }
            }
        }

        private void OnEnable()
        {
            // Subscribe to puzzle events
            if (_controller != null)
            {
                _controller.OnStepCompleted += HandleStepCompleted;
                _controller.OnPuzzleCompleted += HandlePuzzleCompleted;
                _controller.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                _controller.OnStepCompleted -= HandleStepCompleted;
                _controller.OnPuzzleCompleted -= HandlePuzzleCompleted;
                _controller.OnStateChanged -= HandleStateChanged;
            }
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private void HandleStepCompleted(int stepIndex)
        {
            // Animate the lever that was just correctly pulled
            int leverIndex = stepIndex - 1; // stepIndex is 1-based after increment
            if (leverIndex >= 0 && leverIndex < _leverHandles.Length)
            {
                PullLever(leverIndex);
            }
        }

        private void HandlePuzzleCompleted()
        {
            _onSolved?.Raise();
            Debug.Log("[LeverPuzzle] Solved!");
        }

        private void HandleStateChanged(PuzzleState newState)
        {
            // Reset all levers when the puzzle resets
            if (newState == PuzzleState.Available)
            {
                ResetAllLevers();
            }
        }

        // =====================================================================
        // LEVER ANIMATION
        // =====================================================================

        /// <summary>
        /// Visually pulls a lever (rotates the handle transform).
        /// </summary>
        private void PullLever(int index)
        {
            if (_leverHandles == null || index >= _leverHandles.Length) return;
            if (_leverHandles[index] == null) return;

            _leverStates[index] = true;

            // Play pull sound
            if (_leverPullSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_leverPullSound);
            }

            // Start rotation coroutine
            StartCoroutine(AnimateLever(_leverHandles[index], _pullAngle));
        }

        /// <summary>
        /// Resets all levers to their default rotation.
        /// </summary>
        private void ResetAllLevers()
        {
            if (_leverHandles == null) return;

            if (_resetSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_resetSound);
            }

            for (int i = 0; i < _leverHandles.Length; i++)
            {
                if (_leverHandles[i] != null && _leverStates[i])
                {
                    _leverStates[i] = false;
                    StartCoroutine(AnimateLeverToRotation(_leverHandles[i], _defaultRotations[i]));
                }
            }
        }

        // =====================================================================
        // COROUTINES
        // =====================================================================

        private System.Collections.IEnumerator AnimateLever(Transform handle, float angle)
        {
            Quaternion startRot = handle.localRotation;
            Quaternion endRot = startRot * Quaternion.Euler(angle, 0f, 0f);
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * _animSpeed;
                handle.localRotation = Quaternion.Slerp(startRot, endRot, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            handle.localRotation = endRot;
        }

        private System.Collections.IEnumerator AnimateLeverToRotation(Transform handle, Quaternion targetRot)
        {
            Quaternion startRot = handle.localRotation;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * _animSpeed;
                handle.localRotation = Quaternion.Slerp(startRot, targetRot, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            handle.localRotation = targetRot;
        }
    }
}
