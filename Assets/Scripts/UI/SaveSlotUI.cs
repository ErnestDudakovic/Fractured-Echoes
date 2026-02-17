// ============================================================================
// SaveSlotUI.cs — Load Game panel: displays save slots with metadata
// Creates one button per save slot showing timestamp, location, and playtime.
// Allows the player to pick a slot to load or delete it.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using FracturedEchoes.Core.SaveLoad;

namespace FracturedEchoes.UI
{
    /// <summary>
    /// Manages the "Load Game" sub-panel.  Dynamically populates save slot
    /// entries from <see cref="SaveSystem"/>.  Attach to the Load Game panel
    /// GameObject.
    /// </summary>
    public class SaveSlotUI : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Save System")]
        [Tooltip("Reference to the SaveSystem (can be on a DontDestroyOnLoad object).")]
        [SerializeField] private SaveSystem _saveSystem;

        [Header("Slot Container")]
        [Tooltip("Parent transform for instantiated slot entries.")]
        [SerializeField] private Transform _slotContainer;

        [Tooltip("Prefab for a single save slot row.")]
        [SerializeField] private GameObject _slotPrefab;

        [Header("Slot Prefab Children (by name)")]
        [Tooltip("Name of the child TextMeshProUGUI showing slot number.")]
        [SerializeField] private string _slotLabelName = "SlotLabel";
        [Tooltip("Name of the child TextMeshProUGUI showing save info.")]
        [SerializeField] private string _infoLabelName = "InfoLabel";
        [Tooltip("Name of the child Button for loading.")]
        [SerializeField] private string _loadButtonName = "LoadButton";
        [Tooltip("Name of the child Button for deleting.")]
        [SerializeField] private string _deleteButtonName = "DeleteButton";

        [Header("Empty Slot Text")]
        [SerializeField] private string _emptySlotText = "— Empty —";

        [Header("Max Slots")]
        [SerializeField] private int _maxSlots = 3;

        [Header("Main Menu Load")]
        [Tooltip("Scene to load when player picks a save from the main menu.")]
        [SerializeField] private string _gameSceneName = "TestRoom";

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void OnEnable()
        {
            RefreshSlots();
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>Rebuilds the slot list from disk.</summary>
        public void RefreshSlots()
        {
            // Clear existing children
            if (_slotContainer != null)
            {
                for (int i = _slotContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(_slotContainer.GetChild(i).gameObject);
                }
            }

            if (_slotPrefab == null || _slotContainer == null) return;

            for (int i = 0; i < _maxSlots; i++)
            {
                CreateSlotEntry(i);
            }
        }

        // =====================================================================
        // SLOT CREATION
        // =====================================================================

        private void CreateSlotEntry(int slotIndex)
        {
            GameObject entry = Instantiate(_slotPrefab, _slotContainer);
            entry.name = $"SaveSlot_{slotIndex}";

            // Find child components by name
            TextMeshProUGUI slotLabel = FindChildTMP(entry.transform, _slotLabelName);
            TextMeshProUGUI infoLabel = FindChildTMP(entry.transform, _infoLabelName);
            Button loadButton = FindChildButton(entry.transform, _loadButtonName);
            Button deleteButton = FindChildButton(entry.transform, _deleteButtonName);

            // Set slot number
            if (slotLabel != null)
            {
                slotLabel.text = $"Slot {slotIndex + 1}";
            }

            // Check if save exists
            bool exists = _saveSystem != null && _saveSystem.SaveExists(slotIndex);

            if (exists && _saveSystem != null)
            {
                SaveSlotInfo info = _saveSystem.GetSlotInfo(slotIndex);

                if (infoLabel != null && info != null)
                {
                    string playTime = FormatPlayTime(info.playTime);
                    infoLabel.text = $"{info.locationName}\n{info.timestamp}\nPlay time: {playTime}";
                }

                // Wire load button
                if (loadButton != null)
                {
                    int capturedIndex = slotIndex;
                    loadButton.interactable = true;
                    loadButton.onClick.AddListener(() => OnLoadSlot(capturedIndex));
                }

                // Wire delete button
                if (deleteButton != null)
                {
                    int capturedIndex = slotIndex;
                    deleteButton.interactable = true;
                    deleteButton.onClick.AddListener(() => OnDeleteSlot(capturedIndex));
                }
            }
            else
            {
                // Empty slot
                if (infoLabel != null)
                {
                    infoLabel.text = _emptySlotText;
                }

                if (loadButton != null) loadButton.interactable = false;
                if (deleteButton != null) deleteButton.interactable = false;
            }
        }

        // =====================================================================
        // BUTTON HANDLERS
        // =====================================================================

        private void OnLoadSlot(int slotIndex)
        {
            if (_saveSystem == null) return;

            Debug.Log($"[SaveSlotUI] Loading slot {slotIndex}");

            // Detect if we're in the main menu (no ISaveables to restore to).
            // Queue the slot and load the game scene; SaveSystem.Start will apply it.
            string activeScene = SceneManager.GetActiveScene().name;
            if (activeScene == "MainMenu" || activeScene == "Scenes/MainMenu")
            {
                SaveSystem.PendingLoadSlot = slotIndex;

                if (SceneLoader.Instance != null)
                {
                    SceneLoader.Instance.LoadScene(_gameSceneName);
                }
                else
                {
                    SceneManager.LoadScene(_gameSceneName);
                }
                return;
            }

            // In-game: load directly
            _saveSystem.LoadFromSlot(slotIndex);
        }

        private void OnDeleteSlot(int slotIndex)
        {
            if (_saveSystem == null) return;

            Debug.Log($"[SaveSlotUI] Deleting slot {slotIndex}");
            _saveSystem.DeleteSave(slotIndex);
            RefreshSlots();
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private TextMeshProUGUI FindChildTMP(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private Button FindChildButton(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private string FormatPlayTime(float seconds)
        {
            int hrs = Mathf.FloorToInt(seconds / 3600f);
            int mins = Mathf.FloorToInt((seconds % 3600f) / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);

            if (hrs > 0)
                return $"{hrs}h {mins:D2}m";
            else
                return $"{mins}m {secs:D2}s";
        }
    }
}
