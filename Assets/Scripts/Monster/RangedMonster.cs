using System.Collections;
using UnityEngine;

public class RangedMonster : MonoBehaviour
{
    [Header("참조")]
    public PlayerMovement player;          
    public GameObject warningCirclePrefab;  // 바닥 경고

    [Header("공격 주기 (랜덤 간격)")]
    public float minCooldown = 2f;
    public float maxCooldown = 5f;

    [Header("텔레그래프 설정")]
    public float warningDuration = 1f;      // 경고 원 → 낙하 시작까지 시간
    public float impactRadius = 1.2f;       // 데미지 판정 반경
    public int damage = 10;                 // 플레이어에게 줄 데미지

    [Header("낙하 연출")]
    public GameObject fallingObjectPrefab;  // 떨어지는 스프라이트
    public float fallStartHeight = 6f;      // 시작 높이
    public float fallDuration = 0.35f;      // 낙하에 걸리는 시간

    [Header("도망 설정")]
    public float fleeRange = 3f;            // 이 거리 안에 플레이어 들어오면 도망
    public float fleeSpeed = 4f;            // 도망 이동 속도

    [Header("순간이동 (벽에 닿으면 발동)")]
    public GameObject teleportEffectPrefab; // 사라질/나타날 때 이펙트
    public float teleportMinDistance = 5f;  // 순간이동 시 플레이어와 최소 거리
    public LayerMask groundLayer;

    [Header("레이어")]
    public LayerMask obstacleLayer;
    public LayerMask playerLayer;

    private Rigidbody2D rb;
    private Animator anim;
    private bool isAttacking = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        FindPlayerInSameScene();
        StartCoroutine(AttackLoop());
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayerInSameScene();
            return;
        }

        if (isAttacking) return;

        float dist = Vector2.Distance(player.transform.position, transform.position);

        if (dist < fleeRange)
            Flee();
        else
            StopMoving();
    }

    // 플레이어 반대 방향으로 4방향 도망, 벽에 닿으면 즉시 순간이동
    void Flee()
    {
        Vector2 diff = (Vector2)transform.position - (Vector2)player.transform.position;

        Vector2 moveDir;
        if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
            moveDir = new Vector2(diff.x > 0 ? 1 : -1, 0);
        else
            moveDir = new Vector2(0, diff.y > 0 ? 1 : -1);

        RaycastHit2D wallCheck = Physics2D.Raycast(
            transform.position, moveDir, 3f, obstacleLayer);

        //디버깅
        Debug.DrawRay(transform.position, moveDir * 3f,
            wallCheck.collider != null ? Color.green : Color.red, 0.1f);

        if (wallCheck.collider != null)
        {
            //순간이동
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
        anim.SetBool("IsWalking", false);

        if (teleportEffectPrefab != null)
        {
            GameObject fx = Instantiate(teleportEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 1f);
        }

        yield return new WaitForSeconds(0.3f);

        Vector2 newPos = transform.position;
        for (int i = 0; i < 30; i++)
        {
            Vector2 candidate = (Vector2)player.transform.position
                + Random.insideUnitCircle.normalized
                  * Random.Range(teleportMinDistance, teleportMinDistance + 4f);

            bool noWall = !Physics2D.OverlapCircle(candidate, 0.3f, obstacleLayer);
            bool onGround = Physics2D.OverlapCircle(candidate, 0.3f, groundLayer);

            if (noWall && onGround)
            {
                newPos = candidate;
                break;
            }
        }

        transform.position = newPos;

        if (teleportEffectPrefab != null)
        {
            GameObject fx = Instantiate(teleportEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 1f);
        }

        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }

    IEnumerator AttackLoop()
    {
        while (true)
        {
            float wait = Random.Range(minCooldown, maxCooldown);
            yield return new WaitForSeconds(wait);

            if (player == null) continue;

            yield return StartCoroutine(DoAttack());
        }
    }

    IEnumerator DoAttack()
    {
        isAttacking = true;

        StopMoving();

        Vector2 targetPos = player.transform.position;

        if (Physics2D.OverlapCircle(targetPos, 0.1f, obstacleLayer))
        {
            isAttacking = false;
            yield break;
        }

        // 공격 모션
        anim.SetBool("IsAttacking", true);

        GameObject warning = null;
        if (warningCirclePrefab != null)
        {
            warning = Instantiate(warningCirclePrefab, targetPos, Quaternion.identity);
            warning.transform.localScale = Vector3.one * impactRadius * 2f;
        }

        yield return new WaitForSeconds(warningDuration);

        if (warning != null)
        {
            Destroy(warning);
            warning = null;
        }

        if (fallingObjectPrefab != null)
        {
            Vector2 startPos = targetPos + Vector2.up * fallStartHeight;
            GameObject fallingObj = Instantiate(
                fallingObjectPrefab, startPos, Quaternion.identity);

            float elapsed = 0f;
            while (elapsed < fallDuration)
            {
                elapsed += Time.deltaTime;
                if (fallingObj == null) break;

                float t = elapsed / fallDuration;
                fallingObj.transform.position =
                    Vector2.Lerp(startPos, targetPos, t * t);
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

        Collider2D hit = Physics2D.OverlapCircle(targetPos, impactRadius, playerLayer);
        if (hit != null)
        {
            PlayerMovement pm = hit.GetComponentInParent<PlayerMovement>();
            if (pm != null) pm.TakeDamage(damage);
        }

        anim.SetBool("IsAttacking", false);
        isAttacking = false;
    }

    void FindPlayerInSameScene()
    {
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        foreach (PlayerMovement candidate in players)
        {
            if (!candidate.gameObject.activeInHierarchy) continue;

            bool isSameScene = candidate.gameObject.scene == gameObject.scene;
            bool isDontDestroyPlayer = candidate.gameObject.scene.name == "DontDestroyOnLoad";

            if (!isSameScene && !isDontDestroyPlayer) continue;

            player = candidate;
            return;
        }
    }

    // 디버그용: 데미지 반경(빨강) + 도망 트리거 범위(노랑)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (player != null)
            Gizmos.DrawWireSphere(player.transform.position, impactRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fleeRange);
    }


}