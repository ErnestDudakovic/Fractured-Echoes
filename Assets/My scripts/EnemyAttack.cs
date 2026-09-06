using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAttack : MonoBehaviour
{
    private NavMeshAgent Nav;
    private NavMeshHit hit;
    private bool blocked = false;
    public bool RunToPlayer = false;
    private float DistanceToPlayer;
    private bool IsChecking = true;
    private int FailedChecks = 0;
    [SerializeField] Transform Player;
    [SerializeField] Animator Anim;
    [SerializeField] GameObject Enemy;
    [SerializeField] float MaxRange = 35.0f;
    [SerializeField] int MaxChecks = 3;
    [SerializeField] float ChaseSpeed = 8.5f;
    [SerializeField] float WalkSpeed = 1.6f;
    [SerializeField] float AttackDistance = 2.3f;
    [SerializeField] float AttackRotateSpeed = 2.0f;
    [SerializeField] float CheckTime = 3.0f;
    [SerializeField] GameObject ChaseMusic;
    [SerializeField] GameObject HurtUI;
    [SerializeField] GameObject EnemyDamageZone;
    [SerializeField] bool IHaveKnife;
    [SerializeField] bool IHaveBat;
    [SerializeField] bool IHaveAxe;

    [Header("Horror pacing")]
    [Tooltip("Seconds of chasing before the enemy loses interest and walks away.")]
    [SerializeField] float MaxChaseTime = 8.0f;
    [Tooltip("Seconds after giving up before it may notice the player again.")]
    [SerializeField] float GiveUpCooldown = 25.0f;
    [Tooltip("Hard cap on chase speed. Player run speed is 12, so keep this below it.")]
    [SerializeField] float MaxChaseSpeed = 6.5f;
    [Tooltip("Sanity drained per second while this enemy is actively chasing.")]
    [SerializeField] float ChaseSanityDrain = 3.0f;

    private float ChaseTimer = 0.0f;
    private float CooldownTimer = 0.0f;
    private bool CanRun = false;

    // Start is called before the first frame update
    void Start()
    {
        Nav = GetComponentInParent<NavMeshAgent>();

        StartCoroutine(StartElements());

    }

    // Update is called once per frame
    void Update()
    {
        if (CanRun == true)
        {
            if (CooldownTimer > 0.0f) CooldownTimer -= Time.deltaTime;

            if (EnemyDamageZone.GetComponent<EnemyDamage>().HasDied == true)
            {
                ChaseMusic.gameObject.SetActive(false);
            }
            DistanceToPlayer = Vector3.Distance(Player.position, Enemy.transform.position);
            if (DistanceToPlayer < MaxRange)
            {
                if (IsChecking == true && (CooldownTimer <= 0.0f || EscapeSequence.Active))
                {
                    IsChecking = false;

                    blocked = NavMesh.Raycast(transform.position, Player.position, out hit, NavMesh.AllAreas);

                    if (blocked == false)
                    {
                        RunToPlayer = true;
                        FailedChecks = 0;
                    }
                    if (blocked == true)
                    {
                        RunToPlayer = false;
                        Anim.SetInteger("State", 1);
                        FailedChecks++;
                    }

                    StartCoroutine(TimedCheck());
                }
            }

            if (RunToPlayer == true)
            {
                // Horror pacing: the enemy only stays interested for a short burst,
                // then wanders off again. Being hunted forever is survival, not dread.
                ChaseTimer += Time.deltaTime;

                if (SanityScript.Instance != null)
                    SanityScript.Instance.Drain(ChaseSanityDrain * Time.deltaTime);

                if (ChaseTimer >= MaxChaseTime && !EscapeSequence.Active)
                {
                    LoseInterest();
                    return;
                }

                Enemy.GetComponent<EnemyMove>().enabled = false;
                if (EnemyDamageZone.GetComponent<EnemyDamage>().HasDied == false)
                {
                    ChaseMusic.gameObject.SetActive(true);
                }
                if (DistanceToPlayer > AttackDistance)
                {
                    Nav.isStopped = false;
                    Anim.SetInteger("State", 2);
                    Nav.acceleration = 24;
                    Nav.SetDestination(Player.position);
                    // During the escape they are faster and they never give up.
                    float cap = EscapeSequence.Active ? MaxChaseSpeed + 2.5f : MaxChaseSpeed;
                    Nav.speed = Mathf.Min(ChaseSpeed, cap);
                    HurtUI.gameObject.SetActive(false);
                }
                if (DistanceToPlayer < AttackDistance - 0.5f)
                {
                    Nav.isStopped = true;
                    if (IHaveAxe == true)
                    {
                        Anim.SetInteger("State", 3);
                    }
                    if (IHaveBat == true)
                    {
                        Anim.SetInteger("State", 4);
                    }
                    if (IHaveKnife == true)
                    {
                        Anim.SetInteger("State", 5);
                    }
                    Nav.acceleration = 180;
                    HurtUI.gameObject.SetActive(true);

                    Vector3 Pos = (Player.position - Enemy.transform.position).normalized;
                    Quaternion PosRotation = Quaternion.LookRotation(new Vector3(Pos.x, 0, Pos.z));
                    Enemy.transform.rotation = Quaternion.Slerp(Enemy.transform.rotation, PosRotation, Time.deltaTime * AttackRotateSpeed);
                }
            }
            else if (RunToPlayer == false)
            {
                ChaseTimer = 0.0f;
                Nav.isStopped = true;
            }
        }
    }

    /// <summary>Gives up the chase and returns to patrolling for GiveUpCooldown seconds.</summary>
    private void LoseInterest()
    {
        RunToPlayer = false;
        ChaseTimer = 0.0f;
        FailedChecks = 0;
        CooldownTimer = GiveUpCooldown;

        if (Enemy != null)
        {
            EnemyMove move = Enemy.GetComponent<EnemyMove>();
            if (move != null) move.enabled = true;
        }

        if (Nav != null)
        {
            Nav.isStopped = false;
            Nav.speed = WalkSpeed;
            Nav.acceleration = 24;
        }

        if (Anim != null) Anim.SetInteger("State", 0);
        if (HurtUI != null) HurtUI.gameObject.SetActive(false);
        if (ChaseMusic != null) ChaseMusic.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            if (CooldownTimer <= 0.0f || EscapeSequence.Active) RunToPlayer = true;
        }
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
            Anim.SetTrigger("BigReact");
        }
        if (other.gameObject.CompareTag("PCrossbow"))
        {
            Anim.SetTrigger("BigReact");
        }
    }

    IEnumerator TimedCheck()
    {
        yield return new WaitForSeconds(CheckTime);
        IsChecking = true;

        if(FailedChecks > MaxChecks)
        {
            Enemy.GetComponent<EnemyMove>().enabled = true;
            Nav.isStopped = false;
            Nav.speed = WalkSpeed;
            FailedChecks = 0;
            ChaseMusic.gameObject.SetActive(false);
        }
    }

    IEnumerator StartElements()
    {
        yield return new WaitForSeconds(0.1f);
        Player = SaveScript.PlayerChar;
        ChaseMusic = SaveScript.Chase;
        HurtUI = SaveScript.HurtScreen;
        ChaseMusic.gameObject.SetActive(false);
        CanRun = true;
        CheckTime = Random.Range(3, 15);
    }
}
