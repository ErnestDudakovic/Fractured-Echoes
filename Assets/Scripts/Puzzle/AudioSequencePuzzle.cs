// ============================================================================
// AudioSequencePuzzle.cs — Audio-based "repeat the sequence" puzzle
// (Documentation §Stage 4: Audio-based puzzles / §8 Location 4 mechanic)
//
// Plays the puzzle's solution sequence as audio cues (one clip per input,
// with optional light flashes), then the player repeats it by interacting
// with PuzzleInteractable buttons. Input validation is handled by the
// existing PuzzleController state machine — this component only handles
// the audible/visual presentation of the sequence.
//
// Setup:
//   1. Add a PuzzleController with a PuzzleData whose solutionSequence
//      contains the input IDs (e.g. "tone_a", "tone_b", "tone_a").
//   2. Add this component next to it and define one SequenceCue per
//      input ID (clip + optional cue light).
//   3. Wire PuzzleInteractable buttons that send those input IDs.
//   4. Trigger PlaySequence() via a GameEventListener (e.g. from a
//      "Play recording" InteractableObject) or enable _playOnStart.
// ============================================================================

using System.Collections;
using UnityEngine;

namespace FracturedEchoes.Puzzle
{
    /// <summary>
    /// Companion component for <see cref="PuzzleController"/> that performs the
    /// audio playback of a Simon-says style sequence puzzle.
    /// </summary>
    [RequireComponent(typeof(PuzzleController))]
    public class AudioSequencePuzzle : MonoBehaviour
    {
        // =====================================================================
        // DATA
        // =====================================================================

        /// <summary>Maps a puzzle input ID to its audio/visual cue.</summary>
        [System.Serializable]
        public class SequenceCue
        {
            [Tooltip("The input ID this cue represents (must match PuzzleData.solutionSequence entries).")]
            public string inputID;

            [Tooltip("Audio clip played for this step.")]
            public AudioClip clip;

            [Tooltip("Optional light flashed while this step plays.")]
            public Light cueLight;

            [Tooltip("Intensity of the cue light while flashing.")]
            public float cueIntensity = 2f;
        }

        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Cues")]
        [Tooltip("One cue per distinct input ID used in the solution sequence.")]
        [SerializeField] private SequenceCue[] _cues;

        [Header("Playback")]
        [Tooltip("Seconds between sequence steps.")]
        [SerializeField] private float _stepInterval = 0.9f;

        [Tooltip("How long each cue (sound + light) lasts.")]
        [SerializeField] private float _cueDuration = 0.6f;

        [Tooltip("Delay before playback starts after being requested.")]
        [SerializeField] private float _startDelay = 0.75f;

        [Tooltip("Play the sequence automatically on scene start.")]
        [SerializeField] private bool _playOnStart = false;

        [Tooltip("Replay the sequence automatically when the puzzle resets after a mistake.")]
        [SerializeField] private bool _replayOnReset = true;

        [Header("Audio")]
        [Tooltip("Volume of sequence playback.")]
        [SerializeField, Range(0f, 1f)] private float _volume = 0.9f;

        // =====================================================================
        // RUNTIME STATE
        // =====================================================================

        private PuzzleController _controller;
        private AudioSource _audioSource;
        private Coroutine _playbackRoutine;

        /// <summary>True while the sequence is being played back.</summary>
        public bool IsPlaying => _playbackRoutine != null;

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
        }

        private void OnEnable()
        {
            if (_controller != null)
                _controller.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (_controller != null)
                _controller.OnStateChanged -= HandleStateChanged;

            StopPlayback();
        }

        private void Start()
        {
            if (_playOnStart)
                PlaySequence();
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Plays the solution sequence as audio/visual cues.
        /// Call from a GameEventListener, an interactable, or a trigger zone.
        /// </summary>
        public void PlaySequence()
        {
            if (IsPlaying) return;
            if (_controller == null || _controller.CurrentState == Core.Interfaces.PuzzleState.Completed)
                return;

            _playbackRoutine = StartCoroutine(PlaybackRoutine());
        }

        /// <summary>Stops the current playback immediately.</summary>
        public void StopPlayback()
        {
            if (_playbackRoutine != null)
            {
                StopCoroutine(_playbackRoutine);
                _playbackRoutine = null;
            }

            // Make sure no cue lights are left on
            if (_cues != null)
            {
                foreach (var cue in _cues)
                {
                    if (cue?.cueLight != null)
                        cue.cueLight.enabled = false;
                }
            }
        }

        /// <summary>
        /// Plays the single cue for one input ID (feedback when the player
        /// presses a button). Wire from PuzzleInteractable events if desired.
        /// </summary>
        public void PlayCue(string inputID)
        {
            SequenceCue cue = FindCue(inputID);
            if (cue != null)
                StartCoroutine(CueRoutine(cue));
        }

        // =====================================================================
        // COROUTINES
        // =====================================================================

        private IEnumerator PlaybackRoutine()
        {
            yield return new WaitForSeconds(_startDelay);

            string[] sequence = GetSolutionSequence();
            if (sequence == null)
            {
                _playbackRoutine = null;
                yield break;
            }

            foreach (string inputID in sequence)
            {
                SequenceCue cue = FindCue(inputID);
                if (cue != null)
                {
                    yield return CueRoutine(cue);
                }
                else
                {
                    Debug.LogWarning($"[AudioSequence] No cue defined for input ID: {inputID}", this);
                }

                yield return new WaitForSeconds(Mathf.Max(0f, _stepInterval - _cueDuration));
            }

            _playbackRoutine = null;
        }

        private IEnumerator CueRoutine(SequenceCue cue)
        {
            float originalIntensity = 0f;
            bool hadLight = cue.cueLight != null;

            if (hadLight)
            {
                originalIntensity = cue.cueLight.intensity;
                cue.cueLight.enabled = true;
                cue.cueLight.intensity = cue.cueIntensity;
            }

            if (cue.clip != null)
                _audioSource.PlayOneShot(cue.clip, _volume);

            yield return new WaitForSeconds(_cueDuration);

            if (hadLight)
            {
                cue.cueLight.intensity = originalIntensity;
                cue.cueLight.enabled = false;
            }
        }

        // =====================================================================
        // INTERNAL
        // =====================================================================

        private void HandleStateChanged(Core.Interfaces.PuzzleState newState)
        {
            // Replay the sequence when the puzzle resets after failure
            if (_replayOnReset && newState == Core.Interfaces.PuzzleState.Available && !IsPlaying)
            {
                PlaySequence();
            }

            // Stop playback if solved mid-play
            if (newState == Core.Interfaces.PuzzleState.Completed)
            {
                StopPlayback();
            }
        }

        private SequenceCue FindCue(string inputID)
        {
            if (_cues == null) return null;

            foreach (var cue in _cues)
            {
                if (cue != null && string.Equals(cue.inputID, inputID,
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    return cue;
                }
            }

            return null;
        }

        private string[] GetSolutionSequence()
        {
            return _controller != null && _controller.Data != null
                ? _controller.Data.solutionSequence
                : null;
        }
    }
}
