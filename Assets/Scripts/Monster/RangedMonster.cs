/*
 * RangedMonster.cs
 * 역할: 플레이어 위치에 낙하 공격을 예고한 뒤 피해를 주고, 가까워지면 도망/순간이동하는 원거리 특수 몬스터입니다.
 * 연결: MonsterBase의 영속 상태, PlayerHealth 데미지, AudioManager 폭발 SFX, 경고/낙하/텔레포트 프리팹을 사용합니다.
 * 주의: 공격 코루틴과 사망 코루틴이 동시에 겹치지 않도록 isDead/isAttacking 체크를 유지해야 합니다.
 */using System.Collections;
using UnityEngine;

/// <summary>
/// 원거리 낙하 공격 몬스터.
/// MonsterBase에서 HP / 피격 / 사망 / HP바를 상속한다.
/// 플레이어가 가까이 오면 도망치고, 벽에 막히면 순간이동한다.
/// </summary>
public class RangedMonster : MonsterBase
{
    // 씬을 나갔다 돌아와도 HP/사망 상태 유지
    protected override bool PersistsStateAcrossScenes => true;

    [Header("공격 주기")]
    public float minCooldown = 2f; // 연속 공격 최소 간격 (초)
    public float maxCooldown = 5f; // 연속 공격 최대 간격 (초)

    [Header("텔레그래프")]
    // 공격 예고 원: 플레이어가 위험 범위를 시각적으로 인식하고 회피할 수 있도록 함
    public GameObject warningCirclePrefab;
    public float      warningDuration = 1f;  // 예고 원이 유지되는 시간 (회피 여유 시간)
    public float      impactRadius    = 1.2f; // 실제 피격 반경 (예고 원 크기와 일치시킬 것)
    public int        damage          = 10;

    [Header("낙하 연출")]
    // 공격 오브젝트가 위에서 떨어지는 연출 (쿼드라틱 이즈-인)
    public GameObject fallingObjectPrefab;
    public float      fallStartHeight = 6f;   // 낙하 시작 높이 (월드 단위, 목표 위치 기준)
    public float      fallDuration    = 0.35f; // 낙하 소요 시간 (초)

    [Header("도망")]
    public float fleeRange = 3f; // 이 거리 이내로 플레이어가 접근하면 도망
    public float fleeSpeed = 4f;

    [Header("순간이동")]
    public GameObject teleportEffectPrefab;
    public float      teleportMinDistance = 5f; // 순간이동 후 플레이어와의 최소 거리
    public LayerMask  groundLayer;              // 순간이동 착지 가능한 바닥 레이어

    [Header("레이어")]
    public LayerMask obstacleLayer; // 벽/장애물 레이어 (도망 방향 레이캐스트 + 순간이동 착지 검사)
    public LayerMask playerLayer;   // 낙하 충격 피격 판정 대상

    // ── 내부 상태 ──────────────────────────────────────────────────────
    private Transform    playerTransform;
    private PlayerHealth playerHealth;
    // isAttacking: 도망 시 TeleportAway 코루틴 진행 중 or DoAttack 진행 중
    // → 두 코루틴이 동시에 시작되지 않도록 막는 공유 플래그
    private bool         isAttacking = false;

    protected override void Start()
    {
        base.Start();
        FindPlayerInScene();
        // 공격 루프를 Start에서 한 번 시작 – 몬스터 생존 동안 계속 반복
        StartCoroutine(AttackLoop());
    }

    void Update()
    {
        if (playerTransform == null) { FindPlayerInScene(); return; }
        if (isDead || isAttacking) return; // 사망 중이거나 다른 코루틴 진행 중이면 이동 판단 skip

        // 플레이어가 fleeRange 이내이면 도망, 아니면 정지
        float dist = Vector2.Distance(playerTransform.position, transform.position);
        if (dist < fleeRange) Flee();
        else                  StopMoving();
    }

    // ── 이동 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 플레이어 반대 방향으로 4방향 도망.
    /// 도망 방향에 벽이 있으면 즉시 TeleportAway로 전환.
    /// </summary>
    void Flee()
    {
        // 플레이어 → 자신 방향 벡터 (도망 방향)
        Vector2 diff = (Vector2)transform.position - (Vector2)playerTransform.position;
        Vector2 moveDir = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y)
            ? new Vector2(diff.x > 0 ? 1 : -1, 0)
            : new Vector2(0, diff.y > 0 ? 1 : -1);

        // 도망 방향으로 3칸 앞에 벽이 있는지 레이캐스트 확인
        RaycastHit2D wallCheck = Physics2D.Raycast(transform.position, moveDir, 3f, obstacleLayer);
        Debug.DrawRay(transform.position, moveDir * 3f,
            wallCheck.collider != null ? Color.green : Color.red, 0.1f);

        if (wallCheck.collider != null)
        {
            // 벽에 막혀 더 도망갈 수 없으면 순간이동
            rb.linearVelocity = Vector2.zero;
            anim.SetBool("IsWalking", false);
            StartCoroutine(TeleportAway());
        }
        else
        {
            rb.linearVelocity = moveDir * fleeSpeed;
            anim.SetFloat("DirX", moveDir.x);
            anim.SetFloat("DirY", moveDir.y);
            anim.SetBool("IsWalking", true);
        }
    }

    void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("IsWalking", false);
    }

    /// <summary>
    /// 순간이동 코루틴:
    /// 1. 출발지에 이펙트 재생
    /// 2. 플레이어로부터 teleportMinDistance 이상 떨어진 빈 바닥 위치를 최대 30회 무작위 탐색
    /// 3. 적합한 위치를 찾으면 이동, 못 찾으면 제자리 유지
    /// 4. 도착지에 이펙트 재생 후 isAttacking 해제
    /// </summary>
    IEnumerator TeleportAway()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        SpawnEffect(teleportEffectPrefab, transform.position); // 출발 이펙트
        yield return new WaitForSeconds(0.3f);

        // 플레이어 주변 환형에서 무작위 후보 위치 탐색
        Vector2 newPos = transform.position; // 기본값: 이동 실패 시 제자리
        for (int i = 0; i < 30; i++)
        {
            // 플레이어 중심으로 teleportMinDistance ~ +4f 거리의 랜덤 위치
            Vector2 candidate = (Vector2)playerTransform.position
                + Random.insideUnitCircle.normalized
                  * Random.Range(teleportMinDistance, teleportMinDistance + 4f);

            // 조건: 장애물 없음 AND 바닥 위 (groundLayer 위에 있어야 함)
            bool noWall   = !Physics2D.OverlapCircle(candidate, 0.3f, obstacleLayer);
            bool onGround =  Physics2D.OverlapCircle(candidate, 0.3f, groundLayer);

            if (noWall && onGround) { newPos = candidate; break; }
        }

        transform.position = newPos;
        SpawnEffect(teleportEffectPrefab, transform.position); // 도착 이펙트

        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }

    // ── 공격 루프 ─────────────────────────────────────────────────────

    /// <summary>
    /// 공격 루프: 몬스터가 살아있는 동안 랜덤 간격으로 DoAttack을 반복 호출.
    /// AttackLoop와 TeleportAway는 isAttacking 플래그를 공유하지 않음 –
    /// TeleportAway는 Flee()에서 직접 시작하고 AttackLoop는 DoAttack만 담당.
    /// </summary>
    IEnumerator AttackLoop()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(Random.Range(minCooldown, maxCooldown));
            if (isDead) yield break;
            if (playerTransform == null) continue; // 플레이어를 찾지 못한 경우 이번 공격 skip
            yield return StartCoroutine(DoAttack());
        }
    }

    /// <summary>
    /// 낙하 공격 코루틴:
    /// 1. 플레이어 현재 위치를 목표로 잠금
    /// 2. 목표 위치에 경고 원 표시 (warningDuration 동안)
    /// 3. fallStartHeight 위에서 낙하 오브젝트를 목표까지 쿼드라틱 이즈-인으로 이동
    /// 4. 낙하 완료 후 explosion SFX + 피격 반경 안의 플레이어에게 데미지
    /// - 각 단계마다 isDead 체크 → 사망 시 즉시 중단 및 오브젝트 정리
    /// </summary>
    IEnumerator DoAttack()
    {
        if (isDead) yield break;

        isAttacking = true;
        StopMoving();

        // 공격 시작 시점의 플레이어 위치를 고정 (예고 → 낙하 동안 플레이어 이동 허용)
        Vector2 targetPos = playerTransform.position;

        // 목표가 장애물 내부이면 공격 취소 (이펙트/오브젝트가 벽 안에 생성되는 현상 방지)
        if (Physics2D.OverlapCircle(targetPos, 0.1f, obstacleLayer))
        {
            isAttacking = false;
            yield break;
        }

        anim.SetBool("IsAttacking", true);

        // ① 경고 원 표시
        GameObject warning = null;
        if (warningCirclePrefab != null)
        {
            warning = Instantiate(warningCirclePrefab, targetPos, Quaternion.identity);
            warning.transform.localScale = Vector3.one * impactRadius * 2f; // 피격 반경과 크기 일치
        }

        yield return new WaitForSeconds(warningDuration); // 플레이어에게 회피 시간 부여

        // 사망 체크 (경고 표시 중 사망할 수 있음)
        if (isDead)
        {
            if (warning != null) Destroy(warning);
            yield break;
        }
        if (warning != null) Destroy(warning);

        // ② 낙하 연출 (fallStartHeight → targetPos, 쿼드라틱 이즈-인 t*t)
        if (fallingObjectPrefab != null)
        {
            Vector2 startPos = targetPos + Vector2.up * fallStartHeight;
            GameObject fallingObj = Instantiate(fallingObjectPrefab, startPos, Quaternion.identity);

            float elapsed = 0f;
            while (elapsed < fallDuration)
            {
                if (isDead)
                {
                    if (fallingObj != null) Destroy(fallingObj);
                    yield break;
                }
                elapsed += Time.deltaTime;
                if (fallingObj == null) break; // 외부에서 삭제된 경우
                float t = elapsed / fallDuration;
                // t*t: 처음엔 느리게 시작해 빠르게 가속 (이즈-인 효과)
                fallingObj.transform.position = Vector2.Lerp(startPos, targetPos, t * t);
                yield return null;
            }

            // 낙하 완료 → 목표 위치에 고정 후 짧은 딜레이 뒤 제거
            if (fallingObj != null)
            {
                fallingObj.transform.position = targetPos;
                Destroy(fallingObj, 0.1f);
            }
        }
        else
        {
            yield return null; // 낙하 프리팹 없으면 1프레임 대기 후 즉시 피격 판정
        }

        // ③ 피격 판정
        if (isDead) yield break;

        AudioManager.Instance?.PlaySFX("explosion"); // 폭발 효과음

        // 반경 안에 있는 플레이어에게 데미지 (단일 플레이어 → OverlapCircle로 충분)
        Collider2D hit = Physics2D.OverlapCircle(targetPos, impactRadius, playerLayer);
        if (hit != null)
        {
            PlayerHealth ph = hit.GetComponentInParent<PlayerHealth>();
            ph?.TakeDamage(damage);
        }

        anim.SetBool("IsAttacking", false);
        isAttacking = false;
    }

    // ── 유틸 ──────────────────────────────────────────────────────────

    void FindPlayerInScene()
    {
        PlayerHealth ph = FindPlayerHealth(); // MonsterBase 유틸
        if (ph != null)
        {
            playerTransform = ph.transform;
            playerHealth    = ph;
        }
    }

    // ── 에디터 기즈모 ─────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // 빨간색: 낙하 공격 피격 반경 / 노란색: 도망 시작 거리
        Gizmos.color = Color.red;
        if (playerTransform != null)
            Gizmos.DrawWireSphere(playerTransform.position, impactRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fleeRange);
    }
}

