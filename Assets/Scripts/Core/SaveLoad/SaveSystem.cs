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
        // PENDING LOAD (for loading from Main Menu)
        // =====================================================================

        /// <summary>
        /// When set to >= 0, the SaveSystem will load this slot after scene load.
        /// Set by SaveSlotUI when loading from the main menu.
        /// </summary>
        public static int PendingLoadSlot { get; set; } = -1;

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

        private void Start()
        {
            // If a pending load was queued (from main menu), apply it now
            if (PendingLoadSlot >= 0)
            {
                int slot = PendingLoadSlot;
                PendingLoadSlot = -1;
                Debug.Log($"[Save] Applying pending load for slot {slot}");
                LoadFromSlot(slot);
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

        /// <summary>
        /// Returns the current save data as a serialized JSON string.
        /// Useful for cloud save integration (Firebase, etc.).
        /// </summary>
        public string GetCurrentSaveDataJson()
        {
            SaveData data = CollectSaveData();
            return JsonUtility.ToJson(data, true);
        }

        /// <summary>
        /// Loads save data from a JSON string (e.g. downloaded from cloud).
        /// </summary>
        public void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[Save] Cannot load from empty JSON.");
                return;
            }

            try
            {
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);
                DistributeSaveData(saveData);
                Debug.Log($"[Save] Loaded from JSON: {saveData.entries.Count} entries");
                _onLoadCompleted?.Raise();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Save] Failed to load from JSON: {ex.Message}");
                _onSaveFailed?.Raise();
            }
        }

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
            ISaveable[] saveables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<ISaveable>().ToArray();

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
            ISaveable[] saveables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<ISaveable>().ToArray();

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
}
