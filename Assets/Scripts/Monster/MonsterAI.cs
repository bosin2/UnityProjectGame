using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// 몬스터의 AI 상태 머신, 이동, 공격, 피격, HP바 표시를 통합 관리하는 컴포넌트
public class MonsterAI : MonoBehaviour
{
    [Header("스탯")]
    public int hp = 60;
    public int damage = 15;
    public float speed = 2f;

    [Header("감지 및 공격 범위")]
    public float detectionRange = 18f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;

    [Header("접촉 데미지")]
    public float contactDamageInterval = 1.5f;
    private float contactDamageCooldown = 0f;

    [Header("타겟")]
    public Transform target;

    [Header("HP바")]
    public GameObject hpBarPrefab;
    public Vector3 hpBarOffset = new Vector3(0, 1f, 0);
    private GameObject hpBarInstance;
    private Image hpFillImage;

    private int maxHp;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private bool isDead = false;

    private enum State { Idle, Walk, Attack }
    private State currentState = State.Idle;
    private bool isAttacking = false;

    // 컴포넌트 초기화, HP바 생성, 플레이어 탐색 후 Idle 상태로 진입
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        maxHp = hp;

        if (hpBarPrefab != null)
        {
            hpBarInstance = Instantiate(hpBarPrefab);

            Canvas hpCanvas = hpBarInstance.GetComponent<Canvas>();
            if (hpCanvas != null)
                hpCanvas.worldCamera = Camera.main;

            Image[] images = hpBarInstance.GetComponentsInChildren<Image>();
            Debug.Log("이미지 개수: " + images.Length);
            if (images.Length >= 2)
            {
                hpFillImage = images[1];
                Debug.Log("Fill 연결됨: " + hpFillImage.name);
            }
        }

        FindPlayer();
        ChangeState(State.Idle);
    }

    // 매 프레임 HP바 월드 위치를 몬스터 머리 위로 갱신
    void LateUpdate()
    {
        if (hpBarInstance != null)
            hpBarInstance.transform.position = transform.position + hpBarOffset;
    }

    // 씬에서 활성화된 PlayerMovement를 찾아 target으로 설정
    void FindPlayer()
    {
        if (target != null) return;

        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.gameObject.activeInHierarchy)
            {
                target = player.transform;
                return;
            }
        }
    }

    // 플레이어와의 거리에 따라 Idle/Walk/Attack 상태 전환. 다른 씬의 플레이어는 무시
    void Update()
    {
        if (isDead) return;

        if (contactDamageCooldown > 0)
            contactDamageCooldown -= Time.deltaTime;

        if (target == null)
        {
            FindPlayer();
            return;
        }

        if (target.gameObject.scene.name != gameObject.scene.name &&
            target.gameObject.scene.name != "DontDestroyOnLoad")
        {
            target = null;
            rb.linearVelocity = Vector2.zero;
            ChangeState(State.Idle);
            return;
        }

        if (isAttacking) return;

        float dist = Vector2.Distance(transform.position, target.position);

        if (dist <= attackRange)
            ChangeState(State.Attack);
        else if (dist <= detectionRange)
            ChangeState(State.Walk);
        else
            ChangeState(State.Idle);
    }

    // Walk 상태일 때만 4방향 이동 적용. 그 외에는 정지
    void FixedUpdate()
    {
        if (isDead) return;

        if (currentState == State.Walk && !isAttacking && target != null)
            MoveInFourDirections();
        else
            rb.linearVelocity = Vector2.zero;
    }

    // x/y 차이 중 큰 축 방향으로만 이동해 4방향 이동을 구현
    void MoveInFourDirections()
    {
        Vector2 diff = (Vector2)(target.position - transform.position);

        Vector2 moveDir;
        if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
            moveDir = new Vector2(diff.x > 0 ? 1 : -1, 0);
        else
            moveDir = new Vector2(0, diff.y > 0 ? 1 : -1);

        rb.linearVelocity = moveDir * speed;
        anim.SetFloat("DirX", moveDir.x);
        anim.SetFloat("DirY", moveDir.y);
    }

    // 상태를 전환하고 각 상태에 맞는 속도/애니메이션 파라미터 적용
    void ChangeState(State newState)
    {
        if (currentState == newState && newState != State.Attack) return;
        currentState = newState;

        switch (currentState)
        {
            case State.Idle:
                rb.linearVelocity = Vector2.zero;
                anim.SetBool("IsWalking", false);
                break;

            case State.Walk:
                anim.SetBool("IsWalking", true);
                anim.SetBool("IsAttacking", false);
                break;

            case State.Attack:
                if (!isAttacking) StartCoroutine(AttackRoutine());
                break;
        }
    }

    // 공격 애니메이션 재생 후 쿨다운 대기, 범위 내 플레이어에게 데미지와 넉백 적용
    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        if (target == null)
        {
            isAttacking = false;
            ChangeState(State.Idle);
            yield break;
        }

        Vector2 rawDir = (target.position - transform.position).normalized;
        Vector2 attackDir;
        if (Mathf.Abs(rawDir.x) >= Mathf.Abs(rawDir.y))
            attackDir = new Vector2(rawDir.x > 0 ? 1 : -1, 0);
        else
            attackDir = new Vector2(0, rawDir.y > 0 ? 1 : -1);

        anim.SetFloat("DirX", attackDir.x);
        anim.SetFloat("DirY", attackDir.y);
        anim.SetBool("IsWalking", false);
        anim.SetBool("IsAttacking", true);

        yield return new WaitForSeconds(attackCooldown);

        if (target != null)
        {
            Vector2 toPlayer = target.position - transform.position;
            if (toPlayer.magnitude <= attackRange + 0.5f)
            {
                PlayerMovement player = target.GetComponent<PlayerMovement>();
                if (player != null)
                {
                    player.TakeHit(toPlayer.normalized);
                    player.TakeDamage(damage);
                }
            }
        }

        anim.SetBool("IsAttacking", false);
        isAttacking = false;
        ChangeState(State.Idle);
    }

    // 플레이어와 충돌 중일 때 일정 간격으로 접촉 데미지와 넉백 적용
    void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        if (contactDamageCooldown > 0) return;

        PlayerMovement player = collision.gameObject.GetComponent<PlayerMovement>();
        if (player != null)
        {
            Vector2 knockDir = (collision.transform.position - transform.position).normalized;
            player.TakeHit(knockDir);
            player.TakeDamage(damage);
            contactDamageCooldown = contactDamageInterval;
        }
    }

    // 데미지 수치만큼 HP 감소, 피격 애니메이션 재생. HP 0 이하면 사망 처리
    public void TakeDamage(int amount)
    {
        Debug.Log("몬스터 데미지 받음: " + amount);
        if (isDead) return;

        hp -= amount;
        anim.SetTrigger("IsHurt");
        UpdateHPBar();

        if (hp <= 0)
            StartCoroutine(DieRoutine());
    }

    // HP 비율에 맞게 HP바 fillAmount 갱신
    void UpdateHPBar()
    {
        if (hpFillImage != null)
            hpFillImage.fillAmount = (float)hp / maxHp;
    }

    // 사망 애니메이션 재생, HP바 제거 후 오브젝트 삭제
    IEnumerator DieRoutine()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("IsDie", true);

        if (hpBarInstance != null)
            Destroy(hpBarInstance);

        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }

    // 에디터에서 감지 범위(노란색)와 공격 범위(빨간색)를 기즈모로 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}