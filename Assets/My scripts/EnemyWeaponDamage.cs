using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWeaponDamage : MonoBehaviour
{
    [SerializeField] int WeaponDamage = 1;
    [SerializeField] Animator HurtAnim;
    [SerializeField] AudioSource MyPlayer;
    private bool HitActive = false;
    [SerializeField] GameObject FPSArms;

    private void Start()
    {
        StartCoroutine(StartElements());

    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            if(HitActive == false)
            {
                HitActive = true;
                HurtAnim.SetTrigger("Hurt");
                SaveScript.PlayerHealth -= WeaponDamage;
                SaveScript.HealthChanged = true;
                MyPlayer.Play();
                FPSArms.GetComponent<PlayerAttacks>().AttackStamina -= 0.2f;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (HitActive == true)
            {
                HitActive = false;
            }
        }
    }

    IEnumerator StartElements()
    {
        yield return new WaitForSeconds(0.1f);
        HurtAnim = SaveScript.Hurt;
        MyPlayer = SaveScript.AudioP;
        FPSArms = SaveScript.Arms;
    }
}
