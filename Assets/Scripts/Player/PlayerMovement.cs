using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이어 이동만 담당.
/// 공격 → PlayerCombat  /  HP·피격·사망 → PlayerHealth
/// 씬 전환 후에도 유지되므로 DontDestroyOnLoad 적용.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerCombat))]
public class PlayerMovement : MonoBehaviour
{
    [Header("이동 속도")]
    public float moveSpeed = 5f;

    // Inspector 기본값 (절대 변경 안 함)
    private float _baseMoveSpeed;
    // 장비(신발 등)로 인한 영구 보너스 (씬을 넘어 유지)
    private float _equipSpeedBonus = 0f;

    [Header("발소리")]
    public float footstepInterval = 0.2f;

    // ── 프로퍼티 ──────────────────────────────────────────────────────
    /// <summary>마지막 이동 방향 (공격/피격 방향 계산에 사용)</summary>
    public Vector2 LastDir => lastDir;

    /// <summary>Inspector 설정 기본 속도 (장비 보너스 계산용)</summary>
    public float BaseSpeed => _baseMoveSpeed;

    /// <summary>
    /// 장비(신발 등) 속도 보너스를 절댓값으로 설정.
    /// 누적이 아닌 "현재 장비의 보너스" 로 덮어씀 → 씬을 넘어도 안전.
    /// </summary>
    public void SetEquipSpeedBonus(float bonus)
    {
        _equipSpeedBonus = bonus;
        moveSpeed        = _baseMoveSpeed + _equipSpeedBonus;
    }

    /// <summary>실제로 이동 중인지 여부 (발소리·애니메이션 제어용)</summary>
    public bool IsMoving => movement != Vector2.zero
                         && !combat.IsAttacking
                         && !health.IsHurt
                         && !health.IsDead;

    // ── 내부 상태 ──────────────────────────────────────────────────────
    private Vector2 movement;
    private Vector2 lastDir = new Vector2(0, -1);
    private float   footstepTimer;

    private Rigidbody2D rb;
    private Animator    anim;
    private PlayerHealth   health;
    private PlayerCombat   combat;
    private InventoryManager inventory; // 인벤토리 열림 여부 확인용

    // 씬 전환 시 두 번째 플레이어가 생기는 것을 막는 플래그 (public Instance 없음)
    private static bool _exists;
    private bool _isOriginal = false; // 이 인스턴스가 진짜 원본인지 여부

    void Awake()
    {
        if (_exists)
        {
            // 복제본은 그냥 소멸 — OnDestroy에서 _exists를 건드리지 않도록 _isOriginal을 false로 둠
            Destroy(gameObject);
            return;
        }

        _exists        = true;
        _isOriginal    = true;
        _baseMoveSpeed = moveSpeed;  // Inspector 기본값 저장 (이 값은 절대 바뀌지 않음)
        DontDestroyOnLoad(gameObject);

        rb     = GetComponent<Rigidbody2D>();
        anim   = GetComponent<Animator>();
        health = GetComponent<PlayerHealth>();
        combat = GetComponent<PlayerCombat>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        inventory = FindFirstObjectByType<InventoryManager>();
    }

    void Update()
    {
        if (health.IsDead) return;
        if (inventory != null && inventory.IsOpen) return;

        HandleMovementInput();
        UpdateAnimator();
        HandleFootstepSound();
    }

    void FixedUpdate()
    {
        if (!combat.IsAttacking && !health.IsHurt && !health.IsDead)
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    // ── 이동 처리 ──────────────────────────────────────────────────────

    void HandleMovementInput()
    {
        // 공격/피격 중 이동 불가
        if (combat.IsAttacking || health.IsHurt)
        {
            movement = Vector2.zero;
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        // 대각선 이동 방지: x 우선
        if      (x != 0) movement = new Vector2(x, 0);
        else if (y != 0) movement = new Vector2(0, y);
        else             movement = Vector2.zero;

        if (movement != Vector2.zero)
            lastDir = movement;
    }

    void UpdateAnimator()
    {
        anim.SetFloat("DirX", lastDir.x);
        anim.SetFloat("DirY", lastDir.y);
        anim.SetBool("IsWalking", movement != Vector2.zero
                                  && !combat.IsAttacking
                                  && !health.IsHurt);
    }

    void HandleFootstepSound()
    {
        if (!IsMoving) { footstepTimer = 0f; return; }

        footstepTimer += Time.deltaTime;
        if (footstepTimer >= footstepInterval)
        {
            // AudioManager.Instance?.PlaySFX("walk");
            footstepTimer = 0f;
        }
    }

    // ── 씬 전환 처리 ──────────────────────────────────────────────────

    // 씬 로드 후 SceneLoader에 저장된 스폰 데이터를 읽어 위치와 방향 적용
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ① 이전 씬의 잔여 속도 초기화
        movement = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        // ② 임시 속도 부스트(아이템)가 남아 있을 수 있으므로
        //    기본값 + 장비 보너스로 정확히 복원
        //    (장비 보너스 _equipSpeedBonus는 DontDestroyOnLoad이므로 씬을 넘어 유지됨)
        moveSpeed = _baseMoveSpeed + _equipSpeedBonus;

        if (SceneLoader.TryConsumePendingSpawn(out SceneLoader.SpawnData data))
        {
            transform.position = new Vector3(data.position.x, data.position.y, 0f);
            anim.SetFloat("DirX", data.direction.x);
            anim.SetFloat("DirY", data.direction.y);
            lastDir = data.direction;
        }

        // 씬 전환 후 새 씬의 InventoryManager를 다시 찾음
        inventory = FindFirstObjectByType<InventoryManager>();
    }

    void OnDestroy()
    {
        // 원본만 _exists를 리셋 — 복제본이 소멸될 때는 건드리지 않음
        if (_isOriginal)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _exists = false;
        }
    }
}
