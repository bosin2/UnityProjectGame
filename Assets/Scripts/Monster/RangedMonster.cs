using System.Collections;
using UnityEngine;

/// <summary>
/// 원거리 낙하 공격 몬스터.
/// MonsterBase에서 HP / 피격 / 사망 / HP바를 상속한다.
/// 플레이어가 가까이 오면 도망치고, 벽에 막히면 순간이동한다.
/// </summary>
public class RangedMonster : MonsterBase
{
    protected override bool PersistsStateAcrossScenes => true;

    [Header("공격 주기")]
    public float minCooldown = 2f;
    public float maxCooldown = 5f;

    [Header("텔레그래프")]
    public GameObject warningCirclePrefab;
    public float      warningDuration = 1f;
    public float      impactRadius    = 1.2f;
    public int        damage          = 10;

    [Header("낙하 연출")]
    public GameObject fallingObjectPrefab;
    public float      fallStartHeight = 6f;
    public float      fallDuration    = 0.35f;

    [Header("도망")]
    public float fleeRange = 3f;
    public float fleeSpeed = 4f;

    [Header("순간이동")]
    public GameObject teleportEffectPrefab;
    public float      teleportMinDistance = 5f;
    public LayerMask  groundLayer;

    [Header("레이어")]
    public LayerMask obstacleLayer;
    public LayerMask playerLayer;

    // ── 내부 상태 ──────────────────────────────────────────────────────
    private Transform    playerTransform;
    private PlayerHealth playerHealth;
    private bool         isAttacking = false;

    protected override void Start()
    {
        base.Start();
        FindPlayerInScene();
        StartCoroutine(AttackLoop());
    }

    void Update()
    {
        if (playerTransform == null) { FindPlayerInScene(); return; }
        if (isDead || isAttacking) return;

        float dist = Vector2.Distance(playerTransform.position, transform.position);
        if (dist < fleeRange) Flee();
        else                  StopMoving();
    }

    // ── 이동 ──────────────────────────────────────────────────────────

    void Flee()
    {
        Vector2 diff = (Vector2)transform.position - (Vector2)playerTransform.position;
        Vector2 moveDir = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y)
            ? new Vector2(diff.x > 0 ? 1 : -1, 0)
            : new Vector2(0, diff.y > 0 ? 1 : -1);

        RaycastHit2D wallCheck = Physics2D.Raycast(transform.position, moveDir, 3f, obstacleLayer);
        Debug.DrawRay(transform.position, moveDir * 3f,
            wallCheck.collider != null ? Color.green : Color.red, 0.1f);

        if (wallCheck.collider != null)
        {
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

    IEnumerator TeleportAway()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        SpawnEffect(teleportEffectPrefab, transform.position);
        yield return new WaitForSeconds(0.3f);

        // 플레이어로부터 일정 거리 이상인 빈 위치 탐색
        Vector2 newPos = transform.position;
        for (int i = 0; i < 30; i++)
        {
            Vector2 candidate = (Vector2)playerTransform.position
                + Random.insideUnitCircle.normalized
                  * Random.Range(teleportMinDistance, teleportMinDistance + 4f);

            bool noWall   = !Physics2D.OverlapCircle(candidate, 0.3f, obstacleLayer);
            bool onGround =  Physics2D.OverlapCircle(candidate, 0.3f, groundLayer);

            if (noWall && onGround) { newPos = candidate; break; }
        }

        transform.position = newPos;
        SpawnEffect(teleportEffectPrefab, transform.position);

        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }

    // ── 공격 루프 ─────────────────────────────────────────────────────

    IEnumerator AttackLoop()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(Random.Range(minCooldown, maxCooldown));
            if (isDead) yield break;
            if (playerTransform == null) continue;
            yield return StartCoroutine(DoAttack());
        }
    }

    IEnumerator DoAttack()
    {
        if (isDead) yield break;

        isAttacking = true;
        StopMoving();

        Vector2 targetPos = playerTransform.position;

        // 목표 위치가 장애물이면 공격 취소
        if (Physics2D.OverlapCircle(targetPos, 0.1f, obstacleLayer))
        {
            isAttacking = false;
            yield break;
        }

        anim.SetBool("IsAttacking", true);

        // 경고 원 표시
        GameObject warning = null;
        if (warningCirclePrefab != null)
        {
            warning = Instantiate(warningCirclePrefab, targetPos, Quaternion.identity);
            warning.transform.localScale = Vector3.one * impactRadius * 2f;
        }

        yield return new WaitForSeconds(warningDuration);
        if (isDead)
        {
            if (warning != null) Destroy(warning);
            yield break;
        }
        if (warning != null) Destroy(warning);

        // 낙하 연출
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
                if (fallingObj == null) break;
                float t = elapsed / fallDuration;
                fallingObj.transform.position = Vector2.Lerp(startPos, targetPos, t * t);
                yield return null;
            }

            if (fallingObj != null)
            {
                fallingObj.transform.position = targetPos;
                Destroy(fallingObj, 0.1f);
            }
        }
        else
        {
            yield return null;
        }

        // 피격 판정
        if (isDead) yield break;

        AudioManager.Instance?.PlaySFX("explosion");

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
        Gizmos.color = Color.red;
        if (playerTransform != null)
            Gizmos.DrawWireSphere(playerTransform.position, impactRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fleeRange);
    }
}
