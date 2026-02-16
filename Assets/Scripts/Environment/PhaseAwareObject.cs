// ============================================================================
// PhaseAwareObject.cs — Environment object that changes with room phases
// Implements IEnvironmentPhase. Activates/deactivates child objects
// or swaps materials based on the current environment phase.
// ============================================================================

using UnityEngine;
using FracturedEchoes.Core.Interfaces;

namespace FracturedEchoes.Environment
{
    /// <summary>
    /// An environment object that visually changes based on the current room phase.
    /// Configure different states in the Inspector — each phase index maps to
    /// a different visual state (child objects, materials, transforms, etc.).
    /// </summary>
    public class PhaseAwareObject : MonoBehaviour, IEnvironmentPhase
    {
        [Header("Phase Variants")]
        [Tooltip("GameObjects representing each phase state. Index matches phase index.")]
        [SerializeField] private GameObject[] _phaseVariants;

        [Header("Material Phases")]
        [Tooltip("Materials for each phase (if using material swap instead of object swap).")]
        [SerializeField] private Material[] _phaseMaterials;

        [Header("Configuration")]
        [Tooltip("Whether to use material swap instead of object toggling.")]
        [SerializeField] private bool _useMaterialSwap = false;

        private Renderer _renderer;
        private int _currentPhase = 0;

        public int PhaseCount => _phaseVariants != null ? _phaseVariants.Length : 
                                  _phaseMaterials != null ? _phaseMaterials.Length : 0;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        /// <summary>
        /// Applies the visual state for the given phase index.
        /// </summary>
        public void ApplyPhase(int phaseIndex)
        {
            _currentPhase = phaseIndex;

            if (_useMaterialSwap)
            {
                ApplyMaterialPhase(phaseIndex);
            }
            else
            {
                ApplyObjectPhase(phaseIndex);
            }

            Debug.Log($"[PhaseObject] {gameObject.name}: Applied phase {phaseIndex}");
        }

        private void ApplyObjectPhase(int phaseIndex)
        {
            if (_phaseVariants == null) return;

            for (int i = 0; i < _phaseVariants.Length; i++)
            {
                if (_phaseVariants[i] != null)
                {
                    _phaseVariants[i].SetActive(i == phaseIndex);
                }
            }
        }

        private void ApplyMaterialPhase(int phaseIndex)
        {
            if (_phaseMaterials == null || _renderer == null) return;
            if (phaseIndex < _phaseMaterials.Length && _phaseMaterials[phaseIndex] != null)
            {
                _renderer.material = _phaseMaterials[phaseIndex];
            }
        }
    }
}
