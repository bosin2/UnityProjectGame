using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    [HideInInspector] public Vector2 direction = Vector2.down;
    [HideInInspector] public float speed = 12f;
    [HideInInspector] public int damage = 30;
    [HideInInspector] public GameObject hitEffectPrefab;

    private Rigidbody2D rb;
    private bool usePhysics;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = direction * speed;
            usePhysics = true;
        }
    }

    void Update()
    {
        if (!usePhysics)
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

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

        // 2) gunNPC 타격
        if (other.CompareTag("NPC"))
        {
            SpawnHitEffect();
            GameFlowManager gfm = FindFirstObjectByType<GameFlowManager>();
            gfm?.OnGunNPCHit();
            Destroy(gameObject);
            return;
        }

        // 3) 벽/바닥
        if (!other.isTrigger)
        {
            SpawnHitEffect();
            Destroy(gameObject);
        }
    }

    void SpawnHitEffect()
    {
        if (hitEffectPrefab != null)
            Destroy(Instantiate(hitEffectPrefab, transform.position, Quaternion.identity), 0.5f);
    }
}