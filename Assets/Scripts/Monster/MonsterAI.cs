using System.Collections;
using UnityEngine;

/// <summary>
/// 기본 몬스터 AI: Idle → Walk(감지) → Attack 상태 머신.
/// MonsterBase에서 HP / 피격 / 사망 / HP바를 상속한다.
/// </summary>
public class MonsterAI : MonsterBase
{
    [Header("스탯")]
    public int   damage = 15;
    public float speed  = 2f;

    [Header("감지 / 공격 범위")]
    public float detectionRange   = 18f;
    public float attackRange      = 1.2f;
    public float attackCooldown   = 1.0f;

    [Header("접촉 데미지")]
    public float contactDamageInterval = 1.5f;

    [Header("축 전환 마진 (4방향 이동 부드럽게)")]
    [SerializeField] private float axisSwitchMargin = 0.7f;

    // ── 내부 상태 ──────────────────────────────────────────────────────
    private Transform   target;               // 추적 대상 (플레이어)
    private PlayerHealth targetHealth;        // 데미지 / 넉백 전달용

    private enum State { Idle, Walk, Attack }
    private State currentState = State.Idle;
    private bool  isAttacking  = false;

    private float contactCooldown = 0f;

    private enum MoveAxis { None, Horizontal, Vertical }
    private MoveAxis currentAxis = MoveAxis.None;

    // ── 초기화 ──────────────────────────────────────────────────────

    protected override void Start()
    {
        base.Start(); // MonsterBase.Start() : HP 설정 + HP바 생성

        rb.gravityScale   = 0f;
        rb.freezeRotation = true;

        FindPlayer();
        ChangeState(State.Idle);
    }

    void FindPlayer()
    {
        if (target != null) return;
        PlayerHealth ph = FindPlayerHealth(); // MonsterBase 유틸
        if (ph != null)
        {
            target       = ph.transform;
            targetHealth = ph;
        }
    }

    // ── Update / FixedUpdate ──────────────────────────────────────────

    void Update()
    {
        if (isDead) return;

        if (contactCooldown > 0) contactCooldown -= Time.deltaTime;

        if (target == null) { FindPlayer(); return; }

        // 씬이 다른 플레이어는 무시
        if (target.gameObject.scene.name != gameObject.scene.name
            && target.gameObject.scene.name != "DontDestroyOnLoad")
        {
            target       = null;
            targetHealth = null;
            rb.linearVelocity = Vector2.zero;
            ChangeState(State.Idle);
            return;
        }

        if (isAttacking) return;

        float dist = Vector2.Distance(transform.position, target.position);
        if      (dist <= attackRange)     ChangeState(State.Attack);
        else if (dist <= detectionRange)  ChangeState(State.Walk);
        else                              ChangeState(State.Idle);
    }

    void FixedUpdate()
    {
        if (isDead) return;

        if (currentState == State.Walk && !isAttacking && target != null)
            MoveInFourDirections();
        else
            rb.linearVelocity = Vector2.zero;
    }

    // ── 이동 ──────────────────────────────────────────────────────────

    void MoveInFourDirections()
    {
        Vector2 diff = (Vector2)(target.position - transform.position);
        float absX = Mathf.Abs(diff.x);
        float absY = Mathf.Abs(diff.y);

        if (currentAxis == MoveAxis.None)
            currentAxis = absX >= absY ? MoveAxis.Horizontal : MoveAxis.Vertical;
        else if (currentAxis == MoveAxis.Horizontal && absY > absX + axisSwitchMargin)
            currentAxis = MoveAxis.Vertical;
        else if (currentAxis == MoveAxis.Vertical   && absX > absY + axisSwitchMargin)
            currentAxis = MoveAxis.Horizontal;

        Vector2 moveDir = currentAxis == MoveAxis.Horizontal
            ? new Vector2(diff.x > 0 ? 1 : -1, 0)
            : new Vector2(0, diff.y > 0 ? 1 : -1);

        rb.linearVelocity = moveDir * speed;
        anim.SetFloat("DirX", moveDir.x);
        anim.SetFloat("DirY", moveDir.y);
    }

    // ── 상태 머신 ─────────────────────────────────────────────────────

    void ChangeState(State newState)
    {
        if (currentState == newState && newState != State.Attack) return;
        currentState = newState;

        switch (currentState)
        {
            case State.Idle:
                rb.linearVelocity = Vector2.zero;
                anim.SetBool("IsWalking", false);
                break;
            case State.Walk:
                anim.SetBool("IsWalking", true);
                anim.SetBool("IsAttacking", false);
                break;
            case State.Attack:
                if (!isAttacking) StartCoroutine(AttackRoutine());
                break;
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        if (target == null) { isAttacking = false; ChangeState(State.Idle); yield break; }

        // 공격 방향 설정
        Vector2 rawDir = (target.position - transform.position).normalized;
        Vector2 attackDir = Mathf.Abs(rawDir.x) >= Mathf.Abs(rawDir.y)
            ? new Vector2(rawDir.x > 0 ? 1 : -1, 0)
            : new Vector2(0, rawDir.y > 0 ? 1 : -1);

        anim.SetFloat("DirX", attackDir.x);
        anim.SetFloat("DirY", attackDir.y);
        anim.SetBool("IsWalking",   false);
        anim.SetBool("IsAttacking", true);

        yield return new WaitForSeconds(attackCooldown);

        // 범위 내 플레이어에게 데미지 + 넉백
        if (target != null && targetHealth != null)
        {
            Vector2 toPlayer = target.position - transform.position;
            if (toPlayer.magnitude <= attackRange + 0.5f)
            {
                targetHealth.TakeHit(toPlayer.normalized);
                targetHealth.TakeDamage(damage);
            }
        }

        anim.SetBool("IsAttacking", false);
        isAttacking = false;
        ChangeState(State.Idle);
    }

    // ── 접촉 데미지 ───────────────────────────────────────────────────

    void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        if (contactCooldown > 0) return;

        PlayerHealth ph = collision.gameObject.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            Vector2 knockDir = (collision.transform.position - transform.position).normalized;
            ph.TakeHit(knockDir);
            ph.TakeDamage(damage);
            contactCooldown = contactDamageInterval;
        }
    }

    // ── 에디터 기즈모 ─────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
