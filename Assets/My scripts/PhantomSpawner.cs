using UnityEngine;

// PhantomSpawner - places a silent apparition where the player will notice it.
//
// SETUP: empty GameObject -> Box Collider with "Is Trigger" ticked -> this
// component -> drag an enemy prefab into PhantomPrefab and one or more empty
// GameObjects into SpawnPoints.
//
// Modes:
//   InFront  - appears at a spawn point the player is currently looking at
//   Behind   - appears at a spawn point behind the player, for the turn-around
//   Any      - nearest spawn point, no direction check
//
// The spawned object gets a Phantom component, which disables its AI and
// colliders, so it can never actually reach the player.
[DisallowMultipleComponent]
public class PhantomSpawner : MonoBehaviour
{
    public enum Placement { InFront, Behind, Any }

    [Header("What to spawn")]
    [SerializeField] GameObject PhantomPrefab;
    [Tooltip("Empty GameObjects marking where it may appear. Put them on walkable ground.")]
    [SerializeField] Transform[] SpawnPoints;

    [Header("Where")]
    [SerializeField] Placement Where = Placement.InFront;
    [Tooltip("Ignore spawn points closer than this to the player.")]
    [SerializeField] float MinDistance = 8.0f;
    [Tooltip("Ignore spawn points further than this from the player.")]
    [SerializeField] float MaxDistance = 40.0f;

    [Header("When")]
    [Tooltip("Only fire once, then disable this object.")]
    [SerializeField] bool OneShot = true;
    [Tooltip("Seconds before this trigger can fire again when OneShot is off.")]
    [SerializeField] float RearmDelay = 120.0f;
    [Tooltip("Delay between entering the trigger and the apparition appearing.")]
    [SerializeField] float SpawnDelay = 0.0f;

    [Header("Tuning")]
    [Tooltip("Chance the apparition actually appears. Below 1 makes it unpredictable.")]
    [Range(0.0f, 1.0f)][SerializeField] float Chance = 1.0f;

    private float NextAllowedTime;

    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!PlayerRef.IsPlayer(other)) return;
        if (Time.time < NextAllowedTime) return;
        if (PhantomPrefab == null || SpawnPoints == null || SpawnPoints.Length == 0) return;

        NextAllowedTime = Time.time + RearmDelay;

        if (Random.value > Chance) return;

        if (SpawnDelay > 0.0f) Invoke("Spawn", SpawnDelay);
        else Spawn();

        if (OneShot) gameObject.SetActive(false);
    }

    private void Spawn()
    {
        Transform player = SaveScript.PlayerChar;
        if (player == null) player = Camera.main != null ? Camera.main.transform : null;
        if (player == null) return;

        Transform chosen = PickPoint(player);
        if (chosen == null) return;

        GameObject go = Instantiate(PhantomPrefab, chosen.position, chosen.rotation);

        if (go.GetComponent<Phantom>() == null)
            go.AddComponent<Phantom>();
    }

    private Transform PickPoint(Transform player)
    {
        Transform best = null;
        float bestScore = -999.0f;

        Vector3 forward = player.forward;
        forward.y = 0.0f;
        forward.Normalize();

        for (int i = 0; i < SpawnPoints.Length; i++)
        {
            Transform p = SpawnPoints[i];
            if (p == null) continue;

            Vector3 to = p.position - player.position;
            float dist = to.magnitude;

            if (dist < MinDistance || dist > MaxDistance) continue;

            to.y = 0.0f;
            to.Normalize();

            float dot = Vector3.Dot(forward, to);
            float score;

            if (Where == Placement.InFront)
            {
                if (dot < 0.3f) continue;   // not in view
                score = dot;
            }
            else if (Where == Placement.Behind)
            {
                if (dot > -0.2f) continue;  // not behind
                score = -dot;
            }
            else
            {
                score = -dist;              // nearest wins
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = p;
            }
        }

        return best;
    }

    private void OnDrawGizmosSelected()
    {
        if (SpawnPoints == null) return;

        Gizmos.color = new Color(0.8f, 0.2f, 0.9f, 0.8f);

        for (int i = 0; i < SpawnPoints.Length; i++)
        {
            if (SpawnPoints[i] == null) continue;
            Gizmos.DrawWireSphere(SpawnPoints[i].position + Vector3.up, 0.9f);
            Gizmos.DrawLine(transform.position, SpawnPoints[i].position);
        }
    }
}
