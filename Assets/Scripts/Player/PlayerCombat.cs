using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이어 공격(근접/원거리)과 무기 전환을 담당.
/// [같은 GameObject] PlayerMovement(이동), PlayerHealth(HP)와 함께 부착.
///
/// [Inspector 연결 필요]
/// hitboxUp/Down/Left/Right : 4방향 근접 히트박스 Collider2D
/// bulletPrefab             : 총알 프리팹 (원거리 공격용)
///
/// [애니메이션 이벤트 연결 필요]
/// OnMeleeHitStart / OnMeleeHitEnd → 이 컴포넌트(PlayerCombat)에 연결
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerCombat : MonoBehaviour
{
    [Header("근접 공격")]
    public int   meleeDamage   = 20;
    public float meleeDuration = 0.4f;   // 공격 상태 유지 시간

    [Header("원거리 공격")]
    public int        gunDamage    = 30;
    public float      gunDuration  = 0.5f;
    public GameObject bulletPrefab;
    public float      bulletSpeed  = 12f;

    [Header("근접 히트박스 (4방향 Collider2D)")]
    public Collider2D hitboxUp;
    public Collider2D hitboxDown;
    public Collider2D hitboxLeft;
    public Collider2D hitboxRight;

    // ── 프로퍼티 ──────────────────────────────────────────────────────
    public bool IsAttacking   => isAttacking;
    public int  CurrentWeapon => currentWeapon;

    // ── 내부 상태 ──────────────────────────────────────────────────────
    private bool  isAttacking  = false;
    private int   currentWeapon = 0;    // 0 = pipe(근접), 1 = gun(원거리)

    private Animator       anim;
    private PlayerMovement movement;
    private PlayerHealth   health;

    private InventoryManager inventory;

    void Awake()
    {
        anim     = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        health   = GetComponent<PlayerHealth>();

        DisableAllColliders();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        inventory = FindFirstObjectByType<InventoryManager>();
    }

    void Update()
    {
        if (health.IsDead || health.IsHurt) return;
        if (inventory != null && inventory.IsOpen) return;

        if (Input.GetMouseButtonDown(0))
            TryAttack();
    }

    // ── 공격 시작 ─────────────────────────────────────────────────────

    void TryAttack()
    {
        if (isAttacking) return;

        if (currentWeapon == 0)
        {
            // 파이프 근접 공격
            if (GameManager.Instance != null && !GameManager.Instance.hasPipe) return;
            isAttacking = true;
            anim.SetBool("IsAttacking", true);
            AudioManager.Instance?.PlaySFX("swing");
            Invoke(nameof(EndAttack), meleeDuration);
        }
        else
        {
            // 총 원거리 공격
            if (GameManager.Instance != null && !GameManager.Instance.hasGun) return;
            isAttacking = true;
            anim.SetBool("IsAttacking", true);
            FireBullet();
            Invoke(nameof(EndAttack), gunDuration);
        }
    }

    void FireBullet()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("[PlayerCombat] bulletPrefab이 Inspector에서 연결되지 않았습니다!", this);
            return;
        }

        Vector2 dir = movement.LastDir;
        if (dir == Vector2.zero) dir = Vector2.down; // 방향이 없으면 아래 기본값

        // 플레이어 앞쪽에서 스폰 (플레이어 콜라이더와 겹치지 않도록 오프셋)
        Vector3 spawnPos = transform.position + (Vector3)(dir.normalized * 0.7f);
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        // ── PlayerBullet에 방향·속도·데미지 전달 ─────────────────────
        // Instantiate 직후 Start()가 실행되기 전에 값을 설정 → Start()가 이 값으로 velocity 적용
        PlayerBullet pb = bullet.GetComponent<PlayerBullet>();
        if (pb != null)
        {
            pb.direction = dir.normalized;
            pb.speed     = bulletSpeed;
            pb.damage    = gunDamage;
        }
        else
        {
            // PlayerBullet이 없어도 Rigidbody2D로 최소한 이동
            Rigidbody2D brb = bullet.GetComponent<Rigidbody2D>();
            if (brb != null)
            {
                brb.gravityScale   = 0f;
                brb.linearVelocity = dir.normalized * bulletSpeed;
            }
        }

        AudioManager.Instance?.PlaySFX("gun");
        Destroy(bullet, 3f);
    }

    // ── 공격 종료 ─────────────────────────────────────────────────────

    void EndAttack()
    {
        isAttacking = false;
        anim.SetBool("IsAttacking", false);
        DisableAllColliders();
    }

    /// <summary>피격 시 PlayerHealth가 호출 — 진행 중인 공격을 즉시 취소</summary>
    public void CancelAttack()
    {
        if (!isAttacking) return;
        CancelInvoke(nameof(EndAttack));
        isAttacking = false;
        anim.SetBool("IsAttacking", false);
        DisableAllColliders();
    }

    // ── 무기 전환 ─────────────────────────────────────────────────────

    /// <summary>무기 전환 (0=pipe, 1=gun). GameFlowManager에서 호출</summary>
    public void SwitchWeapon(int weaponType)
    {
        currentWeapon = weaponType;
        anim.SetInteger("Weapon", weaponType);
        AudioManager.Instance?.PlaySFX("click");
    }

    // ── 애니메이션 이벤트 ─────────────────────────────────────────────
    // Animator에서 공격 애니메이션의 적절한 프레임에 이 컴포넌트의 메서드를 연결

    /// <summary>공격 판정 시작 (히트박스 활성화)</summary>
    public void OnMeleeHitStart()
    {
        DisableAllColliders();
        Vector2 dir = movement.LastDir;

        if      (dir.y >  0.5f)  { if (hitboxUp    != null) hitboxUp.enabled    = true; }
        else if (dir.y < -0.5f)  { if (hitboxDown  != null) hitboxDown.enabled  = true; }
        else if (dir.x < -0.5f)  { if (hitboxLeft  != null) hitboxLeft.enabled  = true; }
        else if (dir.x >  0.5f)  { if (hitboxRight != null) hitboxRight.enabled = true; }
    }

    /// <summary>공격 판정 종료 (히트박스 비활성화)</summary>
    public void OnMeleeHitEnd()
    {
        DisableAllColliders();
    }

    // ── 히트박스 전체 비활성화 ────────────────────────────────────────

    void DisableAllColliders()
    {
        if (hitboxUp    != null) hitboxUp.enabled    = false;
        if (hitboxDown  != null) hitboxDown.enabled  = false;
        if (hitboxLeft  != null) hitboxLeft.enabled  = false;
        if (hitboxRight != null) hitboxRight.enabled = false;
    }

    // ── 씬 전환 처리 ──────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 바뀌면 새 씬의 InventoryManager를 다시 찾음
        inventory = FindFirstObjectByType<InventoryManager>();
        // 공격 상태 초기화
        CancelInvoke(nameof(EndAttack));
        isAttacking = false;
        DisableAllColliders();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
