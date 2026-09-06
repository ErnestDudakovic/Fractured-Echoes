using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodOnOff : MonoBehaviour
{
    [SerializeField] GameObject BloodOff;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(SwitchOff());
    }

    // Update is called once per frame
    void Update()
    {
        StartCoroutine(SwitchOff());
    }

    IEnumerator SwitchOff()
    {
        yield return new WaitForSeconds(0.2f);
        BloodOff.gameObject.SetActive(false);
    }
}
