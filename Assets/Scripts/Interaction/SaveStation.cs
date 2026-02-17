// ============================================================================
// SaveStation.cs — Interactable save point object
// A glowing crystal-shaped object the player can interact with (E key)
// to open the save/load UI. Implements IInteractable for the raycast system.
// ============================================================================

using UnityEngine;
using FracturedEchoes.Core.Interfaces;
using FracturedEchoes.UI;

namespace FracturedEchoes.Interaction
{
    /// <summary>
    /// Save station world object. When the player looks at it and presses E,
    /// the <see cref="SaveStationUI"/> panel opens for saving / loading.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SaveStation : MonoBehaviour, IInteractable
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Interaction")]
        [SerializeField] private string _promptText = "Save Game";
        [SerializeField] private InteractionType _type = InteractionType.Activate;

        [Header("Visual")]
        [SerializeField] private Color _glowColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private float _pulseSpeed = 1.5f;
        [SerializeField] private float _pulseMin = 0.5f;
        [SerializeField] private float _pulseMax = 1.2f;

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private Renderer _renderer;
        private Material _material;
        private Color _baseEmission;
        private Light _pointLight;
        private bool _isFocused;
        private SaveStationUI _cachedUI;
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

        // =====================================================================
        // IInteractable IMPLEMENTATION
        // =====================================================================

        public string InteractionPrompt => _promptText;
        public InteractionType Type => _type;
        public bool CanInteract => true;
        public float InteractionCooldown => 0.5f;

        public void OnInteract()
        {
            if (_cachedUI != null)
            {
                _cachedUI.Open();
            }
            else
            {
                Debug.LogWarning("[SaveStation] No SaveStationUI found in scene.");
            }
        }

        public void OnFocus()
        {
            _isFocused = true;
        }

        public void OnLoseFocus()
        {
            _isFocused = false;
        }

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            _pointLight = GetComponentInChildren<Light>();
            _cachedUI = FindFirstObjectByType<SaveStationUI>();

            if (_renderer != null)
            {
                _material = _renderer.material; // instance
                _material.EnableKeyword("_EMISSION");
                _baseEmission = _glowColor;
            }
        }

        private void Update()
        {
            // Pulsing glow effect
            float pulse = Mathf.Lerp(_pulseMin, _pulseMax,
                (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f);

            Color emissionColor = _baseEmission * pulse;

            // Brighter pulse when focused
            if (_isFocused)
            {
                emissionColor *= 1.5f;
            }

            if (_material != null)
            {
                _material.SetColor(EmissionColorID, emissionColor);
            }

            if (_pointLight != null)
            {
                _pointLight.intensity = pulse * 0.8f;
                _pointLight.color = _glowColor;
            }
        }
    }
}
