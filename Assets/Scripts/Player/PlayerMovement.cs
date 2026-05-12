using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// 플레이어 이동, 공격, 피격, HP 관리를 담당하는 영구 싱글톤.
// DontDestroyOnLoad로 씬 전환 후에도 유지된다.
public class PlayerMovement : MonoBehaviour
{
    private static PlayerMovement instance;

    [Header("이동 속도")]
    public float moveSpeed = 5f;

    [Header("공격 설정")]
    public int melee_damage = 20;       // 근접 공격 데미지
    public int shoot_damage = 30;       // 원거리 공격 데미지
    public float shootBulletSpeed = 10f; // 총알 이동 속도

    [Header("발소리 설정")]
    public float footstepInterval = 0.4f;  // 발소리 간격
    private float footstepTimer = 0f;

    [Header("공격 콜라이더 (방향별)")]
    public Collider2D attack_Left;
    public Collider2D attack_Right;
    public Collider2D attack_Front;
    public Collider2D attack_Back;

    [Header("피격 설정")]
    public float knockbackForce = 5f;      // 넉백 힘
    public float knockbackDuration = 0.2f; // 넉백 지속 시간
    public float hurtDuration = 0.4f;      // 피격 무적 시간

    [Header("HP 설정")]
    public int maxHp = 100;
    private int currentHp;
    private bool isDead = false;

    [Header("총알 설정")]
    public GameObject bulletPrefab;    // 총알 프리팹
    public GameObject hitEffectPrefab; // 탄착 이펙트 프리팹

    private bool isHurt = false;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private Vector2 movement;                        // 현재 프레임 이동 벡터
    private Vector2 lastDir = new Vector2(0, -1);   // 마지막 이동 방향 (공격 방향 결정에 사용)
    private bool isAttacking = false;

    public int currentWeapon = 0; // 0=파이프, 1=권총

    // 외부에서 이동 여부 확인 (애니메이션 등)
    public bool IsMoving => movement != Vector2.zero && !isAttacking && !isHurt && !isDead;

    // 외부에서 현재 HP 읽기
    public int CurrentHp => currentHp;

    // 일시적으로 이동 속도를 높이는 코루틴 (아이템 효과)
    public IEnumerator SpeedBoostCoroutine(float amount, float duration)
    {
        moveSpeed += amount;
        yield return new WaitForSeconds(duration);
        moveSpeed -= amount;
    }

    void Awake()
    {
        // 싱글톤 보장: 이미 존재하면 새 인스턴스 제거
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        currentHp = maxHp;
        Debug.Log("hasPipe: " + GameManager.Instance?.hasPipe);
        Debug.Log("hasGun: " + GameManager.Instance?.hasGun);
    }

    void Update()
    {
        if (isDead) return;

        // 인벤토리가 열려있으면 조작 차단
        if (InventoryManager.Instance != null && InventoryManager.Instance.isOpen) return;

        // 팝업이 열려있으면 공격 차단
        bool popupOpen = (ItemPopup.Instance != null && ItemPopup.Instance.IsOpen) ||
                         (SlotSelectPopup.Instance != null && SlotSelectPopup.Instance.IsOpen);

        if (Input.GetMouseButtonDown(0) && !isAttacking && !isHurt && !popupOpen)
            Attack();

        // 공격/피격 중에는 이동 불가
        if (isAttacking || isHurt)
        {
            movement = Vector2.zero;
        }
        else
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");

            // 대각선 이동 방지: x축 우선
            if (x != 0)
                movement = new Vector2(x, 0);
            else if (y != 0)
                movement = new Vector2(0, y);
            else
                movement = Vector2.zero;

            if (movement != Vector2.zero)
                lastDir = movement;
        }

        // 애니메이터 파라미터 갱신
        anim.SetFloat("DirX", lastDir.x);
        anim.SetFloat("DirY", lastDir.y);
        anim.SetBool("IsWalking", movement != Vector2.zero && !isAttacking && !isHurt);

        HandleFootstepSound();

        // Tab 키로 무기 전환 (권총 소지 시에만 가능)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (GameManager.Instance != null && GameManager.Instance.hasGun)
                SwitchWeapon(currentWeapon == 0 ? 1 : 0);
        }
    }

    void FixedUpdate()
    {
        // 물리 기반 이동 (공격/피격/사망 중에는 정지)
        if (!isAttacking && !isHurt && !isDead)
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    // 넉백 방향을 받아 피격 코루틴 시작
    public void TakeHit(Vector2 knockbackDirection)
    {
        if (isHurt || isDead) return;
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(HurtRoutine(GetAxisAlignedDirection(knockbackDirection)));
    }

    // 대각선 방향을 4방향(상하좌우) 중 하나로 변환
    Vector2 GetAxisAlignedDirection(Vector2 direction)
    {
        if (direction == Vector2.zero)
            return lastDir == Vector2.zero ? Vector2.down : -lastDir.normalized;

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            return new Vector2(Mathf.Sign(direction.x), 0f);

        return new Vector2(0f, Mathf.Sign(direction.y));
    }

    // HP 감소 후 HP바 UI 갱신. 0 이하면 사망 처리
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHp = Mathf.Max(0, currentHp - amount);
        PlayerHPbar.Instance?.Refresh(currentHp, maxHp);

        if (currentHp <= 0)
            Die();
    }

    // HP 회복 후 HP바 갱신 (플래시 효과 없음)
    public void Heal(int amount)
    {
        if (isDead) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        PlayerHPbar.Instance?.Refresh(currentHp, maxHp, false);
    }

    // 사망 처리: 이동 정지, 사망 애니메이션 재생
    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        movement = Vector2.zero;
        AudioManager.Instance.PlaySFX("death");

        // 마지막 이동 방향에 따라 스프라이트 플립 결정
        float dirX = anim.GetFloat("DirX");
        float dirY = anim.GetFloat("DirY");
        if (Mathf.Abs(dirX) >= Mathf.Abs(dirY))
            sr.flipX = dirX > 0;
        else
            sr.flipX = false;

        anim.SetBool("IsDie", true);
        StartCoroutine(DieRoutine());
    }

    // 사망 애니메이션 후 오브젝트 비활성화 및 게임오버 씬 전환
    IEnumerator DieRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        gameObject.SetActive(false);
        SceneManager.LoadScene("GameOver");
    }

    // 피격: 현재 공격 중단, 넉백 적용, 무적 시간 처리
    IEnumerator HurtRoutine(Vector2 knockbackDirection)
    {
        isHurt = true;
        anim.SetBool("IsHurt", true);
        AudioManager.Instance.PlaySFX("hurt");

        // 피격 시 공격 상태 즉시 초기화
        if (isAttacking)
        {
            isAttacking = false;
            anim.SetBool("IsAttacking", false);
            CancelInvoke("EndAttack");
            DisableAllAttackColliders();
        }

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);
        rb.linearVelocity = Vector2.zero;

        // 남은 무적 시간 대기
        float remaining = hurtDuration - knockbackDuration;
        if (remaining > 0)
            yield return new WaitForSeconds(remaining);

        anim.SetBool("IsHurt", false);
        isHurt = false;
    }

    // 현재 무기에 따라 근접 또는 원거리 공격 시작
    void Attack()
    {
        if (isAttacking) return;
        if (GameManager.Instance == null) return;

        Debug.Log("currentWeapon: " + currentWeapon);
        Debug.Log("hasPipe: " + GameManager.Instance.hasPipe);
        Debug.Log("hasGun: " + GameManager.Instance.hasGun);

        // 파이프 공격 (weapon=0)
        if (currentWeapon == 0)
        {
            if (!GameManager.Instance.hasPipe) return;
            isAttacking = true;
            AttackMelee();
            return;
        }

        // 총 공격 (weapon=1)
        if (currentWeapon == 1)
        {
            if (!GameManager.Instance.hasGun) return;
            isAttacking = true;
            AttackShoot();
            return;
        }
    }

    // 근접 공격: 애니메이션 이벤트에서 콜라이더 활성화
    void AttackMelee()
    {
        anim.SetBool("IsAttacking", true);
        AudioManager.Instance.PlaySFX("swing");
    }

    // 애니메이션 이벤트: 근접 공격 히트박스 활성화
    public void OnMeleeHitStart()
    {
        if (isHurt || isDead) return;
        ActivateAttackCollider(lastDir);
    }

    // 애니메이션 이벤트: 근접 공격 히트박스 비활성화 및 공격 종료
    public void OnMeleeHitEnd()
    {
        DisableAllAttackColliders();
        EndAttack();
    }

    // 마지막 이동 방향에 해당하는 공격 콜라이더만 활성화
    void ActivateAttackCollider(Vector2 direction)
    {
        DisableAllAttackColliders();

        if (direction.x > 0)      attack_Right.enabled = true;
        else if (direction.x < 0) attack_Left.enabled  = true;
        else if (direction.y > 0) attack_Front.enabled = true;
        else if (direction.y < 0) attack_Back.enabled  = true;
    }

    // 모든 방향 공격 콜라이더 비활성화
    void DisableAllAttackColliders()
    {
        attack_Left.enabled  = false;
        attack_Right.enabled = false;
        attack_Front.enabled = false;
        attack_Back.enabled  = false;
    }

    // 원거리 공격: 애니메이션 재생 후 레이캐스트로 탄환 발사
    void AttackShoot()
    {
        anim.SetBool("IsAttacking", true);
        AudioManager.Instance.PlaySFX("gunhit");
        StartCoroutine(ShootBulletCoroutine(lastDir));
        float shootDuration = GetCurrentAnimationLength();
        CancelInvoke("EndAttack");
        Invoke("EndAttack", shootDuration);
    }

    // 레이캐스트로 탄착점 계산 후 총알을 직선 이동시키고 피격 처리
    IEnumerator ShootBulletCoroutine(Vector2 direction)
    {
        // Player 레이어 제외하고 레이캐스트
        int layerMask = ~LayerMask.GetMask("Player");

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction,
            100f,
            layerMask
        );

        Vector2 startPos = transform.position;
        Vector2 endPos = hit.collider != null ? hit.point : startPos + direction * 100f;

        // 총알 생성 및 방향 회전
        GameObject bullet = Instantiate(bulletPrefab, startPos, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(bullet, SceneManager.GetActiveScene());

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

        // 총알을 목적지까지 이동
        float distance = Vector2.Distance(startPos, endPos);
        float duration = distance / shootBulletSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (bullet == null) yield break;
            bullet.transform.position = Vector2.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }

        Destroy(bullet);

        // 충돌 대상에 데미지 처리
        if (hit.collider != null)
        {
            ShowHitEffect(hit.point);
            AudioManager.Instance.PlaySFX("hurt");

            // GetComponentInParent: 히트박스가 자식 오브젝트여도 부모의 MonsterAI를 찾음
            MonsterAI monster = hit.collider.GetComponentInParent<MonsterAI>();
            if (monster != null)
            {
                monster.TakeDamage(shoot_damage);
            }
            else
            {
                // MonsterAI 없으면 gunNPC인지 확인
                var flowManager = FindFirstObjectByType<GameFlowManager>();
                if (flowManager != null &&
                    (hit.collider.gameObject == flowManager.gunNPC ||
                    hit.collider.transform.IsChildOf(flowManager.gunNPC.transform)))
                {
                    flowManager.OnGunNPCHit();
                }
            }
        }
    }

    // 탄착점에 히트 이펙트 생성 후 자동 제거
    void ShowHitEffect(Vector2 position)
    {
        GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(effect, SceneManager.GetActiveScene());
        Destroy(effect, 0.3f);
    }

    // 공격 상태 초기화 및 모든 공격 콜라이더 비활성화
    void EndAttack()
    {
        isAttacking = false;
        anim.SetBool("IsAttacking", false);
        DisableAllAttackColliders();
    }

    // 무기 변경 후 애니메이터 파라미터 동기화
    public void SwitchWeapon(int weaponType)
    {
        if (currentWeapon == weaponType) return;
        currentWeapon = weaponType;
        anim.SetInteger("Weapon", weaponType);
        AudioManager.Instance.PlaySFX("click");
    }

    // 이동 중일 때 일정 간격으로 발소리 재생
    void HandleFootstepSound()
    {
        // 실제로 움직이고 있을 때만 (공격·피격·사망 중에는 자동으로 false)
        if (!IsMoving)
        {
            footstepTimer = 0f;  // 멈추면 타이머 리셋
            return;
        }

        footstepTimer += Time.deltaTime;

        if (footstepTimer >= footstepInterval)
        {
            AudioManager.Instance.PlaySFX("walk");
            footstepTimer = 0f;
        }
    }

    // 현재 재생 중인 애니메이션 클립의 길이 반환 (원거리 공격 타이밍 계산용)
    float GetCurrentAnimationLength()
    {
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        return stateInfo.length / anim.speed;
    }

    // 씬 전환 후 PlayerPrefs에 저장된 스폰 위치가 있으면 해당 위치로 이동
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (PlayerPrefs.HasKey("SpawnX"))
        {
            float x = PlayerPrefs.GetFloat("SpawnX");
            float y = PlayerPrefs.GetFloat("SpawnY");
            transform.position = new Vector3(x, y, 0);
            PlayerPrefs.DeleteKey("SpawnX");
            PlayerPrefs.DeleteKey("SpawnY");
        }

        if (PlayerPrefs.HasKey("SpawnDirX"))
        {
            float dirX = PlayerPrefs.GetFloat("SpawnDirX");
            float dirY = PlayerPrefs.GetFloat("SpawnDirY");
            anim.SetFloat("DirX", dirX);
            anim.SetFloat("DirY", dirY);
            PlayerPrefs.DeleteKey("SpawnDirX");
            PlayerPrefs.DeleteKey("SpawnDirY");
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
