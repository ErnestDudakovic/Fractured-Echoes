// ============================================================================
// IAudioLayer.cs — Audio layering interface
// Supports the layered ambient audio architecture.
// ============================================================================

using UnityEngine;

namespace FracturedEchoes.Core.Interfaces
{
    /// <summary>
    /// Defines an audio layer that can be blended in the ambient system.
    /// Each layer has independent volume, priority, and playback control.
    /// </summary>
    public interface IAudioLayer
    {
        /// <summary>
        /// Unique name for this audio layer.
        /// </summary>
        string LayerName { get; }

        /// <summary>
        /// Current volume of this layer (0–1).
        /// </summary>
        float Volume { get; set; }

        /// <summary>
        /// Whether this layer is currently playing.
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Starts playback of this layer.
        /// </summary>
        void Play();

        /// <summary>
        /// Stops playback of this layer.
        /// </summary>
        void Stop();

        /// <summary>
        /// Crossfades this layer to a target volume over a duration.
        /// </summary>
        void FadeTo(float targetVolume, float duration);
    }
}
