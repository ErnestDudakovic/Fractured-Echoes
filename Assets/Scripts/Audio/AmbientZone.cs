// ============================================================================
// AmbientZone.cs — Spatial ambient audio trigger
// Defines an area where specific audio layers should be active.
// Handles smooth transitions as the player moves between zones.
// ============================================================================

using UnityEngine;

namespace FracturedEchoes.Audio
{
    /// <summary>
    /// Defines an ambient audio zone. When the player enters this zone,
    /// specific audio layers are activated/deactivated.
    /// Attach to a trigger collider to define an ambient region.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AmbientZone : MonoBehaviour
    {
        [Header("Zone Configuration")]
        [Tooltip("Names of audio layers to activate in this zone.")]
        [SerializeField] private string[] _activateLayers;

        [Tooltip("Names of audio layers to deactivate in this zone.")]
        [SerializeField] private string[] _deactivateLayers;

        [Tooltip("Fade duration when entering/leaving this zone.")]
        [SerializeField] private float _fadeDuration = 2f;

        [Tooltip("Tag of the trigger object (usually 'Player').")]
        [SerializeField] private string _triggerTag = "Player";

        [Header("References")]
        [SerializeField] private AudioManager _audioManager;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(_triggerTag)) return;
            if (_audioManager == null) return;

            // Activate zone layers
            if (_activateLayers != null)
            {
                foreach (string layer in _activateLayers)
                {
                    _audioManager.PlayLayer(layer);
                    _audioManager.FadeLayer(layer, 1f, _fadeDuration);
                }
            }

            // Deactivate layers
            if (_deactivateLayers != null)
            {
                foreach (string layer in _deactivateLayers)
                {
                    _audioManager.FadeLayer(layer, 0f, _fadeDuration);
                }
            }

            Debug.Log($"[AmbientZone] Player entered: {gameObject.name}");
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(_triggerTag)) return;
            if (_audioManager == null) return;

            // Reverse: deactivate zone layers when leaving
            if (_activateLayers != null)
            {
                foreach (string layer in _activateLayers)
                {
                    _audioManager.FadeLayer(layer, 0f, _fadeDuration);
                }
            }

            Debug.Log($"[AmbientZone] Player exited: {gameObject.name}");
        }
    }
}
