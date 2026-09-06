using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatteryPickup : MonoBehaviour
{
    [SerializeField] int BatteryNumber;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(CheckBatteries());
    }

    IEnumerator CheckBatteries()
    {
        yield return new WaitForSeconds(1);
        if(BatteryNumber > SaveScript.BatteriesLeft)
        {
            Destroy(gameObject);
        }
    }

}
