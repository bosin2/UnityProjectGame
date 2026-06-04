/*
 * PlayerBullet.cs
 * 역할: 총알 이동, 충돌 판정, 몬스터 데미지 전달, 명중 이펙트 생성을 담당합니다.
 * 연결: PlayerCombat.FireBullet에서 생성되며 MonsterBase.TakeDamage와 충돌 레이어 설정에 의존합니다.
 * 주의: 총알 수명과 충돌 후 Destroy 처리를 유지해야 씬에 불필요한 탄 오브젝트가 쌓이지 않습니다.
 */using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    // PlayerCombat.FireBullet()에서 생성 직후 외부에서 값을 설정하므로 HideInInspector
    [HideInInspector] public Vector2 direction = Vector2.down;
    [HideInInspector] public float speed = 12f;
    [HideInInspector] public int damage = 30;
    [HideInInspector] public GameObject hitEffectPrefab;

    private Rigidbody2D rb;
    // Rigidbody2D가 있으면 물리 기반 이동, 없으면 Transform.Translate 폴백
    private bool usePhysics;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;          // 2D 탑다운 – 중력 불필요
            rb.linearVelocity = direction * speed; // 초기 속도 설정 후 물리엔진이 유지
            usePhysics = true;
        }
    }

    void Update()
    {
        // Rigidbody2D가 없는 경우 매 프레임 직접 이동 (물리 없이도 동작 보장)
        if (!usePhysics)
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// 충돌 우선순위:
    /// 1) 몬스터 → 데미지 + 이펙트 + 총알 제거
    /// 2) NPC (gunNPC) → GameFlowManager 이벤트 + 총알 제거
    /// 3) 비-트리거 콜라이더(벽/바닥) → 이펙트 + 총알 제거
    /// 플레이어·플레이어 공격 히트박스는 무시 (자기 총알에 맞지 않도록)
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("PlayerAttack")) return;

        // 1) 몬스터 타격
        MonsterBase monster = other.GetComponentInParent<MonsterBase>();
        if (monster != null)
        {
            monster.TakeDamage(damage);
            SpawnHitEffect();
            Destroy(gameObject);
            return;
        }

        // 2) gunNPC 타격 (인트로 이벤트용 특수 NPC)
        if (other.CompareTag("NPC"))
        {
            SpawnHitEffect();
            GameFlowManager gfm = FindFirstObjectByType<GameFlowManager>();
            gfm?.OnGunNPCHit();
            Destroy(gameObject);
            return;
        }

        // 3) 벽/바닥 (isTrigger=false인 일반 콜라이더)
        // isTrigger=true인 오브젝트(아이템, 이벤트 존 등)는 통과
        if (!other.isTrigger)
        {
            SpawnHitEffect();
            Destroy(gameObject);
        }
    }

    void SpawnHitEffect()
    {
        if (hitEffectPrefab != null)
            // 0.5초 후 자동 제거 – 이펙트 오브젝트가 씬에 쌓이는 것을 방지
            Destroy(Instantiate(hitEffectPrefab, transform.position, Quaternion.identity), 0.5f);
    }
}
