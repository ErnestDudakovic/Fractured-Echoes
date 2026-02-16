// ============================================================================
// EnvironmentPhaseData.cs — ScriptableObject for room phase definitions
// Defines what changes occur at each corruption phase of a room.
// Create via: Create → Fractured Echoes → Data → Environment Phase Data
// ============================================================================

using UnityEngine;

namespace FracturedEchoes.ScriptableObjects
{
    /// <summary>
    /// Defines the visual and behavioral properties of a single environment phase.
    /// Rooms transition through these phases as the game progresses.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPhase", menuName = "Fractured Echoes/Data/Environment Phase Data")]
    public class EnvironmentPhaseData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Name of this phase (e.g., 'Clean', 'Slightly Altered', 'Corrupted').")]
        public string phaseName;

        [Tooltip("Phase index (0 = clean, higher = more corrupted).")]
        public int phaseIndex;

        [Header("Lighting")]
        [Tooltip("Ambient light color for this phase.")]
        public Color ambientColor = Color.white;

        [Tooltip("Ambient light intensity for this phase.")]
        [Range(0f, 2f)]
        public float ambientIntensity = 1f;

        [Tooltip("Fog color for this phase.")]
        public Color fogColor = Color.gray;

        [Tooltip("Fog density for this phase.")]
        [Range(0f, 0.1f)]
        public float fogDensity = 0.01f;

        [Header("Audio")]
        [Tooltip("Ambient audio clip for this phase.")]
        public AudioClip ambientLoop;

        [Tooltip("Volume of the ambient loop.")]
        [Range(0f, 1f)]
        public float ambientVolume = 0.5f;

        [Header("Post-Processing")]
        [Tooltip("Color grading saturation for this phase.")]
        [Range(-100f, 100f)]
        public float saturation = 0f;

        [Tooltip("Vignette intensity for this phase.")]
        [Range(0f, 1f)]
        public float vignetteIntensity = 0.2f;

        [Tooltip("Chromatic aberration intensity.")]
        [Range(0f, 1f)]
        public float chromaticAberration = 0f;

        [Header("Transition")]
        [Tooltip("Duration of the transition to this phase (seconds).")]
        public float transitionDuration = 3f;
    }
}
