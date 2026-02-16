// ============================================================================
// SaveSystem.cs — JSON-based save/load system
// Collects state from all ISaveable components, serializes to JSON,
// and persists to disk. Supports multiple save slots.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.Core.Events;

namespace FracturedEchoes.Core.SaveLoad
{
    /// <summary>
    /// JSON-based save system. Collects state from all ISaveable components
    /// in the scene, serializes to JSON, and saves to persistent storage.
    /// Supports multiple save slots and auto-save functionality.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Configuration")]
        [Tooltip("Number of available save slots.")]
        [SerializeField] private int _maxSaveSlots = 3;

        [Tooltip("Subfolder name within persistentDataPath.")]
        [SerializeField] private string _saveFolderName = "SaveData";

        [Tooltip("File extension for save files.")]
        [SerializeField] private string _fileExtension = ".json";

        [Header("Auto-Save")]
        [Tooltip("Whether auto-save is enabled.")]
        [SerializeField] private bool _autoSaveEnabled = true;

        [Tooltip("Auto-save interval in seconds.")]
        [SerializeField] private float _autoSaveInterval = 300f;

        [Header("Events")]
        [SerializeField] private GameEvent _onSaveStarted;
        [SerializeField] private GameEvent _onSaveCompleted;
        [SerializeField] private GameEvent _onLoadStarted;
        [SerializeField] private GameEvent _onLoadCompleted;
        [SerializeField] private GameEvent _onSaveFailed;

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private float _autoSaveTimer;
        private string _saveFolderPath;
        private bool _isSaving;
        private bool _isLoading;

        // =====================================================================
        // PROPERTIES
        // =====================================================================

        /// <summary>
        /// The full path to the save folder.
        /// </summary>
        public string SaveFolderPath => _saveFolderPath;

        /// <summary>
        /// Whether a save or load operation is in progress.
        /// </summary>
        public bool IsBusy => _isSaving || _isLoading;

        // =====================================================================
        // C# EVENTS
        // =====================================================================

        public event Action<int> SaveCompleted;
        public event Action<int> LoadCompleted;
        public event Action<string> SaveError;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            _saveFolderPath = Path.Combine(Application.persistentDataPath, _saveFolderName);

            // Ensure save directory exists
            if (!Directory.Exists(_saveFolderPath))
            {
                Directory.CreateDirectory(_saveFolderPath);
            }
        }

        private void Update()
        {
            // Auto-save timer
            if (_autoSaveEnabled && !IsBusy)
            {
                _autoSaveTimer += Time.deltaTime;
                if (_autoSaveTimer >= _autoSaveInterval)
                {
                    _autoSaveTimer = 0f;
                    SaveToSlot(0); // Auto-save always uses slot 0
                    Debug.Log("[Save] Auto-save triggered.");
                }
            }
        }

        // =====================================================================
        // SAVE
        // =====================================================================

        /// <summary>
        /// Saves the current game state to the specified slot.
        /// Collects state from all ISaveable components in the scene.
        /// </summary>
        public void SaveToSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _maxSaveSlots)
            {
                Debug.LogError($"[Save] Invalid slot index: {slotIndex}");
                return;
            }

            if (_isSaving)
            {
                Debug.LogWarning("[Save] Save already in progress.");
                return;
            }

            _isSaving = true;
            _onSaveStarted?.Raise();

            try
            {
                // Collect all saveable state
                SaveData saveData = CollectSaveData();

                // Serialize to JSON
                string json = JsonUtility.ToJson(saveData, true);

                // Write to file
                string filePath = GetSaveFilePath(slotIndex);
                File.WriteAllText(filePath, json);

                Debug.Log($"[Save] Saved to slot {slotIndex}: {filePath}");
                Debug.Log($"[Save] Data: {saveData.entries.Count} entries, {json.Length} chars");

                _onSaveCompleted?.Raise();
                SaveCompleted?.Invoke(slotIndex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Save] Save failed: {ex.Message}");
                _onSaveFailed?.Raise();
                SaveError?.Invoke(ex.Message);
            }
            finally
            {
                _isSaving = false;
            }
        }

        // =====================================================================
        // LOAD
        // =====================================================================

        /// <summary>
        /// Loads game state from the specified slot.
        /// Distributes state to all ISaveable components in the scene.
        /// </summary>
        public void LoadFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _maxSaveSlots)
            {
                Debug.LogError($"[Save] Invalid slot index: {slotIndex}");
                return;
            }

            string filePath = GetSaveFilePath(slotIndex);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[Save] No save file found at slot {slotIndex}");
                return;
            }

            if (_isLoading)
            {
                Debug.LogWarning("[Save] Load already in progress.");
                return;
            }

            _isLoading = true;
            _onLoadStarted?.Raise();

            try
            {
                // Read JSON from file
                string json = File.ReadAllText(filePath);

                // Deserialize
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                // Distribute state to saveables
                DistributeSaveData(saveData);

                Debug.Log($"[Save] Loaded from slot {slotIndex}: {saveData.entries.Count} entries");

                _onLoadCompleted?.Raise();
                LoadCompleted?.Invoke(slotIndex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Save] Load failed: {ex.Message}");
                _onSaveFailed?.Raise();
                SaveError?.Invoke(ex.Message);
            }
            finally
            {
                _isLoading = false;
            }
        }

        // =====================================================================
        // SLOT MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Checks if a save exists in the specified slot.
        /// </summary>
        public bool SaveExists(int slotIndex)
        {
            return File.Exists(GetSaveFilePath(slotIndex));
        }

        /// <summary>
        /// Deletes the save in the specified slot.
        /// </summary>
        public void DeleteSave(int slotIndex)
        {
            string filePath = GetSaveFilePath(slotIndex);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"[Save] Deleted save at slot {slotIndex}");
            }
        }

        /// <summary>
        /// Gets metadata for a save slot (timestamp, location, etc.).
        /// </summary>
        public SaveSlotInfo GetSlotInfo(int slotIndex)
        {
            string filePath = GetSaveFilePath(slotIndex);
            if (!File.Exists(filePath)) return null;

            try
            {
                string json = File.ReadAllText(filePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                return new SaveSlotInfo
                {
                    slotIndex = slotIndex,
                    timestamp = data.timestamp,
                    locationName = data.currentLocation,
                    playTime = data.playTime
                };
            }
            catch
            {
                return null;
            }
        }

        // =====================================================================
        // INTERNAL METHODS
        // =====================================================================

        private SaveData CollectSaveData()
        {
            SaveData data = new SaveData
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                currentLocation = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                playTime = Time.time,
                entries = new List<SaveEntry>()
            };

            // Find all ISaveable components in the scene
            ISaveable[] saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();

            foreach (ISaveable saveable in saveables)
            {
                try
                {
                    object state = saveable.CaptureState();
                    string stateJson = JsonUtility.ToJson(state);

                    data.entries.Add(new SaveEntry
                    {
                        saveID = saveable.SaveID,
                        stateJson = stateJson,
                        typeName = state.GetType().AssemblyQualifiedName
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Save] Failed to capture state for {saveable.SaveID}: {ex.Message}");
                }
            }

            return data;
        }

        private void DistributeSaveData(SaveData data)
        {
            // Build a dictionary of saved states
            Dictionary<string, SaveEntry> stateLookup = new Dictionary<string, SaveEntry>();
            foreach (SaveEntry entry in data.entries)
            {
                stateLookup[entry.saveID] = entry;
            }

            // Find all ISaveable components and restore their state
            ISaveable[] saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();

            foreach (ISaveable saveable in saveables)
            {
                if (stateLookup.TryGetValue(saveable.SaveID, out SaveEntry entry))
                {
                    try
                    {
                        Type stateType = Type.GetType(entry.typeName);
                        if (stateType != null)
                        {
                            object state = JsonUtility.FromJson(entry.stateJson, stateType);
                            saveable.RestoreState(state);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Save] Failed to restore state for {saveable.SaveID}: {ex.Message}");
                    }
                }
            }
        }

        private string GetSaveFilePath(int slotIndex)
        {
            return Path.Combine(_saveFolderPath, $"save_slot_{slotIndex}{_fileExtension}");
        }
    }

    // =========================================================================
    // DATA STRUCTURES
    // =========================================================================

    /// <summary>
    /// Root save data container. Serialized to/from JSON.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public string timestamp;
        public string currentLocation;
        public float playTime;
        public List<SaveEntry> entries;
    }

    /// <summary>
    /// A single save entry for one ISaveable component.
    /// </summary>
    [Serializable]
    public class SaveEntry
    {
        public string saveID;
        public string stateJson;
        public string typeName;
    }

    /// <summary>
    /// Metadata about a save slot for the UI.
    /// </summary>
    public class SaveSlotInfo
    {
        public int slotIndex;
        public string timestamp;
        public string locationName;
        public float playTime;
    }
}
