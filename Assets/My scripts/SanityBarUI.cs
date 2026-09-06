using UnityEngine;
using UnityEngine.UI;

// SanityBarUI - builds the sanity HUD entirely from code, so there is nothing
// to set up in the Inspector. Added automatically by SanityScript.
//
// Two separate canvases are used on purpose:
//   - the darkening overlay sits BELOW the existing game HUD (sorting order 5)
//   - the sanity bar sits ABOVE everything (sorting order 20) so it stays readable
[DisallowMultipleComponent]
public class SanityBarUI : MonoBehaviour
{
    [Header("Bar")]
    [SerializeField] float BarWidth = 220.0f;
    [SerializeField] float BarHeight = 16.0f;
    [SerializeField] float MarginLeft = 30.0f;
    [SerializeField] float MarginBottom = 30.0f;

    [Header("Colors")]
    [SerializeField] Color HighColor = new Color(0.55f, 0.72f, 0.70f, 0.90f);
    [SerializeField] Color MidColor = new Color(0.80f, 0.70f, 0.30f, 0.90f);
    [SerializeField] Color LowColor = new Color(0.75f, 0.12f, 0.12f, 0.95f);

    [Header("Darkening overlay")]
    [Tooltip("Overlay fades in below this fraction of max sanity.")]
    [Range(0.1f, 1.0f)][SerializeField] float OverlayBelow = 0.55f;
    [SerializeField] float MaxOverlayAlpha = 0.60f;

    private SanityScript Sanity;
    private RectTransform FillRect;
    private Image FillImage;
    private Text Label;
    private Image Overlay;
    private float Displayed = 1.0f;

    private void Awake()
    {
        Sanity = GetComponent<SanityScript>();
        if (Sanity == null) Sanity = GetComponentInParent<SanityScript>();
    }

    private void Start()
    {
        BuildOverlay();
        BuildBar();

        Displayed = Sanity != null ? Sanity.Normalized : 1.0f;
        Refresh(Displayed);
    }

    private void Update()
    {
        if (Sanity == null || FillImage == null) return;

        float target = Sanity.Normalized;
        if (!Mathf.Approximately(Displayed, target))
            Displayed = Mathf.MoveTowards(Displayed, target, 1.5f * Time.deltaTime);

        Refresh(Displayed);
    }

    // ---------------------------------------------------------------- build

    private Canvas MakeCanvas(string name, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private void BuildOverlay()
    {
        Canvas canvas = MakeCanvas("SanityOverlayCanvas", 5);

        GameObject go = new GameObject("SanityOverlay");
        go.transform.SetParent(canvas.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Overlay = go.AddComponent<Image>();
        Overlay.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        Overlay.raycastTarget = false;
    }

    private void BuildBar()
    {
        Canvas canvas = MakeCanvas("SanityBarCanvas", 20);

        // container, anchored bottom-left
        GameObject container = new GameObject("SanityContainer");
        container.transform.SetParent(canvas.transform, false);

        RectTransform containerRT = container.AddComponent<RectTransform>();
        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.zero;
        containerRT.pivot = Vector2.zero;
        containerRT.anchoredPosition = new Vector2(MarginLeft, MarginBottom);
        containerRT.sizeDelta = new Vector2(BarWidth, BarHeight + 22.0f);

        // label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(containerRT, false);

        RectTransform labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0.0f, 1.0f);
        labelRT.anchorMax = new Vector2(1.0f, 1.0f);
        labelRT.pivot = new Vector2(0.0f, 1.0f);
        labelRT.anchoredPosition = new Vector2(0.0f, 2.0f);
        labelRT.sizeDelta = new Vector2(BarWidth, 18.0f);

        Label = labelGO.AddComponent<Text>();
        Label.font = BuiltinFont();
        Label.fontSize = 14;
        Label.fontStyle = FontStyle.Bold;
        Label.text = "SANITY";
        Label.alignment = TextAnchor.LowerLeft;
        Label.color = new Color(0.85f, 0.85f, 0.85f, 0.8f);
        Label.raycastTarget = false;

        // border
        GameObject borderGO = new GameObject("BarBorder");
        borderGO.transform.SetParent(containerRT, false);

        RectTransform borderRT = borderGO.AddComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = new Vector2(1.0f, 0.0f);
        borderRT.pivot = Vector2.zero;
        borderRT.anchoredPosition = Vector2.zero;
        borderRT.sizeDelta = new Vector2(0.0f, BarHeight);

        Image borderImg = borderGO.AddComponent<Image>();
        borderImg.color = new Color(0.45f, 0.45f, 0.45f, 0.6f);
        borderImg.raycastTarget = false;

        // background, inset by 2px
        GameObject bgGO = new GameObject("BarBG");
        bgGO.transform.SetParent(borderRT, false);

        RectTransform bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = new Vector2(2.0f, 2.0f);
        bgRT.offsetMax = new Vector2(-2.0f, -2.0f);

        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.08f, 0.7f);
        bgImg.raycastTarget = false;

        // fill
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(bgRT, false);

        FillRect = fillGO.AddComponent<RectTransform>();
        FillRect.anchorMin = Vector2.zero;
        FillRect.anchorMax = Vector2.one;
        FillRect.offsetMin = Vector2.zero;
        FillRect.offsetMax = Vector2.zero;
        FillRect.pivot = new Vector2(0.0f, 0.5f);

        FillImage = fillGO.AddComponent<Image>();
        FillImage.color = HighColor;
        FillImage.raycastTarget = false;
    }

    private static Font BuiltinFont()
    {
        // Unity 2022+ renamed the built-in Arial resource.
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    // ---------------------------------------------------------------- refresh

    private void Refresh(float n)
    {
        if (FillRect != null)
            FillRect.anchorMax = new Vector2(Mathf.Clamp01(n), 1.0f);

        if (FillImage != null)
        {
            FillImage.color = n > 0.5f
                ? Color.Lerp(MidColor, HighColor, (n - 0.5f) * 2.0f)
                : Color.Lerp(LowColor, MidColor, n * 2.0f);
        }

        if (Label != null)
        {
            if (n < 0.2f)
            {
                float pulse = Mathf.PingPong(Time.time * 2.0f, 1.0f);
                Label.color = Color.Lerp(LowColor, Color.white, pulse);
            }
            else
            {
                Label.color = new Color(0.85f, 0.85f, 0.85f, 0.8f);
            }
        }

        if (Overlay != null)
        {
            float alpha = 0.0f;

            if (n < OverlayBelow)
            {
                float intensity = 1.0f - (n / OverlayBelow);
                // slow breathing pulse so it never sits perfectly still
                float breathe = 0.85f + 0.15f * Mathf.Sin(Time.time * 2.5f);
                alpha = intensity * MaxOverlayAlpha * breathe;
            }

            Overlay.color = new Color(0.0f, 0.0f, 0.0f, alpha);
        }
    }
}
