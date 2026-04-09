using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnhancedMonsterAI : MonoBehaviour
{
    [Header("Stats")]
    public int hp = 60;
    public int damage = 15;
    public float speed = 2f;

    [Header("Ranges")]
    public float detectionRange = 18f;
    public float attackRange = 1.2f;
    public float attackTime = 1.0f;

    [Header("Targets")]
    public Transform target;

    // ↓↓↓ 새로 추가된 넉백 설정 ↓↓↓
    [Header("넉백 설정")]
    public float knockbackForce = 4f;
    public float knockbackDuration = 0.15f;
    // ↑↑↑ 새로 추가된 넉백 설정 ↑↑↑

    private NavMeshAgent agent;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    private enum State { Idle, Walk, Attack, Hurt }
    private State currentState = State.Idle;
    private bool isAttacking = false;
    private bool isHurt = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        agent = GetComponent<NavMeshAgent>();
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
        // 피격 중이거나 공격 중이면 AI 정지
        if (target == null || isAttacking || isHurt) return;

        float distanceToPlayer = Vector2.Distance(transform.position, target.position);

        if (distanceToPlayer <= attackRange)
            ChangeState(State.Attack);
        else if (distanceToPlayer <= detectionRange)
            ChangeState(State.Walk);
        else
            ChangeState(State.Idle);

        if (currentState == State.Walk)
        {
            agent.SetDestination(target.position);
            UpdateAnimatorByVelocity();
        }
    }

    void ChangeState(State newState)
    {
        if (currentState == newState && newState != State.Attack) return;

        currentState = newState;

        switch (currentState)
        {
            case State.Idle:
                agent.isStopped = true;
                anim.SetBool("IsWalking", false);
                break;

            case State.Walk:
                agent.isStopped = false;
                anim.SetBool("IsWalking", true);
                anim.SetBool("IsAttacking", false);
                break;

            case State.Attack:
                if (!isAttacking) StartCoroutine(AttackRoutine());
                break;
        }
    }

    // NavMesh velocity를 4방향으로 스냅해서 애니메이터에 전달
    void UpdateAnimatorByVelocity()
    {
        if (agent.velocity.sqrMagnitude < 0.1f) return;

        Vector2 vel = agent.velocity;

        // 수평/수직 중 더 큰 축만 살림 → 4방향 스냅
        Vector2 snapped;
        if (Mathf.Abs(vel.x) >= Mathf.Abs(vel.y))
            snapped = new Vector2(vel.x > 0 ? 1 : -1, 0); // 좌 or 우
        else
            snapped = new Vector2(0, vel.y > 0 ? 1 : -1); // 상 or 하

        anim.SetFloat("DirX", snapped.x);
        anim.SetFloat("DirY", snapped.y);
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // 공격 방향도 4방향으로 스냅
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

        yield return new WaitForSeconds(attackTime);

        // 데미지 + 플레이어 넉백 처리
        if (target != null && Vector2.Distance(transform.position, target.position) <= attackRange + 0.3f)
        {
            PlayerMovement player = target.GetComponent<PlayerMovement>();
            if (player != null)
            {
                // 몬스터 → 플레이어 방향으로 넉백
                Vector2 knockDir = (target.position - transform.position).normalized;
                player.TakeHit(knockDir);
            }
            Debug.Log($"몬스터가 플레이어에게 {damage} 데미지!");
        }

        anim.SetBool("IsAttacking", false);
        isAttacking = false;
        ChangeState(State.Idle);
    }

    // ===== 새로 추가: 몬스터 피격 처리 =====
    public void TakeDamage(int amount, Vector2 knockbackDirection)
    {
        hp -= amount;
        if (hp <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // 아직 피격 중이 아닐 때만 넉백
        if (!isHurt)
            StartCoroutine(HurtRoutine(knockbackDirection));
    }

    // 기존 TakeDamage 호환용 오버로드 (넉백 없이 데미지만)
    public void TakeDamage(int amount)
    {
        TakeDamage(amount, Vector2.zero);
    }

    IEnumerator HurtRoutine(Vector2 knockbackDirection)
    {
        isHurt = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // 넉백
        if (knockbackDirection != Vector2.zero && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(knockbackDuration);

        if (rb != null) rb.linearVelocity = Vector2.zero;

        isHurt = false;

        // 피격 후 다시 추적 재개
        if (currentState != State.Attack)
            ChangeState(State.Walk);
        else
            agent.isStopped = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}