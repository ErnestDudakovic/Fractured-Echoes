using UnityEngine;

// SanityScript - core sanity resource for the horror loop.
//
// Drains while the player stands in darkness (flashlight AND night vision both off)
// and drains much faster while an enemy is close. Recovers when the player is lit
// or standing in a SanityZone marked as safe. Hitting zero kills the player through
// the existing HealthScript death panel.
//
// SETUP: drop this single component on the Player object. It builds its own HUD
// (SanityBarUI) and audio at runtime - nothing to wire in the Inspector.
[DisallowMultipleComponent]
public class SanityScript : MonoBehaviour
{
    public static SanityScript Instance;

    [Header("Sanity")]
    [SerializeField] float MaxSanity = 100.0f;

    [Header("Drain per second")]
    [Tooltip("Off by default. Sanity is meant to drain in designated spooky areas (SanityZone), not everywhere.")]
    [SerializeField] bool DrainInDarkness = false;
    [Tooltip("Only used when DrainInDarkness is ticked.")]
    [SerializeField] float DarknessDrain = 1.5f;
    [Tooltip("Extra drain while any enemy is within EnemyRadius.")]
    [SerializeField] float EnemyDrain = 7.0f;
    [SerializeField] float EnemyRadius = 18.0f;

    [Header("Recovery per second")]
    [SerializeField] float LightRegen = 2.5f;
    [Tooltip("Seconds of calm required before sanity starts recovering.")]
    [SerializeField] float RegenDelay = 3.0f;

    [Header("Feedback")]
    [Tooltip("Optional looping heartbeat. Leave empty for no audio.")]
    [SerializeField] AudioClip Heartbeat;
    [Tooltip("Heartbeat and the darkening overlay start below this fraction of max.")]
    [Range(0.1f, 1.0f)][SerializeField] float EffectsBelow = 0.55f;
    [SerializeField] float MaxHeartbeatVolume = 0.7f;

    // ---------------------------------------------------------------- runtime

    private float RegenTimer;
    private float EnemyCheckTimer;
    private bool EnemyNear;
    private AudioSource HeartSource;

    private float ZoneRate;          // per second, positive restores
    private bool ZoneIsSafe;
    private float ZoneStamp = -999f;

    private bool Dead;

    // ---------------------------------------------------------------- public

    public float Max { get { return MaxSanity; } }
    public float Current { get { return SaveScript.Sanity; } }

    public float Normalized
    {
        get { return MaxSanity <= 0.0f ? 0.0f : Mathf.Clamp01(SaveScript.Sanity / MaxSanity); }
    }

    /// <summary>True while the player has no light on and is not inside a safe zone.</summary>
    public bool InDarkness { get; private set; }

    /// <summary>True while an enemy is within EnemyRadius.</summary>
    public bool EnemyIsNear { get { return EnemyNear; } }

    /// <summary>Instant sanity hit. Use this from jumpscares and scripted events.</summary>
    public void Jumpscare(float amount)
    {
        Drain(amount);
    }

    public void Drain(float amount)
    {
        if (amount <= 0.0f) return;
        SaveScript.Sanity = Mathf.Max(0.0f, SaveScript.Sanity - amount);
        RegenTimer = RegenDelay;
    }

    public void Restore(float amount)
    {
        if (amount <= 0.0f) return;
        SaveScript.Sanity = Mathf.Min(MaxSanity, SaveScript.Sanity + amount);
    }

    /// <summary>Called by SanityZone every physics step while the player is inside it.</summary>
    public void ReportZone(float ratePerSecond, bool countsAsSafe)
    {
        ZoneRate = ratePerSecond;
        ZoneIsSafe = countsAsSafe;
        ZoneStamp = Time.time;
    }

    private bool InZone { get { return Time.time - ZoneStamp < 0.3f; } }

    // ---------------------------------------------------------------- unity

    private void Awake()
    {
        Instance = this;

        if (SaveScript.Sanity <= 0.0f || SaveScript.Sanity > MaxSanity)
            SaveScript.Sanity = MaxSanity;

        if (GetComponent<SanityBarUI>() == null)
            gameObject.AddComponent<SanityBarUI>();

        HeartSource = gameObject.AddComponent<AudioSource>();
        HeartSource.clip = Heartbeat;
        HeartSource.loop = true;
        HeartSource.playOnAwake = false;
        HeartSource.spatialBlend = 0.0f;
        HeartSource.volume = 0.0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (Dead) return;

        RefreshEnemyProximity();

        bool hasLight = SaveScript.FlashLightOn || SaveScript.NVLightOn;
        bool safeHere = InZone && ZoneIsSafe;
        InDarkness = !hasLight && !safeHere;

        float zone = InZone ? ZoneRate : 0.0f;

        // Everything that is currently taking sanity away
        float drain = 0.0f;
        if (DrainInDarkness && InDarkness) drain += DarknessDrain;
        if (EnemyNear) drain += EnemyDrain;
        if (zone < 0.0f) drain += -zone;

        // Everything that is giving it back
        float gain = 0.0f;
        if (zone > 0.0f) gain += zone;

        if (drain > 0.0f)
        {
            RegenTimer = RegenDelay;
        }
        else if (RegenTimer > 0.0f)
        {
            RegenTimer -= Time.deltaTime;
        }
        else if (!InZone)
        {
            // Passive recovery once the player is out of the spooky area and calm
            gain += LightRegen;
        }

        float delta = gain - drain;

        if (delta != 0.0f)
            SaveScript.Sanity = Mathf.Clamp(SaveScript.Sanity + delta * Time.deltaTime, 0.0f, MaxSanity);

        UpdateHeartbeat();

        if (SaveScript.Sanity <= 0.0f)
            Die();
    }

    // ---------------------------------------------------------------- internal

    private void RefreshEnemyProximity()
    {
        EnemyCheckTimer -= Time.deltaTime;
        if (EnemyCheckTimer > 0.0f) return;
        EnemyCheckTimer = 0.25f;

        EnemyNear = false;

        Collider[] hits = Physics.OverlapSphere(transform.position, EnemyRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].CompareTag("Enemy"))
            {
                EnemyNear = true;
                return;
            }
        }
    }

    private void UpdateHeartbeat()
    {
        if (HeartSource == null || HeartSource.clip == null) return;

        float n = Normalized;

        if (n >= EffectsBelow)
        {
            if (HeartSource.isPlaying) HeartSource.Stop();
            HeartSource.volume = 0.0f;
            return;
        }

        // 0 at the threshold, 1 at empty
        float intensity = 1.0f - (n / EffectsBelow);

        if (!HeartSource.isPlaying) HeartSource.Play();
        HeartSource.volume = intensity * MaxHeartbeatVolume;
        HeartSource.pitch = Mathf.Lerp(0.9f, 1.5f, intensity);
    }

    private void Die()
    {
        if (Dead) return;
        Dead = true;

        // Reuse the existing death flow: HealthScript watches PlayerHealth
        // and switches on its DeathPanel when it reaches zero.
        SaveScript.PlayerHealth = 0;
        SaveScript.HealthChanged = true;
    }
}
