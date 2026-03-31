using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이어의 이동, 공격, 무기 교체를 담당하는 컴포넌트.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    // 씬 전체에서 플레이어가 하나만 존재하도록 관리하는 싱글톤 인스턴스
    private static PlayerMovement instance;

    [Header("이동 설정")]
    public float moveSpeed = 5f;        // 플레이어 이동 속도

    private Rigidbody2D rb;             // 물리 이동 처리용
    private Animator anim;              // 애니메이션 제어용
    private SpriteRenderer sr;          // 스프라이트 렌더러 (필요 시 활용)

    private Vector2 movement;           // 현재 프레임의 이동 방향
    private Vector2 lastDir = new Vector2(0, -1); // 마지막 이동 방향 (정지 시에도 방향 유지용, 기본값: 아래)
    private bool isAttacking = false;   // 공격 중 여부 (이동/입력 잠금 플래그)

    private int currentWeapon = 0;      // 현재 장착 무기 (0 = 근접, 1 = 총)

    /// <summary>
    /// 싱글톤 초기화 및 컴포넌트 참조 설정
    /// 씬 전환 후에도 유지 씬 로드 이벤트 등록
    /// </summary>
    void Awake()
    {
        // 이미 플레이어 인스턴스가 있으면 중복이므로 제거
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 이 오브젝트를 유지

        // 컴포넌트 참조 초기화
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // 씬 로드 시 스폰 위치 처리를 위한 이벤트 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// 매 프레임 입력 처리, 애니메이터 갱신, 공격/무기 교체 감지
    /// </summary>
    void Update()
    {
        // 공격 중이면 이동 입력 무시 (이동 잠금)
        if (isAttacking)
        {
            movement = Vector2.zero;
        }
        else
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");

            // 대각선 이동 방지: X 우선, 없으면 Y 적용
            if (x != 0)
                movement = new Vector2(x, 0);
            else if (y != 0)
                movement = new Vector2(0, y);
            else
                movement = Vector2.zero;

            // 이동 중일 때만 lastDir 갱신 (정지 시 마지막 방향 유지)
            if (movement != Vector2.zero)
                lastDir = movement;
        }

        // 애니메이터에 방향 및 이동 상태 전달
        anim.SetFloat("DirX", lastDir.x);
        anim.SetFloat("DirY", lastDir.y);
        anim.SetBool("IsWalking", movement != Vector2.zero && !isAttacking);

        // 마우스 좌클릭 감지 (공격 중이 아닐 때만)
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            Attack();
        }

        // 숫자 키로 무기 교체 (1 = 근접, 2 = 총)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchWeapon(0); // 근접무기
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchWeapon(1); // 총
        }
    }

    /// <summary>
    /// 물리 기반 이동 처리
    /// </summary>
    void FixedUpdate()
    {
        // 공격 중이 아닐 때만 이동 적용
        if (!isAttacking)
        {
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// 공격 시작하고 현재 공격 애니메이션 길이만큼 대기 후 자동으로 공격을 종료
    /// </summary>
    void Attack()
    {
        // 이미 공격 중이면 중복 실행 방지
        if (isAttacking)
            return;

        isAttacking = true;
        anim.SetBool("IsAttacking", true);

        // 현재 공격 애니메이션 길이에 맞춰 종료 타이머 설정
        float attackDuration = GetCurrentAnimationLength();

        CancelInvoke("EndAttack");          // 이전 Invoke(유니티실행명령어)가 남아있을 경우 취소
        Invoke("EndAttack", attackDuration); // 애니메이션 끝나면 공격 종료
    }

    /// <summary>
    /// 공격 상태를 종료하고 이동/입력 잠금 해제
    /// </summary>
    void EndAttack()
    {
        isAttacking = false;
        anim.SetBool("IsAttacking", false);
    }

    /// <summary>
    /// 무기를 교체하고 애니메이터에 반영
    /// </summary>
    /// <param 0 = 근접무기, 1 = 총</param>
    void SwitchWeapon(int weaponType)
    {
        // 이미 같은 무기를 들고 있으면 무시
        if (currentWeapon == weaponType)
            return;

        currentWeapon = weaponType;
        anim.SetInteger("Weapon", weaponType); // 애니메이터 레이어/파라미터에 무기 타입 전달
    }

    /// <summary>
    /// 현재 재생 중인 애니메이션의 실제 길이를 반환
    /// </summary>
    float GetCurrentAnimationLength()
    {
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        return stateInfo.length / anim.speed;
    }

    /// <summary>
    /// 씬 로드 완료 시 호출 프리팹에 저장된 스폰 위치가 있으면 플레이어를 해당 위치로 이동시킴
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (PlayerPrefs.HasKey("SpawnX"))
        {
            float x = PlayerPrefs.GetFloat("SpawnX");
            float y = PlayerPrefs.GetFloat("SpawnY");
            transform.position = new Vector3(x, y, 0);

            Debug.Log($"플레이어 위치 변경: ({x}, {y})");

            // 사용한 스폰 위치 데이터 삭제 (재사용 방지)
            PlayerPrefs.DeleteKey("SpawnX");
            PlayerPrefs.DeleteKey("SpawnY");
        }
    }

    /// <summary>
    /// 오브젝트 파괴 시 씬 로드 이벤트 구독을 해제
    /// 해제하지 않으면 메모리 누수 또는 오류 발생 가능
    /// </summary>
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}