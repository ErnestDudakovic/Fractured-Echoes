// ============================================================================
// GameEventString.cs — Typed event channel carrying a string payload
// Used for events that need to pass data (e.g., puzzle IDs, item names).
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace FracturedEchoes.Core.Events
{
    /// <summary>
    /// A ScriptableObject event channel that carries a string payload.
    /// Useful for puzzle completion events, inventory events, etc.
    /// Create via: Create → Fractured Echoes → Events → String Event
    /// </summary>
    [CreateAssetMenu(fileName = "NewStringEvent", menuName = "Fractured Echoes/Events/String Event")]
    public class GameEventString : ScriptableObject
    {
        private readonly List<IStringEventListener> _listeners = new List<IStringEventListener>();

        /// <summary>
        /// Raises this event with a string payload.
        /// </summary>
        public void Raise(string value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised(value);
            }
        }

        public void RegisterListener(IStringEventListener listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void UnregisterListener(IStringEventListener listener)
        {
            _listeners.Remove(listener);
        }
    }

    /// <summary>
    /// Interface for listeners of string-typed events.
    /// </summary>
    public interface IStringEventListener
    {
        void OnEventRaised(string value);
    }
}
