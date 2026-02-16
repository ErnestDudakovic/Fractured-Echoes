// ============================================================================
// AudioLayerData.cs — ScriptableObject for ambient audio layer definitions
// Defines audio clips, volumes, and behavior for layered ambient system.
// Create via: Create → Fractured Echoes → Data → Audio Layer Data
// ============================================================================

using UnityEngine;

namespace FracturedEchoes.ScriptableObjects
{
    /// <summary>
    /// Defines a single audio layer in the ambient soundscape.
    /// Multiple layers blend together to create the full atmosphere.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioLayer", menuName = "Fractured Echoes/Data/Audio Layer Data")]
    public class AudioLayerData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique name for this audio layer.")]
        public string layerName;

        [Header("Audio")]
        [Tooltip("The audio clip for this layer.")]
        public AudioClip clip;

        [Tooltip("Whether this clip loops.")]
        public bool loop = true;

        [Tooltip("Default volume when this layer is active.")]
        [Range(0f, 1f)]
        public float defaultVolume = 0.5f;

        [Header("Spatial")]
        [Tooltip("Whether this is a 3D positional sound.")]
        public bool is3D = false;

        [Tooltip("Minimum distance for 3D audio falloff.")]
        public float minDistance = 1f;

        [Tooltip("Maximum distance for 3D audio falloff.")]
        public float maxDistance = 50f;

        [Header("Behavior")]
        [Tooltip("Priority of this layer (lower = higher priority).")]
        public int priority = 128;

        [Tooltip("Fade-in duration when this layer starts.")]
        public float fadeInDuration = 2f;

        [Tooltip("Fade-out duration when this layer stops.")]
        public float fadeOutDuration = 2f;

        [Header("Random Variation")]
        [Tooltip("Random pitch variation range (e.g., 0.1 means ±0.1).")]
        [Range(0f, 0.5f)]
        public float pitchVariation = 0f;

        [Tooltip("Random volume variation range.")]
        [Range(0f, 0.3f)]
        public float volumeVariation = 0f;
    }
}
