using UnityEngine;

// SanityZone - a designated area that messes with the player.
//
// This is the ONLY thing that drains sanity over time. Outside these volumes
// the player is left alone, so dread is something you walk into rather than a
// meter that is always ticking down.
//
// SETUP: empty GameObject -> Box/Sphere Collider with "Is Trigger" ticked ->
// this component -> size the collider around the room or area.
//
//   Negative RatePerSecond = spooky area (drains, plays the scary music)
//   Positive RatePerSecond = safe room  (restores, no music)
[DisallowMultipleComponent]
public class SanityZone : MonoBehaviour
{
    [Header("Sanity")]
    [Tooltip("Negative drains, positive restores. Per second.")]
    [SerializeField] float RatePerSecond = -4.0f;

    [Tooltip("Tick on lit / safe rooms. Only matters if DrainInDarkness is on.")]
    [SerializeField] bool CountsAsSafe = false;

    [Header("Scary music")]
    [Tooltip("Loops while the player is inside. Leave empty for silence.")]
    [SerializeField] AudioClip Music;
    [SerializeField] float MusicVolume = 0.6f;
    [Tooltip("Seconds to fade the music in when entering.")]
    [SerializeField] float FadeIn = 1.5f;
    [Tooltip("Seconds to fade the music out when leaving.")]
    [SerializeField] float FadeOut = 2.5f;

    [Header("One-shot sting")]
    [Tooltip("Fire once on entry instead of draining continuously.")]
    [SerializeField] bool OneShotOnEnter = false;
    [Tooltip("Instant sanity change on entry when OneShotOnEnter is ticked.")]
    [SerializeField] float OneShotAmount = -15.0f;

    // ---------------------------------------------------------------- runtime

    private AudioSource Source;
    private float InsideStamp = -999.0f;
    private bool Spent;

    private bool PlayerInside { get { return Time.time - InsideStamp < 0.3f; } }

    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void Awake()
    {
        if (Music == null) return;

        Source = gameObject.AddComponent<AudioSource>();
        Source.clip = Music;
        Source.loop = true;
        Source.playOnAwake = false;
        Source.spatialBlend = 0.0f;   // 2D, so it feels like score not a speaker
        Source.volume = 0.0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!PlayerRef.IsPlayer(other)) return;

        InsideStamp = Time.time;

        if (!OneShotOnEnter || Spent) return;
        if (SanityScript.Instance == null) return;

        Spent = true;

        if (OneShotAmount < 0.0f) SanityScript.Instance.Drain(-OneShotAmount);
        else SanityScript.Instance.Restore(OneShotAmount);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!PlayerRef.IsPlayer(other)) return;

        InsideStamp = Time.time;

        if (OneShotOnEnter) return;
        if (SanityScript.Instance == null) return;

        SanityScript.Instance.ReportZone(RatePerSecond, CountsAsSafe);
    }

    private void Update()
    {
        if (Source == null) return;

        bool inside = PlayerInside;
        float target = inside ? MusicVolume : 0.0f;
        float fade = inside ? FadeIn : FadeOut;

        if (inside && !Source.isPlaying) Source.Play();

        if (fade <= 0.0f)
        {
            Source.volume = target;
        }
        else
        {
            Source.volume = Mathf.MoveTowards(Source.volume, target, (MusicVolume / fade) * Time.deltaTime);
        }

        if (!inside && Source.volume <= 0.001f && Source.isPlaying) Source.Stop();
    }

    private void OnDrawGizmosSelected()
    {
        Collider c = GetComponent<Collider>();
        if (c == null) return;

        Gizmos.color = RatePerSecond >= 0.0f
            ? new Color(0.3f, 0.9f, 0.5f, 0.25f)
            : new Color(0.9f, 0.2f, 0.2f, 0.25f);

        Gizmos.DrawCube(c.bounds.center, c.bounds.size);
    }
}
