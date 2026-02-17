// ============================================================================
// CloudSaveManager.cs — Firebase Firestore cloud save/load
// Saves game data to Firestore under: saves/{UserID}/slots/{slotIndex}
// Works alongside the local SaveSystem — cloud is an additional backup.
// Player can save locally AND in the cloud simultaneously.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

namespace FracturedEchoes.Core.SaveLoad
{
    /// <summary>
    /// Cloud save manager using Firebase Firestore.
    /// Stores save data as JSON strings in Firestore documents.
    /// Each save slot is a separate document under the player's UserID.
    /// 
    /// Firestore structure:
    ///   saves/{UserID}/slots/slot_0  → { json: "...", timestamp: "...", location: "..." }
    ///   saves/{UserID}/slots/slot_1  → { ... }
    ///   saves/{UserID}/slots/slot_2  → { ... }
    /// </summary>
    public class CloudSaveManager : MonoBehaviour
    {
        // =====================================================================
        // SINGLETON
        // =====================================================================

        public static CloudSaveManager Instance { get; private set; }

        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Configuration")]
        [Tooltip("Reference to the local SaveSystem for collecting save data.")]
        [SerializeField] private SaveSystem _saveSystem;

        [Tooltip("Number of cloud save slots.")]
        [SerializeField] private int _maxCloudSlots = 3;

        // =====================================================================
        // STATE
        // =====================================================================

        private FirebaseFirestore _db;
        private bool _isReady;
        private bool _isBusy;

        /// <summary>True once Firestore is initialized and user is authenticated.</summary>
        public bool IsReady => _isReady;

        /// <summary>True while a cloud operation is in progress.</summary>
        public bool IsBusy => _isBusy;

        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Raised when a cloud save completes successfully.</summary>
        public event Action<int> OnCloudSaveCompleted;

        /// <summary>Raised when a cloud load completes successfully.</summary>
        public event Action<int> OnCloudLoadCompleted;

        /// <summary>Raised when a cloud operation fails.</summary>
        public event Action<string> OnCloudError;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            FirebaseAuthManager.OnSignedIn += HandleSignedIn;

            // If already signed in
            if (FirebaseAuthManager.Instance != null && FirebaseAuthManager.Instance.IsSignedIn)
                HandleSignedIn(FirebaseAuthManager.Instance.UserID);
        }

        private void OnDisable()
        {
            FirebaseAuthManager.OnSignedIn -= HandleSignedIn;
        }

        private void HandleSignedIn(string userId)
        {
            _db = FirebaseFirestore.DefaultInstance;
            _isReady = true;
            Debug.Log($"[CloudSave] Ready. UserID: {userId}");
        }

        // =====================================================================
        // SAVE TO CLOUD
        // =====================================================================

        /// <summary>
        /// Saves the current game state to a cloud slot.
        /// Collects data from SaveSystem and uploads to Firestore.
        /// </summary>
        public void SaveToCloud(int slotIndex)
        {
            if (!CanOperate(slotIndex)) return;

            _isBusy = true;
            string userId = FirebaseAuthManager.Instance.UserID;
            string json = _saveSystem.GetCurrentSaveDataJson();

            // Build the Firestore document data
            var docData = new Dictionary<string, object>
            {
                { "json", json },
                { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") },
                { "location", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name },
                { "playTime", Time.time },
                { "version", Application.version }
            };

            // Path: saves/{UserID}/slots/slot_0
            DocumentReference docRef = _db
                .Collection("saves")
                .Document(userId)
                .Collection("slots")
                .Document($"slot_{slotIndex}");

            docRef.SetAsync(docData).ContinueWithOnMainThread(task =>
            {
                _isBusy = false;

                if (task.IsFaulted)
                {
                    string error = task.Exception?.Flatten().InnerException?.Message ?? "Unknown error";
                    Debug.LogError($"[CloudSave] Save failed: {error}");
                    OnCloudError?.Invoke(error);
                    return;
                }

                Debug.Log($"[CloudSave] Saved to cloud slot {slotIndex}.");
                OnCloudSaveCompleted?.Invoke(slotIndex);
            });
        }

        // =====================================================================
        // LOAD FROM CLOUD
        // =====================================================================

        /// <summary>
        /// Loads game state from a cloud slot and applies it via SaveSystem.
        /// </summary>
        public void LoadFromCloud(int slotIndex)
        {
            if (!CanOperate(slotIndex)) return;

            _isBusy = true;
            string userId = FirebaseAuthManager.Instance.UserID;

            DocumentReference docRef = _db
                .Collection("saves")
                .Document(userId)
                .Collection("slots")
                .Document($"slot_{slotIndex}");

            docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                _isBusy = false;

                if (task.IsFaulted)
                {
                    string error = task.Exception?.Flatten().InnerException?.Message ?? "Unknown error";
                    Debug.LogError($"[CloudSave] Load failed: {error}");
                    OnCloudError?.Invoke(error);
                    return;
                }

                DocumentSnapshot snapshot = task.Result;

                if (!snapshot.Exists)
                {
                    Debug.LogWarning($"[CloudSave] No save found in cloud slot {slotIndex}.");
                    OnCloudError?.Invoke($"No save in slot {slotIndex}");
                    return;
                }

                // Extract the JSON string from the document
                string json = snapshot.GetValue<string>("json");

                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning("[CloudSave] Save data is empty.");
                    OnCloudError?.Invoke("Empty save data");
                    return;
                }

                // Apply the save data via SaveSystem
                _saveSystem.LoadFromJson(json);

                string timestamp = snapshot.GetValue<string>("timestamp");
                Debug.Log($"[CloudSave] Loaded cloud slot {slotIndex} (saved: {timestamp}).");
                OnCloudLoadCompleted?.Invoke(slotIndex);
            });
        }

        // =====================================================================
        // DELETE CLOUD SAVE
        // =====================================================================

        /// <summary>
        /// Deletes a cloud save slot.
        /// </summary>
        public void DeleteCloudSave(int slotIndex)
        {
            if (!CanOperate(slotIndex)) return;

            _isBusy = true;
            string userId = FirebaseAuthManager.Instance.UserID;

            DocumentReference docRef = _db
                .Collection("saves")
                .Document(userId)
                .Collection("slots")
                .Document($"slot_{slotIndex}");

            docRef.DeleteAsync().ContinueWithOnMainThread(task =>
            {
                _isBusy = false;

                if (task.IsFaulted)
                {
                    string error = task.Exception?.Flatten().InnerException?.Message ?? "Unknown error";
                    Debug.LogError($"[CloudSave] Delete failed: {error}");
                    OnCloudError?.Invoke(error);
                    return;
                }

                Debug.Log($"[CloudSave] Deleted cloud slot {slotIndex}.");
            });
        }

        // =====================================================================
        // CLOUD SLOT INFO
        // =====================================================================

        /// <summary>
        /// Checks if a cloud save slot exists and returns its metadata.
        /// Calls the callback with (exists, timestamp, location).
        /// </summary>
        public void GetCloudSlotInfo(int slotIndex, Action<bool, string, string> callback)
        {
            if (!CanOperate(slotIndex))
            {
                callback?.Invoke(false, "", "");
                return;
            }

            string userId = FirebaseAuthManager.Instance.UserID;

            DocumentReference docRef = _db
                .Collection("saves")
                .Document(userId)
                .Collection("slots")
                .Document($"slot_{slotIndex}");

            docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || !task.Result.Exists)
                {
                    callback?.Invoke(false, "", "");
                    return;
                }

                DocumentSnapshot snapshot = task.Result;
                string timestamp = snapshot.GetValue<string>("timestamp");
                string location = snapshot.GetValue<string>("location");
                callback?.Invoke(true, timestamp, location);
            });
        }

        /// <summary>
        /// Lists all cloud save slots for the current user.
        /// Calls the callback with a list of (slotIndex, timestamp, location).
        /// </summary>
        public void ListCloudSaves(Action<List<CloudSlotInfo>> callback)
        {
            if (!_isReady || !FirebaseAuthManager.Instance.IsSignedIn)
            {
                callback?.Invoke(new List<CloudSlotInfo>());
                return;
            }

            string userId = FirebaseAuthManager.Instance.UserID;

            _db.Collection("saves")
                .Document(userId)
                .Collection("slots")
                .GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    var results = new List<CloudSlotInfo>();

                    if (task.IsFaulted)
                    {
                        Debug.LogError($"[CloudSave] List failed: {task.Exception?.Message}");
                        callback?.Invoke(results);
                        return;
                    }

                    foreach (DocumentSnapshot doc in task.Result.Documents)
                    {
                        // Parse slot index from document ID (e.g. "slot_0" → 0)
                        string docId = doc.Id;
                        int slotIdx = -1;
                        if (docId.StartsWith("slot_"))
                            int.TryParse(docId.Substring(5), out slotIdx);

                        results.Add(new CloudSlotInfo
                        {
                            slotIndex = slotIdx,
                            timestamp = doc.GetValue<string>("timestamp"),
                            location = doc.GetValue<string>("location"),
                            version = doc.ContainsField("version") ? doc.GetValue<string>("version") : "?"
                        });
                    }

                    Debug.Log($"[CloudSave] Found {results.Count} cloud saves.");
                    callback?.Invoke(results);
                });
        }

        // =====================================================================
        // SYNC: Upload local save to cloud
        // =====================================================================

        /// <summary>
        /// Reads a local save file and uploads it to the corresponding cloud slot.
        /// Useful for "sync to cloud" feature.
        /// </summary>
        public void SyncLocalToCloud(int slotIndex)
        {
            if (_saveSystem == null)
            {
                Debug.LogError("[CloudSave] SaveSystem reference is null.");
                return;
            }

            // Check if local save exists
            if (!_saveSystem.SaveExists(slotIndex))
            {
                Debug.LogWarning($"[CloudSave] No local save in slot {slotIndex} to sync.");
                return;
            }

            // The SaveSystem.SaveToSlot saves to disk, then we can read and upload
            // But we want to upload the CURRENT save data, not the disk version
            SaveToCloud(slotIndex);
        }

        // =====================================================================
        // INTERNAL
        // =====================================================================

        private bool CanOperate(int slotIndex)
        {
            if (!_isReady)
            {
                Debug.LogWarning("[CloudSave] Not ready yet (Firebase not initialized).");
                return false;
            }

            if (!FirebaseAuthManager.Instance.IsSignedIn)
            {
                Debug.LogWarning("[CloudSave] Not signed in.");
                return false;
            }

            if (slotIndex < 0 || slotIndex >= _maxCloudSlots)
            {
                Debug.LogError($"[CloudSave] Invalid slot index: {slotIndex}");
                return false;
            }

            if (_isBusy)
            {
                Debug.LogWarning("[CloudSave] Operation already in progress.");
                return false;
            }

            return true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    // =========================================================================
    // DATA CLASS
    // =========================================================================

    /// <summary>
    /// Metadata about a cloud save slot.
    /// </summary>
    public class CloudSlotInfo
    {
        public int slotIndex;
        public string timestamp;
        public string location;
        public string version;
    }
}
