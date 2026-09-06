using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossAttack : MonoBehaviour
{
    private NavMeshAgent Nav;
    private float DistanceToPlayer;
    [SerializeField] Transform Player;
    [SerializeField] Animator Anim;
    [SerializeField] GameObject Enemy;
    [SerializeField] float AttackRotateSpeed = 2.0f;
    [SerializeField] GameObject ChaseMusic;
    [SerializeField] GameObject HurtUI;
    [SerializeField] GameObject EnemyDamageZone;
    private AnimatorStateInfo BossInfo;
    [SerializeField] Transform ShootScript;


    // Start is called before the first frame update
    void Start()
    {
        Nav = GetComponentInParent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        BossInfo = Anim.GetCurrentAnimatorStateInfo(0);
        DistanceToPlayer = Vector3.Distance(Player.position, Enemy.transform.position);
        if (BossInfo.IsTag("Chase"))
        {
            if (EnemyDamageZone.GetComponent<EnemyDamage>().HasDied == false)
            {
                ChaseMusic.gameObject.SetActive(true);
                Nav.isStopped = false;
                Nav.acceleration = 24;
                Nav.SetDestination(Player.position);
                HurtUI.gameObject.SetActive(false);
            }
        }
        if(BossInfo.IsTag("Shoot"))
        {
            if (EnemyDamageZone.GetComponent<EnemyDamage>().HasDied == false)
            {
                Nav.isStopped = true;
                Nav.acceleration = 180;
                HurtUI.gameObject.SetActive(true);

                Vector3 Pos = (Player.position - Enemy.transform.position).normalized;
                Quaternion PosRotation = Quaternion.LookRotation(new Vector3(Pos.x, 0, Pos.z));
                Enemy.transform.rotation = Quaternion.Slerp(Enemy.transform.rotation, PosRotation, Time.deltaTime * AttackRotateSpeed);
            }
        }

        if(BossInfo.IsTag("Hurt"))
        {
            ShootScript.GetComponent<SimpleShoot1>().enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PKnife"))
        {
            Anim.SetTrigger("SmallReact");
        }
        if (other.gameObject.CompareTag("PBat"))
        {
            Anim.SetTrigger("SmallReact");
        }
        if (other.gameObject.CompareTag("PAxe"))
        {
            Anim.SetTrigger("SmallReact");
        }
        if (other.gameObject.CompareTag("PCrossbow"))
        {
            Anim.SetTrigger("BigReact");
        }
    }
}
