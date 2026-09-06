using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossShoots : MonoBehaviour
{
    [SerializeField] Animator Anim;
    [SerializeField] Transform ShootScript;
    private bool CanShoot = false;

    // Start is called before the first frame update
    void Start()
    {
        ShootScript.GetComponent<SimpleShoot1>().enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            CanShoot = true;
            Anim.SetTrigger("AimShoot");
            StartCoroutine(ShootPlayer());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            CanShoot = false;
            StartCoroutine(ShootPlayer());
        }
    }

    IEnumerator ShootPlayer()
    {
        yield return new WaitForSeconds(1f);
        if(CanShoot == true)
        {
            ShootScript.GetComponent<SimpleShoot1>().enabled = true;
        }
        if (CanShoot == false)
        {
            ShootScript.GetComponent<SimpleShoot1>().enabled = false;
        }
    }
}
