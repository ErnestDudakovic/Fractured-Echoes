// ============================================================================
// GameEventInt.cs — Typed event channel carrying an integer payload
// Used for phase changes, state indices, etc.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace FracturedEchoes.Core.Events
{
    /// <summary>
    /// A ScriptableObject event channel that carries an integer payload.
    /// Useful for environment phase changes, score updates, etc.
    /// Create via: Create → Fractured Echoes → Events → Int Event
    /// </summary>
    [CreateAssetMenu(fileName = "NewIntEvent", menuName = "Fractured Echoes/Events/Int Event")]
    public class GameEventInt : ScriptableObject
    {
        private readonly List<IIntEventListener> _listeners = new List<IIntEventListener>();

        /// <summary>
        /// Raises this event with an integer payload.
        /// </summary>
        public void Raise(int value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised(value);
            }
        }

        public void RegisterListener(IIntEventListener listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void UnregisterListener(IIntEventListener listener)
        {
            _listeners.Remove(listener);
        }
    }

    /// <summary>
    /// Interface for listeners of integer-typed events.
    /// </summary>
    public interface IIntEventListener
    {
        void OnEventRaised(int value);
    }
}
