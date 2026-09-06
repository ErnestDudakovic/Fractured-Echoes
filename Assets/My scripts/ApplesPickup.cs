using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplesPickup : MonoBehaviour
{
    [SerializeField] int AppleNumber;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(CheckApples());
    }

    IEnumerator CheckApples()
    {
        yield return new WaitForSeconds(1);
        if(AppleNumber > SaveScript.ApplesLeft)
        {
            Destroy(gameObject);
        }
    }

}
