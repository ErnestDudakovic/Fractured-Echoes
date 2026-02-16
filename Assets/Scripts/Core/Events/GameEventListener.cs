// ============================================================================
// GameEventListener.cs — MonoBehaviour listener for GameEvent channels
// Attach to any GameObject to respond to ScriptableObject-based events.
// Wire event responses via UnityEvents in the Inspector.
// ============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace FracturedEchoes.Core.Events
{
    /// <summary>
    /// Listens to a GameEvent ScriptableObject and invokes a UnityEvent response.
    /// Attach this to any GameObject that needs to respond to game events.
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        [Tooltip("The ScriptableObject event channel to listen to.")]
        [SerializeField] private GameEvent _gameEvent;

        [Tooltip("The response to invoke when the event is raised.")]
        [SerializeField] private UnityEvent _response;

        private void OnEnable()
        {
            if (_gameEvent != null)
            {
                _gameEvent.RegisterListener(this);
            }
        }

        private void OnDisable()
        {
            if (_gameEvent != null)
            {
                _gameEvent.UnregisterListener(this);
            }
        }

        /// <summary>
        /// Called by the GameEvent when it is raised.
        /// </summary>
        public void OnEventRaised()
        {
            _response?.Invoke();
        }
    }
}
