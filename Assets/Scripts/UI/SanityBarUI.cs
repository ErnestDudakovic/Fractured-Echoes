// ============================================================================
// SanityBarUI.cs — On-screen sanity bar HUD
// A horizontal fill bar at the bottom-left of the screen that visualises the
// player's sanity. Colour shifts from green → yellow → red as sanity drops.
// Self-builds its own UI elements (no prefab required).
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FracturedEchoes.Player;

namespace FracturedEchoes.UI
{
    /// <summary>
    /// Renders a sanity bar at the bottom-left of the HUD.
    /// Subscribes to <see cref="SanitySystem.SanityChanged"/>.
    /// Attach to the Player or a UI manager object.
    /// </summary>
    public class SanityBarUI : MonoBehaviour
    {
        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Bar Dimensions")]
        [SerializeField] private float _barWidth = 200f;
        [SerializeField] private float _barHeight = 18f;
        [SerializeField] private float _marginLeft = 30f;
        [SerializeField] private float _marginBottom = 30f;

        [Header("Colors")]
        [SerializeField] private Color _highColor = new Color(0.2f, 0.85f, 0.3f, 0.9f);    // green
        [SerializeField] private Color _midColor = new Color(0.95f, 0.85f, 0.15f, 0.9f);   // yellow
        [SerializeField] private Color _lowColor = new Color(0.9f, 0.15f, 0.15f, 0.9f);    // red
        [SerializeField] private Color _bgColor = new Color(0.1f, 0.1f, 0.1f, 0.65f);
        [SerializeField] private Color _borderColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

        // =====================================================================
        // RUNTIME STATE
        // =====================================================================

        private SanitySystem _sanity;
        private Canvas _canvas;
        private Image _fillImage;
        private TextMeshProUGUI _label;
        private float _displayedFill = 1f;

        // Smoothing
        private const float SMOOTH_SPEED = 4f;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            _sanity = GetComponent<SanitySystem>();
            if (_sanity == null)
                _sanity = GetComponentInParent<SanitySystem>();
        }

        private void Start()
        {
            BuildUI();
            _displayedFill = _sanity != null ? _sanity.SanityNormalized : 1f;
            UpdateBar(_displayedFill);
        }

        private void OnEnable()
        {
            if (_sanity != null)
                _sanity.SanityChanged += OnSanityChanged;
        }

        private void OnDisable()
        {
            if (_sanity != null)
                _sanity.SanityChanged -= OnSanityChanged;
        }

        private void Update()
        {
            if (_sanity == null || _fillImage == null) return;

            // Smooth fill animation
            float target = _sanity.SanityNormalized;
            if (!Mathf.Approximately(_displayedFill, target))
            {
                _displayedFill = Mathf.MoveTowards(_displayedFill, target, SMOOTH_SPEED * Time.deltaTime);
                UpdateBar(_displayedFill);
            }
        }

        // =====================================================================
        // EVENT
        // =====================================================================

        private void OnSanityChanged(float current, float max)
        {
            // Target fill is set; Update() will smooth toward it.
        }

        // =====================================================================
        // UI BUILDING
        // =====================================================================

        private void BuildUI()
        {
            // --- Canvas ---
            var canvasGO = new GameObject("SanityBarCanvas");
            canvasGO.transform.SetParent(transform, false);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 14;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // --- Container (bottom-left) ---
            var containerGO = new GameObject("SanityContainer");
            containerGO.transform.SetParent(canvasGO.transform, false);
            var containerRT = containerGO.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0f, 0f);
            containerRT.anchorMax = new Vector2(0f, 0f);
            containerRT.pivot = new Vector2(0f, 0f);
            containerRT.anchoredPosition = new Vector2(_marginLeft, _marginBottom);
            containerRT.sizeDelta = new Vector2(_barWidth, _barHeight + 22f);

            // --- Label ---
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(containerRT, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 1f);
            labelRT.anchorMax = new Vector2(1f, 1f);
            labelRT.pivot = new Vector2(0f, 1f);
            labelRT.anchoredPosition = new Vector2(0f, 2f);
            labelRT.sizeDelta = new Vector2(_barWidth, 18f);

            _label = labelGO.AddComponent<TextMeshProUGUI>();
            _label.text = "SANITY";
            _label.fontSize = 13;
            _label.fontStyle = FontStyles.Bold;
            _label.color = new Color(0.85f, 0.85f, 0.85f, 0.8f);
            _label.alignment = TextAlignmentOptions.BottomLeft;

            // --- Bar background / border ---
            var borderGO = new GameObject("BarBorder");
            borderGO.transform.SetParent(containerRT, false);
            var borderRT = borderGO.AddComponent<RectTransform>();
            borderRT.anchorMin = new Vector2(0f, 0f);
            borderRT.anchorMax = new Vector2(1f, 0f);
            borderRT.pivot = new Vector2(0f, 0f);
            borderRT.anchoredPosition = Vector2.zero;
            borderRT.sizeDelta = new Vector2(0f, _barHeight);
            var borderImg = borderGO.AddComponent<Image>();
            borderImg.color = _borderColor;

            // Bar bg (inset by 2px)
            var bgGO = new GameObject("BarBG");
            bgGO.transform.SetParent(borderRT, false);
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = new Vector2(2, 2);
            bgRT.offsetMax = new Vector2(-2, -2);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = _bgColor;

            // Fill
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(bgRT, false);
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(1f, 1f);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            fillRT.pivot = new Vector2(0f, 0.5f);
            _fillImage = fillGO.AddComponent<Image>();
            _fillImage.color = _highColor;
        }

        // =====================================================================
        // REFRESH
        // =====================================================================

        private void UpdateBar(float normalised)
        {
            if (_fillImage == null) return;

            // Scale fill width (anchored from left)
            var rt = _fillImage.rectTransform;
            rt.anchorMax = new Vector2(normalised, 1f);

            // Color gradient: high → mid → low
            Color c;
            if (normalised > 0.5f)
                c = Color.Lerp(_midColor, _highColor, (normalised - 0.5f) * 2f);
            else
                c = Color.Lerp(_lowColor, _midColor, normalised * 2f);

            _fillImage.color = c;

            // Pulse label when critical
            if (normalised < 0.2f)
            {
                float pulse = Mathf.PingPong(Time.time * 2f, 1f);
                _label.color = Color.Lerp(_lowColor, Color.white, pulse);
            }
            else
            {
                _label.color = new Color(0.85f, 0.85f, 0.85f, 0.8f);
            }
        }
    }
}
