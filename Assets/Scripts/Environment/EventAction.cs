// ============================================================================
// EventAction.cs — Abstract base class for modular event actions
// Separates the "what happens" from the "when it happens" in the scripted
// event system. Attach concrete subclasses to GameObjects referenced by
// ScriptedEventInstance.targetObject to override the default switch-based
// execution with custom behaviour.
// ============================================================================

using System.Collections;
using UnityEngine;

namespace FracturedEchoes.Environment
{
    /// <summary>
    /// Base class for individual event actions.  A <see cref="ScriptedEventController"/>
    /// checks whether the target object carries an <see cref="EventAction"/>; if it does,
    /// the action's <see cref="Execute"/> coroutine runs instead of the built-in switch
    /// implementation, enabling fully custom behaviour without modifying the controller.
    /// </summary>
    public abstract class EventAction : MonoBehaviour
    {
        // =====================================================================
        // ABSTRACT API
        // =====================================================================

        /// <summary>
        /// Executes the action.  Yield <c>null</c> each frame for smooth
        /// transitions, or <c>yield break</c> for instant effects.
        /// The caller guarantees <paramref name="duration"/> matches
        /// <see cref="ScriptableObjects.ScriptedEventData.effectDuration"/>.
        /// </summary>
        /// <param name="duration">Desired effect duration in seconds.</param>
        public abstract IEnumerator Execute(float duration);

        /// <summary>
        /// Optional cleanup called if the event is interrupted (e.g. scene
        /// unload).  Default implementation does nothing.
        /// </summary>
        public virtual void Cancel()
        {
            StopAllCoroutines();
        }

        // =====================================================================
        // HELPERS — available to subclasses
        // =====================================================================

        /// <summary>Unclamped smooth-step (Hermite) interpolation.</summary>
        protected static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
