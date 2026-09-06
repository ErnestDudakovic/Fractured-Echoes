using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossActivate : MonoBehaviour
{
    [SerializeField] GameObject EnemyDetectionZone;

    // Start is called before the first frame update
    void Start()
    {
        EnemyDetectionZone.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            EnemyDetectionZone.gameObject.SetActive(true);
        }
    }
}
