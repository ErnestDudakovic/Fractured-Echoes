using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// NoteUI - the full-screen reading panel.
//
// Builds itself from code, so there is nothing to wire up. It is created
// automatically the first time a note is read.
//
// While a note is open the game is paused and every player script is switched
// off, then restored exactly as it was on close. E or Escape closes the page.
public class NoteUI : MonoBehaviour
{
    public static NoteUI Instance;

    /// <summary>True while a note is being read. Pickups.cs checks this.</summary>
    public static bool IsOpen;

    private GameObject Root;
    private Text TitleText;
    private Text BodyText;
    private Text CounterText;

    private readonly List<MonoBehaviour> Suspended = new List<MonoBehaviour>();
    private float SavedTimeScale = 1.0f;
    private CursorLockMode SavedLock;
    private bool SavedCursorVisible;
    private float OpenedAt;

    // ---------------------------------------------------------------- bootstrap

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("NoteUI");
        Instance = go.AddComponent<NoteUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsOpen = false;
        Build();
        Root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            IsOpen = false;
        }
    }

    private void Update()
    {
        if (!IsOpen) return;

        // small guard so the same E press cannot open and close it in one frame
        if (Time.unscaledTime - OpenedAt < 0.25f) return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Mouse0))
            Hide();
    }

    // ---------------------------------------------------------------- public

    public void Show(string title, string body)
    {
        if (IsOpen) return;

        TitleText.text = title;
        BodyText.text = body;
        CounterText.text = "Notes found: " + SaveScript.NotesFound;

        Root.SetActive(true);
        IsOpen = true;
        OpenedAt = Time.unscaledTime;

        SuspendPlayer();
    }

    public void Hide()
    {
        if (!IsOpen) return;

        Root.SetActive(false);
        IsOpen = false;

        RestorePlayer();
    }

    // ---------------------------------------------------------------- player

    private void SuspendPlayer()
    {
        SavedTimeScale = Time.timeScale;
        SavedLock = Cursor.lockState;
        SavedCursorVisible = Cursor.visible;

        Time.timeScale = 0.0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        Suspended.Clear();

        Transform player = SaveScript.PlayerChar;
        if (player == null) return;

        MonoBehaviour[] scripts = player.root.GetComponentsInChildren<MonoBehaviour>(false);

        for (int i = 0; i < scripts.Length; i++)
        {
            MonoBehaviour m = scripts[i];
            if (m == null || !m.enabled) continue;

            // Leave our own systems running so sanity and the HUD keep working.
            if (m is NoteUI || m is SanityScript || m is SanityBarUI) continue;

            m.enabled = false;
            Suspended.Add(m);
        }
    }

    private void RestorePlayer()
    {
        for (int i = 0; i < Suspended.Count; i++)
        {
            if (Suspended[i] != null) Suspended[i].enabled = true;
        }

        Suspended.Clear();

        Time.timeScale = SavedTimeScale;
        Cursor.lockState = SavedLock;
        Cursor.visible = SavedCursorVisible;
    }

    // ---------------------------------------------------------------- ui

    private void Build()
    {
        GameObject canvasGO = new GameObject("NoteCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Root = canvasGO;

        // dim the world behind the page
        Image dim = NewImage("Dim", canvasGO.transform, new Color(0.0f, 0.0f, 0.0f, 0.82f));
        Stretch(dim.rectTransform);

        // the page itself
        Image page = NewImage("Page", canvasGO.transform, new Color(0.86f, 0.83f, 0.74f, 0.97f));
        RectTransform pageRT = page.rectTransform;
        pageRT.anchorMin = new Vector2(0.5f, 0.5f);
        pageRT.anchorMax = new Vector2(0.5f, 0.5f);
        pageRT.pivot = new Vector2(0.5f, 0.5f);
        pageRT.sizeDelta = new Vector2(900.0f, 700.0f);
        pageRT.anchoredPosition = Vector2.zero;
        pageRT.localRotation = Quaternion.Euler(0.0f, 0.0f, -0.7f); // slightly crooked

        Font font = BuiltinFont();

        TitleText = NewText("Title", pageRT, font, 40, FontStyle.Bold, TextAnchor.UpperCenter);
        RectTransform titleRT = TitleText.rectTransform;
        titleRT.anchorMin = new Vector2(0.0f, 1.0f);
        titleRT.anchorMax = new Vector2(1.0f, 1.0f);
        titleRT.pivot = new Vector2(0.5f, 1.0f);
        titleRT.offsetMin = new Vector2(60.0f, 0.0f);
        titleRT.offsetMax = new Vector2(-60.0f, -50.0f);
        titleRT.sizeDelta = new Vector2(titleRT.sizeDelta.x, 60.0f);
        TitleText.color = new Color(0.12f, 0.09f, 0.07f, 1.0f);

        BodyText = NewText("Body", pageRT, font, 26, FontStyle.Normal, TextAnchor.UpperLeft);
        RectTransform bodyRT = BodyText.rectTransform;
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(70.0f, 90.0f);
        bodyRT.offsetMax = new Vector2(-70.0f, -130.0f);
        BodyText.color = new Color(0.16f, 0.13f, 0.10f, 1.0f);
        BodyText.lineSpacing = 1.25f;

        CounterText = NewText("Counter", pageRT, font, 20, FontStyle.Italic, TextAnchor.LowerLeft);
        RectTransform counterRT = CounterText.rectTransform;
        counterRT.anchorMin = Vector2.zero;
        counterRT.anchorMax = new Vector2(1.0f, 0.0f);
        counterRT.pivot = new Vector2(0.5f, 0.0f);
        counterRT.offsetMin = new Vector2(70.0f, 30.0f);
        counterRT.offsetMax = new Vector2(-70.0f, 60.0f);
        CounterText.color = new Color(0.32f, 0.27f, 0.22f, 1.0f);

        Text hint = NewText("Hint", pageRT, font, 20, FontStyle.Italic, TextAnchor.LowerRight);
        RectTransform hintRT = hint.rectTransform;
        hintRT.anchorMin = Vector2.zero;
        hintRT.anchorMax = new Vector2(1.0f, 0.0f);
        hintRT.pivot = new Vector2(0.5f, 0.0f);
        hintRT.offsetMin = new Vector2(70.0f, 30.0f);
        hintRT.offsetMax = new Vector2(-70.0f, 60.0f);
        hint.text = "E to close";
        hint.color = new Color(0.32f, 0.27f, 0.22f, 1.0f);
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static Text NewText(string name, Transform parent, Font font, int size, FontStyle style, TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        Text t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Font BuiltinFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }
}
