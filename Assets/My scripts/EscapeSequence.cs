using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// EscapeSequence - the finale.
//
// Watches for the Room Key being picked up. The moment the player takes it,
// every rule the game has taught so far is revoked: the hunters stop losing
// interest, they move faster, and sanity drains no matter where you stand.
// The only way out is the street gate.
//
// Bootstraps itself - there is nothing to place in the scene. Enemies are
// spawned onto the NavMesh behind the player, so no spawn points are needed
// either.
public class EscapeSequence : MonoBehaviour
{
    public static EscapeSequence Instance;

    /// <summary>True once the Room Key is taken and the hunt is on.</summary>
    public static bool Active;

    // ---- tuning ----------------------------------------------------------

    private const int HunterCount = 4;
    private const float SpawnBehindMin = 28.0f;
    private const float SpawnBehindMax = 45.0f;
    private const float DrainPerSecond = 1.8f;
    private const float ReinforceEvery = 35.0f;

    [Header("Hunters")]
    [Tooltip("Enemy prefab spawned behind the player during the escape.")]
    [SerializeField] GameObject HunterPrefab;

    [Header("Target")]
    [Tooltip("The street gate, so the banner can count down the distance.")]
    [SerializeField] Vector3 GatePosition = new Vector3(683.0f, 17.0f, 415.0f);

    // ---- runtime ---------------------------------------------------------

    private Text Banner;
    private float ReinforceTimer;
    private readonly List<GameObject> Spawned = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Active = false;
        BuildBanner();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Active = false;
        }
    }

    private void Update()
    {
        if (!Active)
        {
            // The Room Key is the trigger. Pickups.cs sets this when the player
            // lifts it off Halloway's table.
            if (SaveScript.RoomKey) Begin();
            return;
        }

        SaveScript.Sanity = Mathf.Max(0.0f, SaveScript.Sanity - DrainPerSecond * Time.deltaTime);

        ReinforceTimer -= Time.deltaTime;
        if (ReinforceTimer <= 0.0f)
        {
            ReinforceTimer = ReinforceEvery;
            SpawnHunters(2);
        }

        UpdateBanner();
    }

    // ---- start -----------------------------------------------------------

    private void Begin()
    {
        Active = true;
        ReinforceTimer = ReinforceEvery;

        // Chase music is already wired up in the scene via SaveScript.
        if (SaveScript.Chase != null) SaveScript.Chase.SetActive(true);

        SpawnHunters(HunterCount);

        if (Banner != null) Banner.gameObject.SetActive(true);
    }

    /// <summary>Called by EscapeGate when the player gets out.</summary>
    public void End()
    {
        Active = false;

        if (SaveScript.Chase != null) SaveScript.Chase.SetActive(false);
        if (Banner != null) Banner.gameObject.SetActive(false);
    }

    // ---- spawning --------------------------------------------------------

    private void SpawnHunters(int count)
    {
        if (HunterPrefab == null) return;

        Transform player = SaveScript.PlayerChar;
        if (player == null) return;

        for (int i = 0; i < count; i++)
        {
            if (SaveScript.EnemiesOnScreen >= SaveScript.MaxEnemiesOnScreen + 4) return;

            Vector3 spot;
            if (!FindSpotBehind(player, out spot)) continue;

            GameObject e = Instantiate(HunterPrefab, spot, Quaternion.identity);
            Spawned.Add(e);
            SaveScript.EnemiesOnScreen++;
            SaveScript.EnemiesCurrent++;
        }
    }

    private bool FindSpotBehind(Transform player, out Vector3 result)
    {
        result = Vector3.zero;

        Vector3 back = -player.forward;
        back.y = 0.0f;
        back.Normalize();

        for (int attempt = 0; attempt < 12; attempt++)
        {
            float spread = Random.Range(-70.0f, 70.0f);
            float dist = Random.Range(SpawnBehindMin, SpawnBehindMax);

            Vector3 dir = Quaternion.Euler(0.0f, spread, 0.0f) * back;
            Vector3 want = player.position + dir * dist;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(want, out hit, 12.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        return false;
    }

    // ---- hud -------------------------------------------------------------

    private void BuildBanner()
    {
        GameObject canvasGO = new GameObject("EscapeCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject textGO = new GameObject("EscapeBanner");
        textGO.transform.SetParent(canvasGO.transform, false);

        RectTransform rt = textGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.0f, 1.0f);
        rt.anchorMax = new Vector2(1.0f, 1.0f);
        rt.pivot = new Vector2(0.5f, 1.0f);
        rt.anchoredPosition = new Vector2(0.0f, -70.0f);
        rt.sizeDelta = new Vector2(0.0f, 60.0f);

        Banner = textGO.AddComponent<Text>();
        Banner.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Banner.fontSize = 34;
        Banner.fontStyle = FontStyle.Bold;
        Banner.alignment = TextAnchor.UpperCenter;
        Banner.color = new Color(0.80f, 0.12f, 0.12f, 1.0f);
        Banner.raycastTarget = false;
        Banner.text = "RUN FOR THE STREET GATE";

        textGO.SetActive(false);
    }

    private void UpdateBanner()
    {
        if (Banner == null) return;

        Transform player = SaveScript.PlayerChar;
        if (player == null) return;

        Vector3 flat = player.position;
        flat.y = GatePosition.y;

        int metres = Mathf.RoundToInt(Vector3.Distance(flat, GatePosition));

        Banner.text = "RUN FOR THE STREET GATE   -   " + metres + "m";

        float pulse = Mathf.PingPong(Time.time * 2.5f, 1.0f);
        Banner.color = Color.Lerp(new Color(0.55f, 0.08f, 0.08f), new Color(0.95f, 0.25f, 0.25f), pulse);
    }
}
