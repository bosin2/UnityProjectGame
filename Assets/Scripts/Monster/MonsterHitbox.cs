/*
 * MonsterHitbox.cs
 * 역할: 플레이어 근접 공격 히트박스가 몬스터에게 닿았을 때 부모 MonsterBase에 피해를 전달합니다.
 * 연결: PlayerCombat의 방향별 근접 Collider와 MonsterBase.TakeMeleeDamage가 만나는 접점입니다.
 * 주의: 부모 MonsterBase를 찾지 못하면 데미지가 들어가지 않으므로 몬스터 계층 구조 변경 시 확인해야 합니다.
 */using UnityEngine;

/// <summary>
/// 몬스터의 피격 감지 히트박스.
/// 부모(또는 조상) 오브젝트의 MonsterBase에 데미지를 전달한다.
/// PlayerAttack 태그 콜라이더가 충돌했을 때만 반응.
/// </summary>
public class MonsterHitbox : MonoBehaviour
{
    // GetComponentInParent로 부모 계층의 MonsterBase를 찾음
    // (MonsterAI, RangedMonster 등 MonsterBase 상속 클래스 모두 호환)
    private MonsterBase monster;

    void Start()
    {
        monster = GetComponentInParent<MonsterBase>();

        if (monster == null)
            Debug.LogWarning("[MonsterHitbox] 부모에서 MonsterBase를 찾을 수 없습니다.", this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return; // 플레이어 본체는 무시
        if (!other.CompareTag("PlayerAttack")) return;

        // 공격 콜라이더의 부모에서 PlayerCombat을 찾아 데미지 수치를 가져옴
        PlayerCombat combat = other.GetComponentInParent<PlayerCombat>();
        if (combat != null && monster != null)
            monster.TakeMeleeDamage(combat.meleeDamage);
    }
}

