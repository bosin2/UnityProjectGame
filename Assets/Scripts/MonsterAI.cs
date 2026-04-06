using UnityEngine;
using System.Collections; // Coroutine 사용을 위해 필요

public class EnhancedMonsterAI : MonoBehaviour
{
    // --- 스탯 및 설정 (기본값) ---
    [Header("Stats")]
    public int hp = 60;
    public int damage = 15;
    public float speed = 2f;

    [Header("Ranges")]
    public float detectionRange = 10f; // 인지 범위
    public float attackRange = 4f;    // 공격 범위 (1칸보다 살짝 크게 설정 추천)
    public float rayDistance = 0.1f;   // 벽 감지 거리

    [Header("Timers")]
    public float attackTime = 1.0f;    // 공격 애니메이션 및 대기 시간

    [Header("Targets")]
    public Transform target;

    // --- 내부 컴포넌트 및 상태 ---
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    // 몬스터의 현재 행동 상태
    private enum State { Idle, Walk, Attack }
    private State currentState = State.Idle;

    private bool isAttacking = false;
    private Vector2 lastMoveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // 방향 전환(Flip)용

        // 타겟 자동 검색
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        // 초기 상태 설정
        ChangeState(State.Idle);
    }

    void Update()
    {
        if (target == null || isAttacking) return; // 공격 중이거나 타겟 없으면 Update 건너뜀

        float distanceToPlayer = Vector2.Distance(transform.position, target.position);

        // 거리별 상태 결정
        if (distanceToPlayer <= attackRange)
        {
            ChangeState(State.Attack);
        }
        else if (distanceToPlayer <= detectionRange)
        {
            ChangeState(State.Walk);
        }
        else
        {
            ChangeState(State.Idle);
        }
    }

    void FixedUpdate()
    {
        // Walk 상태일 때만 물리 이동 처리
        if (currentState == State.Walk && !isAttacking && target != null)
        {
            MoveAndAvoidWalls();
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // 정지
        }
    }

    // --- 상태 관리 및 애니메이션 통합 ---
    void ChangeState(State newState)
    {
        if (currentState == newState) return; // 같은 상태로 변경 시 무시

        currentState = newState;

        // 기존 테스트 코드의 StartIdle, StartWalk 등의 역할을 통합
        switch (currentState)
        {
            case State.Idle:
                anim.SetBool("IsWalking", false);
                anim.SetBool("IsAttacking", false);
                break;

            case State.Walk:
                anim.SetBool("IsWalking", true);
                anim.SetBool("IsAttacking", false);
                break;

            case State.Attack:
                // 공격 상태로 바뀌는 순간 코루틴 실행
                if (!isAttacking)
                {
                    StartCoroutine(AttackRoutine());
                }
                break;
        }
    }

    // 애니메이션 방향 파라미터 업데이트 (4방향만)
    void UpdateAnimatorDirection(Vector2 direction)
    {
        // 가장 강한 축을 기준으로 4방향만 정함
        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);

        float dirX = 0f;
        float dirY = 0f;

        if (absX > absY) // 좌우가 더 강하면
        {
            dirX = direction.x > 0 ? 1f : -1f;
            dirY = 0f;
        }
        else // 상하가 더 강하면
        {
            dirX = 0f;
            dirY = direction.y > 0 ? 1f : -1f;
        }

        anim.SetFloat("DirX", dirX);
        anim.SetFloat("DirY", dirY);
    }


    // ===== 몬스터 4방향 이동 및 장애물 회피 시스템 (상세 주석) =====

    // --- 이동 및 장애물 회피 함수 (4방향 전용) ---
    void MoveAndAvoidWalls()
    {
        // ============================================
        // 1단계: 플레이어 방향 계산
        // ============================================
        // normalized = 벡터의 길이를 1로 만듦 (방향만 유지, 거리 정보는 제거)
        // 예) 플레이어가 (5, 3) 거리에 있으면
        //     (5, 3) → (0.857, 0.514) 정도의 단위벡터로 변환
        Vector2 dir = (target.position - transform.position).normalized;

        // ============================================
        // 2단계: 방향을 4방향(상/하/좌/우)으로만 정제
        // ============================================
        // 예) (0.857, 0.514) → (1, 0) 즉 "오른쪽" 한 방향으로만 정함
        // GetFourDirection 함수가 이 변환을 담당
        Vector2 fourDirDir = GetFourDirection(dir);

        // ============================================
        // 3단계: 애니메이션 업데이트
        // ============================================
        // Animator의 DirX, DirY 파라미터를 업데이트
        // 올바른 방향의 애니메이션이 재생되도록 함
        // 예) fourDirDir = (1, 0)이면 "오른쪽으로 걷는" 애니메이션 재생
        UpdateAnimatorDirection(fourDirDir);

        // ============================================
        // 4단계: 몬스터 앞에 벽이 있는지 체크 (Raycast)
        // ============================================
        // Raycast = 시선의 광선을 쏴서 무엇에 부딪히는지 확인하는 물리 감지
        // 몬스터 위치에서 fourDirDir 방향으로 rayDistance(0.1f) 거리까지 검사
        // hit.collider = 부딪힌 오브젝트의 Collider 정보 (벽이면 저장됨)
        RaycastHit2D hit = Physics2D.Raycast(transform.position, fourDirDir, rayDistance);

        // ============================================
        // 5단계: 벽에 부딪혔는지 판단
        // ============================================
        // hit.collider != null = 뭔가 감지됨
        // CompareTag로 3가지 벽 태그 중 하나라도 있으면 "벽이다" 판단
        if (hit.collider != null && (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("OutLine") || hit.collider.CompareTag("Table")))
        {
            // 벽이 있을 때: 좌우 양쪽으로 우회 경로 계산
            // 90도 회전 공식 사용:
            // 왼쪽 90도: (-y, x)
            // 오른쪽 90도: (y, -x)
            // 예) 오른쪽(1,0) 방향이면
            //     side1 = (0, 1) = 위쪽
            //     side2 = (0, -1) = 아래쪽
            Vector2 side1 = new Vector2(-fourDirDir.y, fourDirDir.x);    // 왼쪽 우회 시도
            Vector2 side2 = new Vector2(fourDirDir.y, -fourDirDir.x);    // 오른쪽 우회 시도

            // 왼쪽이 뚫려있으면 (벽 없음) 왼쪽으로 이동
            if (!IsBlocked(side1))
                rb.linearVelocity = side1 * speed;
            // 왼쪽이 막혀있고 오른쪽이 뚫려있으면 오른쪽으로 이동
            else if (!IsBlocked(side2))
                rb.linearVelocity = side2 * speed;
            // 좌우 모두 막혀있으면 그냥 멈춤
            else
                rb.linearVelocity = Vector2.zero;
        }
        else
        {
            // 벽이 없으면 그냥 플레이어 방향으로 직진
            // rb.linearVelocity = 벡터 * 스칼라값
            // 예) (1, 0) * 2f = (2, 0) = 초당 2유닛씩 오른쪽으로 이동
            rb.linearVelocity = fourDirDir * speed;
        }
    }


    // === 벽 감지 함수 ===
    // 목적: 특정 방향이 벽으로 막혀있는지 확인 (불값 반환: true/false)
    bool IsBlocked(Vector2 direction)
    {
        // 주어진 방향으로 rayDistance(0.1f) 거리까지 벽이 있는지 체크
        // Raycast 다시 수행해서 해당 방향이 뚫려있는지 확인
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, rayDistance);

        // 벽이 있으면 true(막혀있음), 없으면 false(뚫려있음) 반환
        // 이 반환값으로 우회 방향을 선택함
        return hit.collider != null && (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("OutLine") || hit.collider.CompareTag("Table"));
    }


    // === 핵심 함수: 방향을 4방향으로 정제 ===
    // 목적: 대각선이 아닌 상/하/좌/우 정확히 4방향만 반환
    // 입력: normalized된 2D 벡터 (소수값)
    // 출력: 4방향 중 하나 (1, 0, -1로만 이루어진 벡터)
    Vector2 GetFourDirection(Vector2 direction)
    {
        // ============================================
        // 각 축의 강도 계산 (절댓값)
        // ============================================
        // 예) direction = (-0.857, 0.514)
        //     absX = 0.857 (X축 강도)
        //     absY = 0.514 (Y축 강도)
        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);

        // ============================================
        // 어느 축이 더 강한지 비교해서 결정
        // ============================================
        // X축 강도가 더 크면 = 좌우 움직임이 더 큼 = 좌우 방향 선택
        if (absX > absY)
        {
            // direction.x > 0이면 오른쪽(1, 0)
            // direction.x < 0이면 왼쪽(-1, 0)
            // Y값은 0으로 고정 (세로 성분 제거)
            return new Vector2(direction.x > 0 ? 1f : -1f, 0f);
        }
        else
        {
            // Y축 강도가 더 크거나 같음 = 상하 움직임이 더 큼 = 상하 방향 선택
            // direction.y > 0이면 위(0, 1)
            // direction.y < 0이면 아래(0, -1)
            // X값은 0으로 고정 (가로 성분 제거)
            return new Vector2(0f, direction.y > 0 ? 1f : -1f);
        }
    }

    // --- 실제 전투 로직 (코루틴) ---
    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero; // 공격 시 멈춤

        // 1. 플레이어 방향을 바라보게 애니메이션 설정
        Vector2 attackDir = (target.position - transform.position).normalized;
        UpdateAnimatorDirection(attackDir);

        // 2. 공격 애니메이션 재생
        anim.SetBool("IsWalking", false);
        anim.SetBool("IsAttacking", true);
        Debug.Log("몬스터: 공격 애니메이션 재생!");

        // 3. 공격 애니메이션 지속 시간만큼 대기 (이 시간 동안은 Idle/Walk로 안 바뀜)
        yield return new WaitForSeconds(attackTime);

        // 4. 데미지 판정 (대기 시간이 끝난 후에도 범위 안에 있는지 확인)
        if (target != null)
        {
            float distance = Vector2.Distance(transform.position, target.position);
            if (distance <= attackRange + 0.3f) // 약간의 오차 허용
            {
                Debug.Log($"몬스터: 플레이어에게 {damage} 데미지 부여!");

                // [중요] 플레이어 스크립트에 TakeDamage 메서드가 있어야 합니다.
                // target.GetComponent<PlayerStatus>()?.TakeDamage(damage); 
            }
        }

        // 5. 공격 상태 해제 및 초기화
        anim.SetBool("IsAttacking", false);
        isAttacking = false;
        ChangeState(State.Idle); // 공격 후 잠시 Idle 상태로
    }


    // --- 에디터 시각화 ---
    private void OnDrawGizmosSelected()
    {
        // 인지 범위 (노랑)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 공격 범위 (빨강)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}