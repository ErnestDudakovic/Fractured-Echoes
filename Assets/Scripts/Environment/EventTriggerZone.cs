// ============================================================================
// EventTriggerZone.cs — Trigger zone for scripted events
// Place on a trigger collider to fire events when the player enters.
// ============================================================================

using UnityEngine;
using FracturedEchoes.ScriptableObjects;

namespace FracturedEchoes.Environment
{
    /// <summary>
    /// Trigger zone that fires scripted events when the player enters.
    /// Attach to a GameObject with a Collider set to "Is Trigger".
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EventTriggerZone : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Specific event ID to trigger (leave empty to trigger by type).")]
        [SerializeField] private string _specificEventID;

        [Tooltip("Tag of the object that can trigger this zone.")]
        [SerializeField] private string _triggerTag = "Player";

        [Tooltip("Whether this trigger zone works only once.")]
        [SerializeField] private bool _oneTimeOnly = true;

        [Header("References")]
        [Tooltip("The scripted event controller to notify.")]
        [SerializeField] private ScriptedEventController _eventController;

        private bool _hasTriggered;

        private void OnTriggerEnter(Collider other)
        {
            if (_hasTriggered && _oneTimeOnly) return;
            if (!other.CompareTag(_triggerTag)) return;

            _hasTriggered = true;

            if (_eventController != null)
            {
                if (!string.IsNullOrEmpty(_specificEventID))
                {
                    _eventController.TriggerByID(_specificEventID);
                }
                else
                {
                    _eventController.TriggerByType(TriggerType.EnterArea);
                }
            }

            Debug.Log($"[TriggerZone] Player entered: {gameObject.name}");
        }
    }
}
