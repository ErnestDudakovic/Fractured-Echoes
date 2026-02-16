// ============================================================================
// GameStateManager.cs — Central game state orchestrator
// Tracks global game state: current location, puzzle progress,
// environment phase, triggered events, and inventory state.
// Acts as the top-level coordinator without tightly coupling systems.
// Uses events and interfaces — NOT a static singleton.
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using FracturedEchoes.Core.Events;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.Core.SaveLoad;

namespace FracturedEchoes.Core
{
    /// <summary>
    /// Central game state manager. Tracks high-level progression and coordinates
    /// between systems via events. This is NOT a singleton — it lives on a
    /// dedicated GameObject and is referenced via Inspector wiring or FindObjectOfType.
    /// </summary>
    public class GameStateManager : MonoBehaviour, ISaveable
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Game Configuration")]
        [Tooltip("Total number of locations in the game.")]
        [SerializeField] private int _totalLocations = 5;

        [Tooltip("Scene names for each location (in order).")]
        [SerializeField] private string[] _locationSceneNames;

        [Header("System References")]
        [Tooltip("Reference to the save system.")]
        [SerializeField] private SaveSystem _saveSystem;

        [Header("Events")]
        [Tooltip("Raised when the game state changes significantly.")]
        [SerializeField] private GameEvent _onGameStateChanged;

        [Tooltip("Raised when a location transition begins.")]
        [SerializeField] private GameEventString _onLocationTransition;

        [Tooltip("Raised when the game is paused/unpaused.")]
        [SerializeField] private GameEvent _onPauseToggled;

        [Header("Save")]
        [SerializeField] private string _saveID = "game_state";

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private GameState _currentState = GameState.Playing;
        private int _currentLocationIndex = 0;
        private HashSet<string> _completedPuzzles = new HashSet<string>();
        private HashSet<string> _triggeredEvents = new HashSet<string>();
        private Dictionary<int, int> _locationPhases = new Dictionary<int, int>();
        private float _totalPlayTime;
        private bool _isPaused;

        // =====================================================================
        // PROPERTIES
        // =====================================================================

        public string SaveID => _saveID;
        public GameState CurrentState => _currentState;
        public int CurrentLocationIndex => _currentLocationIndex;
        public string CurrentLocationName =>
            _locationSceneNames != null && _currentLocationIndex < _locationSceneNames.Length
                ? _locationSceneNames[_currentLocationIndex]
                : "Unknown";
        public bool IsPaused => _isPaused;
        public float TotalPlayTime => _totalPlayTime;
        public int CompletedPuzzleCount => _completedPuzzles.Count;

        // =====================================================================
        // C# EVENTS
        // =====================================================================

        public event Action<GameState> StateChanged;
        public event Action<int> LocationChanged;
        public event Action<string> PuzzleCompleted;
        public event Action<bool> PauseStateChanged;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Update()
        {
            if (_currentState == GameState.Playing && !_isPaused)
            {
                _totalPlayTime += Time.unscaledDeltaTime;
            }

            // Pause toggle
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }

            // Quick save / Quick load
            if (Input.GetKeyDown(KeyCode.F5) && _saveSystem != null)
            {
                _saveSystem.SaveToSlot(0);
            }

            if (Input.GetKeyDown(KeyCode.F9) && _saveSystem != null)
            {
                _saveSystem.LoadFromSlot(0);
            }
        }

        // =====================================================================
        // GAME STATE CONTROL
        // =====================================================================

        /// <summary>
        /// Changes the current game state.
        /// </summary>
        public void SetGameState(GameState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            StateChanged?.Invoke(newState);
            _onGameStateChanged?.Raise();

            Debug.Log($"[GameState] State changed to: {newState}");
        }

        /// <summary>
        /// Toggles pause state.
        /// </summary>
        public void TogglePause()
        {
            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0f : 1f;

            // Lock/unlock cursor
            Cursor.lockState = _isPaused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = _isPaused;

            PauseStateChanged?.Invoke(_isPaused);
            _onPauseToggled?.Raise();

            Debug.Log($"[GameState] Paused: {_isPaused}");
        }

        // =====================================================================
        // LOCATION MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Transitions to the next location.
        /// </summary>
        public void AdvanceToNextLocation()
        {
            if (_currentLocationIndex < _totalLocations - 1)
            {
                TransitionToLocation(_currentLocationIndex + 1);
            }
            else
            {
                Debug.Log("[GameState] Already at final location.");
                SetGameState(GameState.GameComplete);
            }
        }

        /// <summary>
        /// Transitions to a specific location by index.
        /// </summary>
        public void TransitionToLocation(int locationIndex)
        {
            if (locationIndex < 0 || locationIndex >= _totalLocations)
            {
                Debug.LogWarning($"[GameState] Invalid location index: {locationIndex}");
                return;
            }

            _currentLocationIndex = locationIndex;
            string sceneName = CurrentLocationName;

            LocationChanged?.Invoke(locationIndex);
            _onLocationTransition?.Raise(sceneName);

            Debug.Log($"[GameState] Transitioning to location {locationIndex}: {sceneName}");

            // Load the scene (async recommended for production)
            // UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        // =====================================================================
        // PUZZLE TRACKING
        // =====================================================================

        /// <summary>
        /// Marks a puzzle as completed. Called by puzzle controllers via events.
        /// </summary>
        public void MarkPuzzleCompleted(string puzzleID)
        {
            if (_completedPuzzles.Add(puzzleID))
            {
                PuzzleCompleted?.Invoke(puzzleID);
                _onGameStateChanged?.Raise();

                Debug.Log($"[GameState] Puzzle completed: {puzzleID} (Total: {_completedPuzzles.Count})");
            }
        }

        /// <summary>
        /// Checks if a specific puzzle has been completed.
        /// </summary>
        public bool IsPuzzleCompleted(string puzzleID)
        {
            return _completedPuzzles.Contains(puzzleID);
        }

        // =====================================================================
        // EVENT TRACKING
        // =====================================================================

        /// <summary>
        /// Records that a scripted event has been triggered.
        /// </summary>
        public void MarkEventTriggered(string eventID)
        {
            _triggeredEvents.Add(eventID);
        }

        /// <summary>
        /// Checks if a scripted event has been triggered.
        /// </summary>
        public bool IsEventTriggered(string eventID)
        {
            return _triggeredEvents.Contains(eventID);
        }

        // =====================================================================
        // PHASE TRACKING
        // =====================================================================

        /// <summary>
        /// Records the environment phase for a location.
        /// </summary>
        public void SetLocationPhase(int locationIndex, int phase)
        {
            _locationPhases[locationIndex] = phase;
        }

        /// <summary>
        /// Gets the environment phase for a location.
        /// </summary>
        public int GetLocationPhase(int locationIndex)
        {
            return _locationPhases.TryGetValue(locationIndex, out int phase) ? phase : 0;
        }

        // =====================================================================
        // ISaveable IMPLEMENTATION
        // =====================================================================

        public object CaptureState()
        {
            return new GameStateSaveData
            {
                currentState = _currentState,
                currentLocationIndex = _currentLocationIndex,
                completedPuzzles = new List<string>(_completedPuzzles),
                triggeredEvents = new List<string>(_triggeredEvents),
                totalPlayTime = _totalPlayTime
            };
        }

        public void RestoreState(object state)
        {
            if (state is GameStateSaveData saveData)
            {
                _currentState = saveData.currentState;
                _currentLocationIndex = saveData.currentLocationIndex;
                _completedPuzzles = new HashSet<string>(saveData.completedPuzzles);
                _triggeredEvents = new HashSet<string>(saveData.triggeredEvents);
                _totalPlayTime = saveData.totalPlayTime;

                StateChanged?.Invoke(_currentState);
                _onGameStateChanged?.Raise();
            }
        }
    }

    // =========================================================================
    // ENUMS & DATA STRUCTURES
    // =========================================================================

    /// <summary>
    /// High-level game states.
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Cutscene,
        Loading,
        GameComplete
    }

    /// <summary>
    /// Serializable save data for the game state.
    /// </summary>
    [Serializable]
    public class GameStateSaveData
    {
        public GameState currentState;
        public int currentLocationIndex;
        public List<string> completedPuzzles;
        public List<string> triggeredEvents;
        public float totalPlayTime;
    }
}
