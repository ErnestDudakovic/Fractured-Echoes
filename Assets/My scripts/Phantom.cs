using UnityEngine;

// Phantom - turns a normal enemy into a silent apparition.
//
// A phantom never walks, never attacks and never touches the player. It simply
// stands there, faces the player, and is gone the moment you look away or after
// a few seconds. The point is doubt, not danger.
//
// Normally added by PhantomSpawner, but you can also drop it on an enemy placed
// by hand in the scene.
[DisallowMultipleComponent]
public class Phantom : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Seconds it stays even if the player keeps staring at it.")]
    [SerializeField] float MaxLifetime = 6.0f;
    [Tooltip("Minimum seconds before it is allowed to vanish.")]
    [SerializeField] float MinVisibleTime = 0.6f;
    [Tooltip("Vanish as soon as the player looks away.")]
    [SerializeField] bool VanishWhenUnseen = true;

    [Header("Effect")]
    [Tooltip("Sanity drained once, the first time the player actually sees it.")]
    [SerializeField] float SanityOnSighting = 8.0f;
    [Tooltip("Optional sound played once when it appears.")]
    [SerializeField] AudioClip AppearSound;
    [SerializeField] float AppearVolume = 0.5f;

    [Header("Facing")]
    [Tooltip("Slowly turn to keep facing the player.")]
    [SerializeField] bool FacePlayer = true;

    private Transform Player;
    private Camera PlayerCam;
    private float Age;
    private bool HasBeenSeen;

    private void Start()
    {
        Disarm();

        Player = SaveScript.PlayerChar;
        PlayerCam = Camera.main;

        if (AppearSound != null)
            AudioSource.PlayClipAtPoint(AppearSound, transform.position, AppearVolume);
    }

    private void Update()
    {
        Age += Time.deltaTime;

        if (Player != null && FacePlayer)
        {
            Vector3 dir = Player.position - transform.position;
            dir.y = 0.0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion want = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, want, Time.deltaTime * 3.0f);
            }
        }

        bool seen = IsOnScreen();

        if (seen && !HasBeenSeen)
        {
            HasBeenSeen = true;

            if (SanityScript.Instance != null)
                SanityScript.Instance.Jumpscare(SanityOnSighting);
        }

        if (Age >= MaxLifetime)
        {
            Vanish();
            return;
        }

        // The classic trick: it is only ever gone when you are not watching.
        if (VanishWhenUnseen && HasBeenSeen && Age >= MinVisibleTime && !seen)
            Vanish();
    }

    /// <summary>Strips out everything that would make this thing behave like a real enemy.</summary>
    private void Disarm()
    {
        MonoBehaviour[] scripts = GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < scripts.Length; i++)
        {
            MonoBehaviour m = scripts[i];
            if (m == null || m == this) continue;

            if (m is EnemyMove || m is EnemyAttack || m is EnemyDamage || m is BossAttack || m is BossShoots)
                m.enabled = false;
        }

        UnityEngine.AI.NavMeshAgent nav = GetComponentInChildren<UnityEngine.AI.NavMeshAgent>(true);
        if (nav != null) nav.enabled = false;

        // No colliders means it can never hurt or block the player.
        Collider[] cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;

        Animator anim = GetComponentInChildren<Animator>(true);
        if (anim != null) anim.SetInteger("State", 1); // idle / standing
    }

    private bool IsOnScreen()
    {
        if (PlayerCam == null)
        {
            PlayerCam = Camera.main;
            if (PlayerCam == null) return false;
        }

        Vector3 vp = PlayerCam.WorldToViewportPoint(transform.position + Vector3.up * 1.0f);

        return vp.z > 0.0f
            && vp.x > 0.05f && vp.x < 0.95f
            && vp.y > 0.05f && vp.y < 0.95f;
    }

    private void Vanish()
    {
        Destroy(gameObject);
    }
}
