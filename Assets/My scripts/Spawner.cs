using System.Collections;
using UnityEngine;

// Spawner - horror-paced enemy spawning.
//
// The original version dropped three enemies at once every time the player
// touched the trigger. For a no-combat horror game that is instant death, so
// this version spawns ONE enemy, picks a random spawn point, and refuses to
// fire again until RearmDelay has passed.
public class Spawner : MonoBehaviour
{
    [SerializeField] GameObject EnemySpawn1;
    [SerializeField] GameObject EnemySpawn2;
    [SerializeField] GameObject EnemySpawn3;
    [SerializeField] Transform SpawnPoint1;
    [SerializeField] Transform SpawnPoint2;
    [SerializeField] Transform SpawnPoint3;

    [Header("Horror pacing")]
    [Tooltip("How many enemies this trigger releases at once.")]
    [SerializeField] int SpawnCount = 1;
    [Tooltip("Seconds before this trigger can fire again. Set 0 for one-shot.")]
    [SerializeField] float RearmDelay = 90.0f;
    [Tooltip("Never fire again after the first time.")]
    [SerializeField] bool OneShot = false;

    private bool CanSpawn = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!PlayerRef.IsPlayer(other)) return;
        if (!CanSpawn) return;
        if (SaveScript.EnemiesCurrent >= SaveScript.MaxEnemiesInGame) return;
        if (SaveScript.EnemiesOnScreen >= SaveScript.MaxEnemiesOnScreen) return;

        CanSpawn = false;

        for (int i = 0; i < SpawnCount; i++)
        {
            if (SaveScript.EnemiesOnScreen >= SaveScript.MaxEnemiesOnScreen) break;
            SpawnOne();
        }

        if (!OneShot && RearmDelay > 0.0f)
            StartCoroutine(Rearm());
    }

    private void SpawnOne()
    {
        GameObject prefab = null;
        Transform point = null;

        // Pick a random valid prefab/point pair so repeat visits are not identical.
        int roll = Random.Range(0, 3);

        for (int i = 0; i < 3; i++)
        {
            int slot = (roll + i) % 3;

            if (slot == 0 && EnemySpawn1 != null && SpawnPoint1 != null)
            {
                prefab = EnemySpawn1; point = SpawnPoint1; break;
            }
            if (slot == 1 && EnemySpawn2 != null && SpawnPoint2 != null)
            {
                prefab = EnemySpawn2; point = SpawnPoint2; break;
            }
            if (slot == 2 && EnemySpawn3 != null && SpawnPoint3 != null)
            {
                prefab = EnemySpawn3; point = SpawnPoint3; break;
            }
        }

        if (prefab == null || point == null) return;

        Instantiate(prefab, point.position, point.rotation);
        SaveScript.EnemiesOnScreen++;
        SaveScript.EnemiesCurrent++;
    }

    private IEnumerator Rearm()
    {
        yield return new WaitForSeconds(RearmDelay);
        CanSpawn = true;
    }
}
