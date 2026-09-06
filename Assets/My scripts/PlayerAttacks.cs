using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttacks : MonoBehaviour
{
    private Animator Anim;
    public float AttackStamina;
    [SerializeField] float MaxAttackStamina = 10;
    [SerializeField] float AttackDrain = 2;
    [SerializeField] float AttackRefill = 1;
    [SerializeField] GameObject Crosshair;
    [SerializeField] GameObject Pointer;
    private AudioSource MyPlayer;
    [SerializeField] AudioClip GunShotSound;
    [SerializeField] AudioClip BowShotSound;

    // Start is called before the first frame update
    void Start()
    {
        Anim = GetComponent<Animator>();
        AttackStamina = MaxAttackStamina;
        Crosshair.gameObject.SetActive(false);
        Pointer.gameObject.SetActive(true);
        MyPlayer = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        // No-combat horror: no weapon input at all.
        if (!GameRules.WeaponsEnabled) return;

        if(AttackStamina < MaxAttackStamina)
        {
            AttackStamina += AttackRefill * Time.deltaTime;
        }
        if(AttackStamina <= 0.1)
        {
            AttackStamina = 0.1f;
        }
        if (AttackStamina > 3.0)
        {
            if (SaveScript.InventoryOpen == false && SaveScript.OptionsOpen == false)
            {
                if (SaveScript.HaveKnife == true)
                {
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        Anim.SetTrigger("KnifeLMB");
                        AttackStamina -= AttackDrain;
                    }
                    if (Input.GetKeyDown(KeyCode.Mouse1))
                    {
                        Anim.SetTrigger("KnifeRMB");
                        AttackStamina -= AttackDrain;
                    }
                }
                if (SaveScript.HaveBat == true)
                {
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        Anim.SetTrigger("BatLMB");
                        AttackStamina -= AttackDrain;
                    }
                    if (Input.GetKeyDown(KeyCode.Mouse1))
                    {
                        Anim.SetTrigger("BatRMB");
                        AttackStamina -= AttackDrain;
                    }
                }
                if (SaveScript.HaveAxe == true)
                {
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        Anim.SetTrigger("AxeLMB");
                        AttackStamina -= AttackDrain;
                    }
                    if (Input.GetKeyDown(KeyCode.Mouse1))
                    {
                        Anim.SetTrigger("AxeRMB");
                        AttackStamina -= AttackDrain;
                    }
                }

                if (SaveScript.HaveGun == true)
                {
                    if (Input.GetKey(KeyCode.Mouse1))
                    {
                        Anim.SetBool("AimGun", true);
                        Crosshair.gameObject.SetActive(true);
                        Pointer.gameObject.SetActive(false);
                    }
                    if (Input.GetKeyUp(KeyCode.Mouse1))
                    {
                        Anim.SetBool("AimGun", false);
                        Crosshair.gameObject.SetActive(false);
                        Pointer.gameObject.SetActive(true);
                    }
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        if (SaveScript.Bullets > 0)
                        {
                            MyPlayer.clip = GunShotSound;
                            MyPlayer.Play();
                        }
                    }
                }

                if (SaveScript.HaveCrossbow == true)
                {
                    if (Input.GetKey(KeyCode.Mouse1))
                    {
                        Anim.SetBool("AimGun", true);
                        Crosshair.gameObject.SetActive(true);
                        Pointer.gameObject.SetActive(false);
                    }
                    if (Input.GetKeyUp(KeyCode.Mouse1))
                    {
                        Anim.SetBool("AimGun", false);
                        Crosshair.gameObject.SetActive(false);
                        Pointer.gameObject.SetActive(true);
                    }
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        if (SaveScript.Arrows > 0)
                        {
                            MyPlayer.clip = BowShotSound;
                            MyPlayer.Play();
                        }
                    }
                }
            }
        }
    }
}
