using UnityEngine;

/// <summary>
/// 플레이어 총알.
/// PlayerCombat.FireBullet()이 Instantiate 직후 direction / speed / damage 를 설정한다.
///
/// 이동 방식:
///   - Rigidbody2D가 있으면 → linearVelocity 로 이동 (물리 기반)
///   - Rigidbody2D가 없으면  → Transform.Translate 로 이동 (폴백)
/// 중력은 항상 0으로 강제해 위아래로 흘러내리지 않게 한다.
/// </summary>
public class PlayerBullet : MonoBehaviour
{
    // PlayerCombat이 Instantiate 직후 설정하는 값들
    [HideInInspector] public Vector2 direction = Vector2.down;
    [HideInInspector] public float   speed     = 12f;
    [HideInInspector] public int     damage    = 30;

    private Rigidbody2D rb;
    private bool        usePhysics;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale   = 0f;
            rb.linearVelocity = direction * speed;
            usePhysics        = true;
        }
    }

    void Update()
    {
        // Rigidbody2D가 없는 폴백: Transform으로 직접 이동
        if (!usePhysics)
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어 자신과 근접 히트박스는 무시
        if (other.CompareTag("Player") || other.CompareTag("PlayerAttack")) return;

        MonsterBase monster = other.GetComponentInParent<MonsterBase>();
        if (monster != null)
        {
            monster.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // 트리거가 아닌 콜라이더(벽, 바닥 등)에 닿으면 소멸
        if (!other.isTrigger)
            Destroy(gameObject);
    }
}
