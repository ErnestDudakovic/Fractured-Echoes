// ============================================================================
// GameEvent.cs — ScriptableObject-based event channel
// Central to the event-driven architecture. Decouples systems by allowing
// them to communicate through shared ScriptableObject event assets.
// Usage: Create event assets in the Project, wire listeners in Inspectors.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace FracturedEchoes.Core.Events
{
    /// <summary>
    /// A ScriptableObject-based event channel with no parameters.
    /// Systems raise and listen to these events without direct references.
    /// Create instances via: Create → Fractured Echoes → Events → Game Event
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "Fractured Echoes/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        // All currently registered listeners
        private readonly List<GameEventListener> _listeners = new List<GameEventListener>();

        /// <summary>
        /// Raises this event, notifying all registered listeners.
        /// </summary>
        public void Raise()
        {
            // Iterate backwards so listeners can safely unregister during callback
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised();
            }
        }

        /// <summary>
        /// Registers a listener to receive callbacks when this event is raised.
        /// </summary>
        public void RegisterListener(GameEventListener listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        /// <summary>
        /// Unregisters a listener so it no longer receives callbacks.
        /// </summary>
        public void UnregisterListener(GameEventListener listener)
        {
            _listeners.Remove(listener);
        }
    }
}
