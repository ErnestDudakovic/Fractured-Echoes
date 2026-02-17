// ============================================================================
// ScriptedEventData.cs — ScriptableObject for scripted event definitions
// Defines psychological events: object disappearance, lighting flicker, etc.
// Create via: Create → Fractured Echoes → Data → Scripted Event Data
// ============================================================================

using UnityEngine;

namespace FracturedEchoes.ScriptableObjects
{
    /// <summary>
    /// Defines a scripted psychological event.
    /// These are the core tools for creating unease and tension.
    /// </summary>
    [CreateAssetMenu(fileName = "NewScriptedEvent", menuName = "Fractured Echoes/Data/Scripted Event Data")]
    public class ScriptedEventData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this scripted event.")]
        public string eventID;

        [Tooltip("Description for editor reference.")]
        [TextArea(2, 4)]
        public string editorDescription;

        [Header("Trigger Settings")]
        [Tooltip("How this event is triggered.")]
        public TriggerType triggerType;

        [Tooltip("Delay before the event executes after being triggered (seconds).")]
        public float triggerDelay = 0f;

        [Tooltip("Whether this event can only fire once.")]
        public bool oneTimeOnly = true;

        [Header("Conditions")]
        [Tooltip("Required environment phase to trigger (-1 = any).")]
        public int requiredPhase = -1;

        [Tooltip("Puzzle that must be completed before this event can trigger.")]
        public PuzzleData requiredPuzzle;

        [Tooltip("Item that must be in inventory for this event to trigger.")]
        public ItemData requiredItem;

        [Header("Event Type")]
        [Tooltip("The type of psychological event.")]
        public ScriptedEventType eventType;

        [Header("Timing")]
        [Tooltip("Duration of the event effect (seconds).")]
        public float effectDuration = 2f;

        [Header("Audio")]
        [Tooltip("Sound to play when this event triggers.")]
        public AudioClip eventSound;

        [Tooltip("Volume of the event sound.")]
        [Range(0f, 1f)]
        public float soundVolume = 1f;

        [Header("Sanity")]
        [Tooltip("Amount of sanity to drain when this event fires (0 = none).")]
        public float sanityDamage = 0f;

        [Header("Chaining")]
        [Tooltip("ID of another event to trigger immediately after this one completes. Leave empty for no chain.")]
        public string chainedEventID;
    }

    /// <summary>
    /// How a scripted event is triggered.
    /// </summary>
    public enum TriggerType
    {
        EnterArea,          // Player enters a trigger zone
        SolvePuzzle,        // A specific puzzle is solved
        PickUpObject,       // Player picks up a specific item
        LookAtObject,       // Player looks at a specific object
        Timer,              // Triggered after a time delay
        Combination,        // Multiple conditions must be met
        PhaseChange         // Environment phase changes
    }

    /// <summary>
    /// The type of psychological effect.
    /// </summary>
    public enum ScriptedEventType
    {
        ObjectAppear,           // An object appears
        ObjectDisappear,        // An object vanishes
        ObjectReposition,       // An object silently moves
        SoundTrigger,           // A sound plays from a location
        LightFlicker,           // Lights flicker
        ShadowMovement,         // A shadow shifts
        EnvironmentRearrange,   // Room layout subtly changes
        HallwayExtension,       // A corridor stretches
        DoorRelocation,         // A door moves to a different wall
        GeometryChange,         // Subtle geometry distortion
        MaterialShift,          // Materials/textures change
        CameraDistortion        // Subtle camera effects
    }
}
