// ============================================================================
// ScriptedEventController.cs — Psychological event execution system
// Manages scripted events: object appearance/disappearance, lighting flicker,
// shadow movement, environmental rearrangement, hallway extension, etc.
// Event-driven triggering with conditions and delays.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.Core.Events;
using FracturedEchoes.ScriptableObjects;

namespace FracturedEchoes.Environment
{
    /// <summary>
    /// Executes scripted psychological events based on triggers and conditions.
    /// This is the core system for creating tension and unease.
    /// Attach to a manager GameObject in each location/scene.
    /// </summary>
    public class ScriptedEventController : MonoBehaviour, ISaveable
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Configuration")]
        [Tooltip("All scripted events for this location.")]
        [SerializeField] private ScriptedEventInstance[] _events;

        [Header("Save")]
        [SerializeField] private string _saveID = "scripted_events";

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private HashSet<string> _triggeredEvents = new HashSet<string>();
        private Dictionary<string, ScriptedEventInstance> _eventLookup;

        // =====================================================================
        // PROPERTIES
        // =====================================================================

        public string SaveID => _saveID;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            // Build lookup dictionary for quick access
            _eventLookup = new Dictionary<string, ScriptedEventInstance>();
            if (_events != null)
            {
                foreach (var evt in _events)
                {
                    if (evt.eventData != null && !_eventLookup.ContainsKey(evt.eventData.eventID))
                    {
                        _eventLookup[evt.eventData.eventID] = evt;
                    }
                }
            }
        }

        // =====================================================================
        // PUBLIC API — TRIGGER METHODS
        // =====================================================================

        /// <summary>
        /// Triggers all events of a specific trigger type.
        /// Called by trigger zones, puzzle completions, etc.
        /// </summary>
        public void TriggerByType(TriggerType triggerType)
        {
            if (_events == null) return;

            foreach (var evt in _events)
            {
                if (evt.eventData != null && evt.eventData.triggerType == triggerType)
                {
                    TryTriggerEvent(evt);
                }
            }
        }

        /// <summary>
        /// Triggers a specific event by ID.
        /// </summary>
        public void TriggerByID(string eventID)
        {
            if (_eventLookup != null && _eventLookup.TryGetValue(eventID, out var evt))
            {
                TryTriggerEvent(evt);
            }
        }

        /// <summary>
        /// Triggers events associated with a phase change.
        /// </summary>
        public void TriggerPhaseEvents(int newPhase)
        {
            if (_events == null) return;

            foreach (var evt in _events)
            {
                if (evt.eventData != null &&
                    evt.eventData.triggerType == TriggerType.PhaseChange &&
                    evt.eventData.requiredPhase == newPhase)
                {
                    TryTriggerEvent(evt);
                }
            }
        }

        // =====================================================================
        // EVENT EXECUTION
        // =====================================================================

        private void TryTriggerEvent(ScriptedEventInstance evt)
        {
            if (evt.eventData == null) return;

            string eventID = evt.eventData.eventID;

            // Check if already triggered (one-time events)
            if (evt.eventData.oneTimeOnly && _triggeredEvents.Contains(eventID))
            {
                return;
            }

            // Check conditions
            if (!CheckConditions(evt.eventData))
            {
                return;
            }

            // Execute with optional delay
            if (evt.eventData.triggerDelay > 0f)
            {
                StartCoroutine(DelayedExecution(evt));
            }
            else
            {
                ExecuteEvent(evt);
            }
        }

        private IEnumerator DelayedExecution(ScriptedEventInstance evt)
        {
            yield return new WaitForSeconds(evt.eventData.triggerDelay);
            ExecuteEvent(evt);
        }

        private void ExecuteEvent(ScriptedEventInstance evt)
        {
            string eventID = evt.eventData.eventID;

            // Mark as triggered
            _triggeredEvents.Add(eventID);

            // Play event sound
            if (evt.eventData.eventSound != null && evt.audioSource != null)
            {
                evt.audioSource.PlayOneShot(evt.eventData.eventSound, evt.eventData.soundVolume);
            }

            // Execute based on event type
            switch (evt.eventData.eventType)
            {
                case ScriptedEventType.ObjectAppear:
                    ExecuteObjectAppear(evt);
                    break;

                case ScriptedEventType.ObjectDisappear:
                    ExecuteObjectDisappear(evt);
                    break;

                case ScriptedEventType.ObjectReposition:
                    ExecuteObjectReposition(evt);
                    break;

                case ScriptedEventType.SoundTrigger:
                    ExecuteSoundTrigger(evt);
                    break;

                case ScriptedEventType.LightFlicker:
                    ExecuteLightFlicker(evt);
                    break;

                case ScriptedEventType.ShadowMovement:
                    ExecuteShadowMovement(evt);
                    break;

                case ScriptedEventType.EnvironmentRearrange:
                    ExecuteEnvironmentRearrange(evt);
                    break;

                case ScriptedEventType.HallwayExtension:
                    ExecuteHallwayExtension(evt);
                    break;

                case ScriptedEventType.DoorRelocation:
                    ExecuteDoorRelocation(evt);
                    break;

                case ScriptedEventType.GeometryChange:
                    ExecuteGeometryChange(evt);
                    break;

                case ScriptedEventType.MaterialShift:
                    ExecuteMaterialShift(evt);
                    break;

                case ScriptedEventType.CameraDistortion:
                    ExecuteCameraDistortion(evt);
                    break;
            }

            Debug.Log($"[ScriptedEvent] Executed: {eventID} ({evt.eventData.eventType})");
        }

        // =====================================================================
        // EVENT TYPE IMPLEMENTATIONS
        // =====================================================================

        private void ExecuteObjectAppear(ScriptedEventInstance evt)
        {
            if (evt.targetObject != null)
            {
                StartCoroutine(FadeInObject(evt.targetObject, evt.eventData.effectDuration));
            }
        }

        private void ExecuteObjectDisappear(ScriptedEventInstance evt)
        {
            if (evt.targetObject != null)
            {
                StartCoroutine(FadeOutObject(evt.targetObject, evt.eventData.effectDuration));
            }
        }

        private void ExecuteObjectReposition(ScriptedEventInstance evt)
        {
            if (evt.targetObject != null && evt.targetPosition != null)
            {
                // Silently reposition — only when player isn't looking
                evt.targetObject.transform.position = evt.targetPosition.position;
                evt.targetObject.transform.rotation = evt.targetPosition.rotation;
            }
        }

        private void ExecuteSoundTrigger(ScriptedEventInstance evt)
        {
            // Sound is already played via eventSound above
            // Additional positional audio can be triggered here
            if (evt.audioSource != null && evt.eventData.eventSound != null)
            {
                evt.audioSource.spatialBlend = 1f; // 3D audio
                evt.audioSource.PlayOneShot(evt.eventData.eventSound, evt.eventData.soundVolume);
            }
        }

        private void ExecuteLightFlicker(ScriptedEventInstance evt)
        {
            if (evt.targetLight != null)
            {
                StartCoroutine(FlickerLight(evt.targetLight, evt.eventData.effectDuration));
            }
        }

        private void ExecuteShadowMovement(ScriptedEventInstance evt)
        {
            if (evt.targetObject != null && evt.targetPosition != null)
            {
                StartCoroutine(MoveObjectSmooth(evt.targetObject, evt.targetPosition.position, evt.eventData.effectDuration));
            }
        }

        private void ExecuteEnvironmentRearrange(ScriptedEventInstance evt)
        {
            // Activate alternate layout, deactivate current
            if (evt.targetObject != null) evt.targetObject.SetActive(false);
            if (evt.alternateObject != null) evt.alternateObject.SetActive(true);
        }

        private void ExecuteHallwayExtension(ScriptedEventInstance evt)
        {
            if (evt.targetObject != null)
            {
                StartCoroutine(ScaleObject(evt.targetObject, evt.targetScale, evt.eventData.effectDuration));
            }
        }

        private void ExecuteDoorRelocation(ScriptedEventInstance evt)
        {
            if (evt.targetObject != null) evt.targetObject.SetActive(false);
            if (evt.alternateObject != null) evt.alternateObject.SetActive(true);
        }

        private void ExecuteGeometryChange(ScriptedEventInstance evt)
        {
            if (evt.targetObject != null && evt.targetPosition != null)
            {
                StartCoroutine(MoveObjectSmooth(evt.targetObject, evt.targetPosition.position, evt.eventData.effectDuration));
            }
        }

        private void ExecuteMaterialShift(ScriptedEventInstance evt)
        {
            if (evt.targetObject != null && evt.alternateMaterial != null)
            {
                Renderer renderer = evt.targetObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    StartCoroutine(TransitionMaterial(renderer, evt.alternateMaterial, evt.eventData.effectDuration));
                }
            }
        }

        private void ExecuteCameraDistortion(ScriptedEventInstance evt)
        {
            // Camera effects are handled via player controller stress system
            Player.FirstPersonController player = FindObjectOfType<Player.FirstPersonController>();
            if (player != null)
            {
                player.SetPsychologicalEffects(true, true, true);
                StartCoroutine(ResetCameraEffects(player, evt.eventData.effectDuration));
            }
        }

        // =====================================================================
        // COROUTINE HELPERS
        // =====================================================================

        private IEnumerator FadeInObject(GameObject obj, float duration)
        {
            obj.SetActive(true);
            Renderer renderer = obj.GetComponent<Renderer>();

            if (renderer != null)
            {
                Color color = renderer.material.color;
                float elapsed = 0f;
                color.a = 0f;
                renderer.material.color = color;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    color.a = Mathf.Lerp(0f, 1f, elapsed / duration);
                    renderer.material.color = color;
                    yield return null;
                }

                color.a = 1f;
                renderer.material.color = color;
            }
        }

        private IEnumerator FadeOutObject(GameObject obj, float duration)
        {
            Renderer renderer = obj.GetComponent<Renderer>();

            if (renderer != null)
            {
                Color color = renderer.material.color;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    color.a = Mathf.Lerp(1f, 0f, elapsed / duration);
                    renderer.material.color = color;
                    yield return null;
                }
            }

            obj.SetActive(false);
        }

        private IEnumerator FlickerLight(Light light, float duration)
        {
            float originalIntensity = light.intensity;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // Random flicker pattern
                light.intensity = originalIntensity * Random.Range(0.1f, 1.2f);
                yield return new WaitForSeconds(Random.Range(0.02f, 0.15f));
            }

            light.intensity = originalIntensity;
        }

        private IEnumerator MoveObjectSmooth(GameObject obj, Vector3 target, float duration)
        {
            Vector3 start = obj.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t); // Smooth step
                obj.transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }

            obj.transform.position = target;
        }

        private IEnumerator ScaleObject(GameObject obj, Vector3 targetScale, float duration)
        {
            Vector3 startScale = obj.transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                obj.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            obj.transform.localScale = targetScale;
        }

        private IEnumerator TransitionMaterial(Renderer renderer, Material newMaterial, float duration)
        {
            // Simple material swap — for smooth transitions, use shader blending
            yield return new WaitForSeconds(duration * 0.5f);
            renderer.material = newMaterial;
        }

        private IEnumerator ResetCameraEffects(Player.FirstPersonController player, float duration)
        {
            yield return new WaitForSeconds(duration);
            player.SetPsychologicalEffects(false, false, false);
        }

        // =====================================================================
        // CONDITION CHECKING
        // =====================================================================

        private bool CheckConditions(ScriptedEventData data)
        {
            // Check required phase
            if (data.requiredPhase >= 0)
            {
                EnvironmentStateManager envManager = FindObjectOfType<EnvironmentStateManager>();
                if (envManager != null && envManager.CurrentPhase != data.requiredPhase)
                {
                    return false;
                }
            }

            // Check required puzzle completion
            if (data.requiredPuzzle != null)
            {
                Puzzle.PuzzleController[] puzzles = FindObjectsOfType<Puzzle.PuzzleController>();
                bool puzzleSolved = false;
                foreach (var puzzle in puzzles)
                {
                    if (puzzle.PuzzleID == data.requiredPuzzle.puzzleID &&
                        puzzle.CurrentState == Core.Interfaces.PuzzleState.Completed)
                    {
                        puzzleSolved = true;
                        break;
                    }
                }
                if (!puzzleSolved) return false;
            }

            // Check required item
            if (data.requiredItem != null)
            {
                InventorySystem.InventoryManager inventory = FindObjectOfType<InventorySystem.InventoryManager>();
                if (inventory == null || !inventory.HasItem(data.requiredItem.itemID))
                {
                    return false;
                }
            }

            return true;
        }

        // =====================================================================
        // ISaveable IMPLEMENTATION
        // =====================================================================

        public object CaptureState()
        {
            return new List<string>(_triggeredEvents);
        }

        public void RestoreState(object state)
        {
            if (state is List<string> triggeredIDs)
            {
                _triggeredEvents = new HashSet<string>(triggeredIDs);
            }
        }
    }

    // =========================================================================
    // EDITOR DATA STRUCTURE
    // =========================================================================

    /// <summary>
    /// Pairs ScriptedEventData with scene references for execution.
    /// Configured in the Inspector for each event instance.
    /// </summary>
    [System.Serializable]
    public class ScriptedEventInstance
    {
        [Tooltip("The event data configuration.")]
        public ScriptedEventData eventData;

        [Header("Scene References")]
        [Tooltip("The primary target object for this event.")]
        public GameObject targetObject;

        [Tooltip("Target position/rotation for repositioning events.")]
        public Transform targetPosition;

        [Tooltip("Alternate object (for rearrange/door relocation events).")]
        public GameObject alternateObject;

        [Tooltip("Target scale (for hallway extension events).")]
        public Vector3 targetScale = Vector3.one;

        [Tooltip("Alternate material (for material shift events).")]
        public Material alternateMaterial;

        [Tooltip("Target light (for flicker events).")]
        public Light targetLight;

        [Tooltip("Audio source for event sounds.")]
        public AudioSource audioSource;
    }
}
