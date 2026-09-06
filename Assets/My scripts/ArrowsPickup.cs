using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowsPickup : MonoBehaviour
{
    [SerializeField] int ArrowsNumber;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(CheckArrows());
    }

    IEnumerator CheckArrows()
    {
        yield return new WaitForSeconds(1);
        if(ArrowsNumber > SaveScript.ArrowsLeft)
        {
            Destroy(gameObject);
        }
    }

}
