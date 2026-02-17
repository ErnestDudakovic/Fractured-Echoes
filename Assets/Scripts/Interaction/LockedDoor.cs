// ============================================================================
// LockedDoor.cs — A door that requires a specific inventory item to unlock
// Implements IInteractable (raycast target) + IItemReceiver (accepts items).
// When the correct item is used, the door opens (slides up or deactivates).
// ============================================================================

using UnityEngine;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.Core.Events;
using FracturedEchoes.ScriptableObjects;

namespace FracturedEchoes.Interaction
{
    /// <summary>
    /// A locked door that can be opened by using the correct inventory item on it.
    /// The player interacts via the InteractionSystem, and if they have the required
    /// item, it is consumed and the door opens.
    /// </summary>
    public class LockedDoor : MonoBehaviour, IInteractable, IItemReceiver, ISaveable
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Lock Settings")]
        [Tooltip("The item required to unlock this door.")]
        [SerializeField] private ItemData _requiredItem;

        [Tooltip("Prompt shown when the door is locked.")]
        [SerializeField] private string _lockedPrompt = "Locked — Requires Key";

        [Tooltip("Prompt shown after unlocking.")]
        [SerializeField] private string _unlockedPrompt = "Open Door";

        [Header("Door Behavior")]
        [Tooltip("How far the door slides up when opened.")]
        [SerializeField] private float _openHeight = 4f;

        [Tooltip("Speed of the opening animation.")]
        [SerializeField] private float _openSpeed = 3f;

        [Tooltip("If true, destroy the door after fully opening.")]
        [SerializeField] private bool _destroyWhenOpen = false;

        [Header("Audio")]
        [SerializeField] private AudioClip _unlockSound;
        [SerializeField] private AudioClip _openSound;
        [SerializeField] private AudioClip _lockedSound;

        [Header("Events")]
        [SerializeField] private GameEvent _onUnlocked;
        [SerializeField] private GameEvent _onOpened;

        [Header("Save")]
        [SerializeField] private string _saveID = "locked_door";

        // =====================================================================
        // STATE
        // =====================================================================

        private bool _isUnlocked;
        private bool _isOpen;
        private bool _isOpening;
        private Vector3 _closedPosition;
        private Vector3 _openPosition;
        private AudioSource _audioSource;

        // =====================================================================
        // IInteractable
        // =====================================================================

        public string InteractionPrompt => _isUnlocked ? _unlockedPrompt : _lockedPrompt;
        public InteractionType Type => InteractionType.Activate;
        public bool CanInteract => !_isOpen && !_isOpening;
        public float InteractionCooldown => 0.5f;

        public void OnInteract()
        {
            if (_isOpen || _isOpening) return;

            if (_isUnlocked)
            {
                // Already unlocked — open the door
                StartOpening();
            }
            else
            {
                // Still locked — play locked sound
                if (_lockedSound != null && _audioSource != null)
                    _audioSource.PlayOneShot(_lockedSound);

                Debug.Log($"[LockedDoor] Door is locked. Requires: {_requiredItem?.displayName ?? "???"}");
            }
        }

        public void OnFocus() { }
        public void OnLoseFocus() { }

        // =====================================================================
        // IItemReceiver
        // =====================================================================

        public string RequiredItemID => _requiredItem != null ? _requiredItem.itemID : null;

        public bool CanReceiveItem(ItemData item)
        {
            if (_isUnlocked || _isOpen) return false;
            if (_requiredItem == null) return true;
            return item != null && item.itemID == _requiredItem.itemID;
        }

        public bool ReceiveItem(ItemData item)
        {
            if (!CanReceiveItem(item)) return false;

            _isUnlocked = true;

            if (_unlockSound != null && _audioSource != null)
                _audioSource.PlayOneShot(_unlockSound);

            _onUnlocked?.Raise();
            Debug.Log($"[LockedDoor] Unlocked with: {item.displayName}");

            // Auto-open after unlocking
            StartOpening();
            return true; // consume the item
        }

        // =====================================================================
        // ISaveable
        // =====================================================================

        public string SaveID => _saveID;

        public object CaptureState()
        {
            return new DoorSaveState { isUnlocked = _isUnlocked, isOpen = _isOpen };
        }

        public void RestoreState(object state)
        {
            if (state is DoorSaveState doorState)
            {
                _isUnlocked = doorState.isUnlocked;
                if (doorState.isOpen)
                {
                    _isOpen = true;
                    transform.position = _openPosition;
                    if (_destroyWhenOpen) gameObject.SetActive(false);
                }
            }
        }

        [System.Serializable]
        private class DoorSaveState
        {
            public bool isUnlocked;
            public bool isOpen;
        }

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            _closedPosition = transform.position;
            _openPosition = _closedPosition + Vector3.up * _openHeight;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f;
            }
        }

        private void Update()
        {
            if (!_isOpening) return;

            transform.position = Vector3.MoveTowards(
                transform.position, _openPosition, _openSpeed * Time.deltaTime);

            if ((transform.position - _openPosition).sqrMagnitude < 0.0001f)
            {
                transform.position = _openPosition;
                _isOpening = false;
                _isOpen = true;
                _onOpened?.Raise();

                if (_destroyWhenOpen)
                    gameObject.SetActive(false);

                Debug.Log("[LockedDoor] Door fully opened.");
            }
        }

        // =====================================================================
        // INTERNAL
        // =====================================================================

        private void StartOpening()
        {
            _isOpening = true;

            if (_openSound != null && _audioSource != null)
                _audioSource.PlayOneShot(_openSound);
        }

        /// <summary>Force unlock without requiring an item (e.g. via puzzle solve).</summary>
        public void ForceUnlock()
        {
            _isUnlocked = true;
            _onUnlocked?.Raise();
        }
    }
}
