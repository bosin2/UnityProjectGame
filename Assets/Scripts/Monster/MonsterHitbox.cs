using UnityEngine;

// 몬스터의 피격 감지 히트박스 컴포넌트 (부모 MonsterAI에 데미지 전달)
public class MonsterHitbox : MonoBehaviour
{
    private MonsterAI monster;

    void Start()
    {
        // 부모 오브젝트에서 MonsterAI 컴포넌트 획득
        monster = GetComponentInParent<MonsterAI>();
    }

    // PlayerAttack 태그의 콜라이더에 닿으면 플레이어 근접 데미지를 MonsterAI에 전달
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;

        if (other.CompareTag("PlayerAttack"))
        {
            PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
            if (player != null && monster != null)
            {
                monster.TakeDamage(player.melee_damage);
            }
        }
    }
}
