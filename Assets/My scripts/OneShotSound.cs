using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneShotSound : MonoBehaviour
{
    private AudioSource OneShot;
    private Collider Col;
    [SerializeField] bool OneTime = false;
    [SerializeField] float PauseTime = 5.0f;

    // Start is called before the first frame update
    void Start()
    {
        OneShot = GetComponent<AudioSource>();
        Col = GetComponent<Collider>();
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            OneShot.Play();
            Col.enabled = false;

            if(OneTime == false)
            {
                StartCoroutine(Reset());
            }
            else
            {
                Destroy(gameObject, PauseTime);
            }
        }
    }


    IEnumerator Reset()
    {
        yield return new WaitForSeconds(PauseTime);
        Col.enabled = true;
    }


}
