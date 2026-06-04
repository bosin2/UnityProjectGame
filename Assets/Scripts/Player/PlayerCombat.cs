/*
 * PlayerCombat.cs
 * 역할: 플레이어의 근접 공격, 총 공격, 무기 전환, 공격 애니메이션/히트박스 타이밍을 관리합니다.
 * 연결: PlayerMovement의 LastDir, PlayerHealth의 피격/사망 상태, GameManager의 무기 소지 플래그, PlayerBullet과 연결됩니다.
 * 주의: 씬 전환 시 진행 중인 공격 Invoke와 히트박스를 반드시 초기화해야 중복 데미지나 멈춘 공격 상태를 막을 수 있습니다.
 */using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerCombat : MonoBehaviour
{
    [Header("근접 공격")]
    public int meleeDamage = 20;
    public float meleeDuration = 0.6f;

    [Header("원거리 공격")]
    public int gunDamage = 30;
    public float gunDuration = 0.5f;
    public GameObject bulletPrefab;
    public GameObject hitEffectPrefab;
    public float bulletSpeed = 12f;

    [Header("근접 공격 판정 범위 (4방향 collider)")]
    public Collider2D hitboxUp;
    public Collider2D hitboxDown;
    public Collider2D hitboxLeft;
    public Collider2D hitboxRight;

    public bool IsAttacking => isAttacking;
    public int CurrentWeapon => currentWeapon;

    private bool isAttacking = false;
    private int currentWeapon = 0;

    private Animator anim;
    private PlayerMovement movement;
    private PlayerHealth health;
    private InventoryManager inventory;

    void Awake()
    {
        anim = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>();

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

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (GameManager.Instance != null && GameManager.Instance.hasGun)
                SwitchWeapon(currentWeapon == 0 ? 1 : 0);
        }

        if (Input.GetMouseButtonDown(0) &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            TryAttack();
    }

    void TryAttack()
    {
        if (isAttacking) return;

        if (currentWeapon == 0)
        {
            if (GameManager.Instance != null && !GameManager.Instance.hasPipe) return;
            isAttacking = true;
            anim.SetBool("IsAttacking", true);
            AudioManager.Instance?.PlaySFX("swing");
            Invoke(nameof(EndAttack), meleeDuration);
        }
        else
        {
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
        if (dir == Vector2.zero) dir = Vector2.down;

        Vector3 spawnPos = transform.position + (Vector3)(dir.normalized * 0.7f);
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        PlayerBullet pb = bullet.GetComponent<PlayerBullet>();
        if (pb != null)
        {
            pb.direction = dir.normalized;
            pb.speed = bulletSpeed;
            pb.damage = gunDamage;
            pb.hitEffectPrefab = hitEffectPrefab;
        }
        else
        {
            Rigidbody2D brb = bullet.GetComponent<Rigidbody2D>();
            if (brb != null)
            {
                brb.gravityScale = 0f;
                brb.linearVelocity = dir.normalized * bulletSpeed;
            }
        }

        AudioManager.Instance?.PlaySFX("gunhit");
        Destroy(bullet, 3f);
    }

    void EndAttack()
    {
        isAttacking = false;
        anim.SetBool("IsAttacking", false);
        DisableAllColliders();
    }

    public void CancelAttack()
    {
        if (!isAttacking) return;
        CancelInvoke(nameof(EndAttack));
        isAttacking = false;
        anim.SetBool("IsAttacking", false);
        DisableAllColliders();
    }

    public void SwitchWeapon(int weaponType)
    {
        currentWeapon = weaponType;
        anim.SetInteger("Weapon", weaponType);
        AudioManager.Instance?.PlaySFX("click");
    }

    public void OnMeleeHitStart()
    {
        DisableAllColliders();
        Vector2 dir = movement.LastDir;

        if (dir.y > 0.5f) { if (hitboxUp != null) hitboxUp.enabled = true; }
        else if (dir.y < -0.5f) { if (hitboxDown != null) hitboxDown.enabled = true; }
        else if (dir.x < -0.5f) { if (hitboxLeft != null) hitboxLeft.enabled = true; }
        else if (dir.x > 0.5f) { if (hitboxRight != null) hitboxRight.enabled = true; }
    }

    public void OnMeleeHitEnd()
    {
        DisableAllColliders();
    }

    void DisableAllColliders()
    {
        if (hitboxUp != null) hitboxUp.enabled = false;
        if (hitboxDown != null) hitboxDown.enabled = false;
        if (hitboxLeft != null) hitboxLeft.enabled = false;
        if (hitboxRight != null) hitboxRight.enabled = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        inventory = FindFirstObjectByType<InventoryManager>();
        CancelInvoke(nameof(EndAttack));
        isAttacking = false;
        DisableAllColliders();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

