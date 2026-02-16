// ============================================================================
// ItemData.cs — ScriptableObject definition for inventory items
// Stores all metadata for an item: name, description, icon, 3D model, etc.
// Create instances via: Create → Fractured Echoes → Data → Item Data
// ============================================================================

using UnityEngine;

namespace FracturedEchoes.ScriptableObjects
{
    /// <summary>
    /// Data container for an inventory item.
    /// Each item in the game has one of these assets defining its properties.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Fractured Echoes/Data/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this item. Must be unique across all items.")]
        public string itemID;

        [Tooltip("Display name shown in the inventory UI.")]
        public string displayName;

        [TextArea(3, 6)]
        [Tooltip("Description shown when the player inspects this item.")]
        public string description;

        [Header("Visuals")]
        [Tooltip("Icon displayed in the inventory grid.")]
        public Sprite icon;

        [Tooltip("3D prefab used for the inspection view (rotate in 3D).")]
        public GameObject inspectionPrefab;

        [Tooltip("Prefab used when the item exists in the game world.")]
        public GameObject worldPrefab;

        [Header("Interaction")]
        [Tooltip("Whether this item can be combined with other items.")]
        public bool canCombine;

        [Tooltip("Whether this item can be used on environment objects.")]
        public bool canUseOnEnvironment;

        [Tooltip("Items this can be combined with (if canCombine is true).")]
        public ItemData[] combinableWith;

        [Tooltip("The item produced when combining (if applicable).")]
        public ItemData combinationResult;

        [Header("Audio")]
        [Tooltip("Sound played when picking up this item.")]
        public AudioClip pickupSound;

        [Tooltip("Sound played when using this item.")]
        public AudioClip useSound;
    }
}
