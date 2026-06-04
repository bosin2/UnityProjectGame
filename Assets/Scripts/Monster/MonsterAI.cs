/*
 * MonsterAI.cs
 * 역할: 일반 몬스터의 순찰, 추적, 공격 범위 판단 등 기본 AI 행동을 담당합니다.
 * 연결: MonsterBase의 HP/피격/사망 처리와 PlayerHealth 피격/데미지 처리에 의존합니다.
 * 주의: 특수몹과 달리 기본 PersistsStateAcrossScenes는 false이므로 씬 재진입 시 일반 몬스터는 기본 배치로 돌아갑니다.
 */using System.Collections;
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
    public float detectionRange   = 18f;  // 이 반경 안에 들어오면 Walk(추적)으로 전환
    public float attackRange      = 1.2f; // 이 반경 안에 들어오면 Attack으로 전환
    public float attackCooldown   = 1.0f; // 공격 시작 ~ 타격 판정까지 대기 시간(애니메이션 동기화)

    [Header("접촉 데미지")]
    // 콜라이더가 맞닿아 있는 동안 매 contactDamageInterval초마다 추가로 데미지를 줌
    // AttackRoutine 공격과 별개 – 같이 걸리면 두 데미지가 동시에 들어갈 수 있음
    public float contactDamageInterval = 1.5f;

    [Header("축 전환 마진 (4방향 이동 부드럽게)")]
    // 이동 축 전환을 막는 히스테리시스 값.
    // 예: 현재 Horizontal로 이동 중일 때 absY > absX + 0.7f 가 될 때까지 Vertical로 바꾸지 않음.
    // 이 값이 0이면 대각선 거리에서 축이 계속 번갈아 전환되어 지그재그 이동이 발생함.
    [SerializeField] private float axisSwitchMargin = 0.7f;

    // ── 내부 상태 ──────────────────────────────────────────────────────
    private Transform   target;               // 추적 대상 (플레이어)
    private PlayerHealth targetHealth;        // 데미지 / 넉백 전달용

    // 3-상태 머신: Idle(대기) → Walk(추적) → Attack(공격)
    private enum State { Idle, Walk, Attack }
    private State currentState = State.Idle;
    private bool  isAttacking  = false;       // AttackRoutine 진행 중 여부 (중복 공격 방지)

    private float contactCooldown = 0f;       // OnCollisionStay2D 접촉 데미지 쿨다운 타이머

    // 4방향 이동 시 현재 이동 중인 축 (Horizontal / Vertical)
    private enum MoveAxis { None, Horizontal, Vertical }
    private MoveAxis currentAxis = MoveAxis.None;

    // ── 초기화 ──────────────────────────────────────────────────────

    protected override void Start()
    {
        base.Start(); // MonsterBase.Start() : HP 설정 + HP바 생성

        rb.gravityScale   = 0f;   // 2D 탑다운 – 중력 불필요
        rb.freezeRotation = true; // 물리 충돌로 인한 회전 방지

        FindPlayer();
        ChangeState(State.Idle);
    }

    /// <summary>씬에서 PlayerHealth를 찾아 target/targetHealth에 캐싱.</summary>
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

        // 접촉 데미지 쿨다운 감소
        if (contactCooldown > 0) contactCooldown -= Time.deltaTime;

        if (target == null) { FindPlayer(); return; }

        // DontDestroyOnLoad 플레이어가 다른 씬에 있을 때는 무시
        // (예: 플레이어가 씬 A → B로 이동했지만 이 몬스터는 A에 남은 경우)
        if (target.gameObject.scene.name != gameObject.scene.name
            && target.gameObject.scene.name != "DontDestroyOnLoad")
        {
            target       = null;
            targetHealth = null;
            rb.linearVelocity = Vector2.zero;
            ChangeState(State.Idle);
            return;
        }

        if (isAttacking) return; // 공격 코루틴이 진행 중이면 상태 전환 막음

        // 거리 기반 상태 전환
        float dist = Vector2.Distance(transform.position, target.position);
        if      (dist <= attackRange)     ChangeState(State.Attack);
        else if (dist <= detectionRange)  ChangeState(State.Walk);
        else                              ChangeState(State.Idle);
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // Walk 상태이면서 공격 중이 아닐 때만 이동
        if (currentState == State.Walk && !isAttacking && target != null)
            MoveInFourDirections();
        else
            rb.linearVelocity = Vector2.zero;
    }

    // ── 이동 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 플레이어를 향해 상하좌우 4방향으로 이동.
    /// 대각선 이동 없이 한 축만 사용하며, axisSwitchMargin(히스테리시스)으로
    /// 축 전환을 부드럽게 만들어 지그재그 떨림 현상을 방지한다.
    /// </summary>
    void MoveInFourDirections()
    {
        Vector2 diff = (Vector2)(target.position - transform.position);
        float absX = Mathf.Abs(diff.x);
        float absY = Mathf.Abs(diff.y);

        // 처음 이동 시 지배적인 축 결정
        if (currentAxis == MoveAxis.None)
            currentAxis = absX >= absY ? MoveAxis.Horizontal : MoveAxis.Vertical;
        // 현재 Horizontal 이동 중 → Y 거리가 (X + 마진) 초과하면 Vertical로 전환
        else if (currentAxis == MoveAxis.Horizontal && absY > absX + axisSwitchMargin)
            currentAxis = MoveAxis.Vertical;
        // 현재 Vertical 이동 중 → X 거리가 (Y + 마진) 초과하면 Horizontal로 전환
        else if (currentAxis == MoveAxis.Vertical   && absX > absY + axisSwitchMargin)
            currentAxis = MoveAxis.Horizontal;

        // 결정된 축 방향으로 단위 벡터 생성
        Vector2 moveDir = currentAxis == MoveAxis.Horizontal
            ? new Vector2(diff.x > 0 ? 1 : -1, 0)
            : new Vector2(0, diff.y > 0 ? 1 : -1);

        rb.linearVelocity = moveDir * speed;
        anim.SetFloat("DirX", moveDir.x);
        anim.SetFloat("DirY", moveDir.y);
    }

    // ── 상태 머신 ─────────────────────────────────────────────────────

    /// <summary>
    /// 상태를 전환하고 해당 상태 진입 동작을 수행.
    /// Attack은 중복 전환을 허용(연속 공격 시도)하지만 isAttacking으로 실제 코루틴은 한 번만 실행.
    /// </summary>
    void ChangeState(State newState)
    {
        // 같은 상태로 전환 시 무시 (단, Attack은 반복 진입 허용 – 코루틴 중복은 isAttacking으로 방지)
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
                // isAttacking이 false일 때만 코루틴 시작 (중복 공격 방지)
                if (!isAttacking) StartCoroutine(AttackRoutine());
                break;
        }
    }

    /// <summary>
    /// 공격 코루틴:
    /// 1. 공격 방향 설정 (4방향 스냅)
    /// 2. attackCooldown 대기 (공격 애니메이션 재생 시간과 맞춤)
    /// 3. 범위 내 플레이어에게 데미지 + 넉백
    /// 4. 상태를 Idle로 복귀 (다음 Update에서 거리 재판단)
    /// </summary>
    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        if (target == null) { isAttacking = false; ChangeState(State.Idle); yield break; }

        // 공격 방향을 4방향으로 스냅 (대각선 공격 없음)
        Vector2 rawDir = (target.position - transform.position).normalized;
        Vector2 attackDir = Mathf.Abs(rawDir.x) >= Mathf.Abs(rawDir.y)
            ? new Vector2(rawDir.x > 0 ? 1 : -1, 0)
            : new Vector2(0, rawDir.y > 0 ? 1 : -1);

        anim.SetFloat("DirX", attackDir.x);
        anim.SetFloat("DirY", attackDir.y);
        anim.SetBool("IsWalking",   false);
        anim.SetBool("IsAttacking", true);

        // 애니메이션이 실제로 타격하는 프레임까지 대기
        yield return new WaitForSeconds(attackCooldown);

        // 쿨다운 동안 플레이어가 범위 밖으로 나갔을 수도 있으므로 재확인 (0.5f 여유)
        if (target != null && targetHealth != null)
        {
            Vector2 toPlayer = target.position - transform.position;
            if (toPlayer.magnitude <= attackRange + 0.5f)
            {
                targetHealth.TakeHit(toPlayer.normalized); // 넉백 방향
                targetHealth.TakeDamage(damage);
            }
        }

        anim.SetBool("IsAttacking", false);
        isAttacking = false;
        ChangeState(State.Idle); // 다음 Update에서 거리에 따라 다시 상태 결정
    }

    // ── 접촉 데미지 ───────────────────────────────────────────────────

    /// <summary>
    /// 플레이어와 콜라이더가 맞닿아 있는 동안 일정 주기로 추가 접촉 데미지를 줌.
    /// 공격 범위 밖에서 모서리로 몬스터에 닿는 경우 등 비정상 근접 상황을 커버.
    /// </summary>
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
        // 노란색: 감지 범위 / 빨간색: 공격 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

