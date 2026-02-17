// ============================================================================
// SanityDrainZone.cs — Trigger zone that drains player sanity
// Place on a GameObject with a trigger collider. While the player stands
// inside, sanity is drained at a configurable rate per second.
// ============================================================================

using UnityEngine;
using FracturedEchoes.Player;

namespace FracturedEchoes.Environment
{
    /// <summary>
    /// Continuously drains sanity while the player remains inside the trigger.
    /// Useful for horror areas, darkness zones, or testing.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SanityDrainZone : MonoBehaviour
    {
        [Header("Drain Settings")]
        [Tooltip("Sanity drained per second while inside the zone.")]
        [SerializeField] private float _drainPerSecond = 10f;

        [Tooltip("One-time burst of sanity drain on first entry.")]
        [SerializeField] private float _entryDrain = 15f;

        [Tooltip("Tag of the object that triggers the zone.")]
        [SerializeField] private string _triggerTag = "Player";

        private SanitySystem _sanity;
        private bool _playerInside;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(_triggerTag)) return;

            _playerInside = true;

            if (_sanity == null)
                _sanity = other.GetComponent<SanitySystem>()
                       ?? other.GetComponentInParent<SanitySystem>();

            if (_sanity != null && _entryDrain > 0f)
            {
                _sanity.DrainSanity(_entryDrain);
                Debug.Log($"[SanityDrainZone] Entry drain: {_entryDrain}");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(_triggerTag)) return;
            _playerInside = false;
        }

        private void Update()
        {
            if (!_playerInside || _sanity == null) return;
            _sanity.DrainSanity(_drainPerSecond * Time.deltaTime);
        }
    }
}
