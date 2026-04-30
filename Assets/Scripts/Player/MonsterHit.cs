using UnityEngine;

public class MonsterHitbox : MonoBehaviour
{
    private MonsterAI monster;

    void Start()
    {
        // 부모 오브젝트에서 MonsterAI 가져오기
        monster = GetComponentInParent<MonsterAI>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return; // 플레이어 콜라이더 무시

        // 공격 콜라이더에 닿으면 데미지
        if (other.CompareTag("PlayerAttack"))
        {
            PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
            if (player != null && monster != null)
                monster.TakeDamage(player.melee_damage);
                Debug.Log("몬스터가 플레이어의 공격에 맞았습니다! 데미지: " + player.melee_damage);
        }
    }
}