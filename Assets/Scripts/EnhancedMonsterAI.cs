using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MonsterAI : MonoBehaviour
{
    [Header("스탯")]
    public int hp = 60;
    public int damage = 15;
    public float speed = 2f;

    [Header("범위")]
    public float detectionRange = 18f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;

    [Header("타겟")]
    public Transform target;

    private NavMeshAgent agent;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    private enum State { Idle, Walk, Attack }
    private State currentState = State.Idle;
    private bool isAttacking = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        agent = GetComponent<NavMeshAgent>();

        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = speed;

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        ChangeState(State.Idle);
    }

    void Update()
    {
        if (target == null || isAttacking) return;

        float dist = Vector2.Distance(transform.position, target.position);

        if (dist <= attackRange)
            ChangeState(State.Attack);
        else if (dist <= detectionRange)
            ChangeState(State.Walk);
        else
            ChangeState(State.Idle);

        if (currentState == State.Walk)
        {
            if (IsAgentReady()) agent.SetDestination(target.position);
            UpdateAnimatorByVelocity();
        }
    }

    private bool IsAgentReady()
    {
        return agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
    }

    void ChangeState(State newState)
    {
        if (currentState == newState && newState != State.Attack) return;
        currentState = newState;

        switch (currentState)
        {
            case State.Idle:
                if (IsAgentReady()) agent.isStopped = true;
                anim.SetBool("IsWalking", false);
                break;

            case State.Walk:
                if (IsAgentReady()) agent.isStopped = false;
                anim.SetBool("IsWalking", true);
                anim.SetBool("IsAttacking", false);
                break;

            case State.Attack:
                if (!isAttacking) StartCoroutine(AttackRoutine());
                break;
        }
    }

    void UpdateAnimatorByVelocity()
    {
        if (!IsAgentReady() || agent.velocity.sqrMagnitude < 0.1f) return;

        Vector2 vel = agent.velocity;
        Vector2 snapped;
        if (Mathf.Abs(vel.x) >= Mathf.Abs(vel.y))
            snapped = new Vector2(vel.x > 0 ? 1 : -1, 0);
        else
            snapped = new Vector2(0, vel.y > 0 ? 1 : -1);

        anim.SetFloat("DirX", snapped.x);
        anim.SetFloat("DirY", snapped.y);
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        if (IsAgentReady())
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        Vector2 rawDir = (target.position - transform.position).normalized;
        Vector2 attackDir;
        if (Mathf.Abs(rawDir.x) >= Mathf.Abs(rawDir.y))
            attackDir = new Vector2(rawDir.x > 0 ? 1 : -1, 0);
        else
            attackDir = new Vector2(0, rawDir.y > 0 ? 1 : -1);

        anim.SetFloat("DirX", attackDir.x);
        anim.SetFloat("DirY", attackDir.y);
        anim.SetBool("IsWalking", false);
        anim.SetBool("IsAttacking", true);

        yield return new WaitForSeconds(attackCooldown);

        if (target != null)
        {
            Vector2 toPlayer = target.position - transform.position;
            bool inRange = toPlayer.magnitude <= attackRange + 0.3f;
            bool inFront = Vector2.Dot(attackDir, toPlayer.normalized) > 0.5f;

            if (inRange && inFront)
            {
                PlayerMovement player = target.GetComponent<PlayerMovement>();
                if (player != null) player.TakeHit(toPlayer.normalized);
                Debug.Log($"몬스터가 플레이어에게 {damage} 데미지!");
            }
        }

        anim.SetBool("IsAttacking", false);
        isAttacking = false;
        ChangeState(State.Idle);
    }

    public void TakeDamage(int amount)
    {
        hp -= amount;
        if (hp <= 0) Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}