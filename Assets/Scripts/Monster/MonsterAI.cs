using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class MonsterAI : MonoBehaviour
{
    [Header("스탯")]
    public int hp = 60;
    public int damage = 15;
    public float speed = 2f;

    [Header("범위")]
    public float detectionRange = 18f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;

    [Header("접촉 데미지")]
    public float contactDamageInterval = 1.5f;
    private float contactDamageCooldown = 0f;

    [Header("타겟")]
    public Transform target;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private bool isDead = false;

    private enum State { Idle, Walk, Attack }
    private State currentState = State.Idle;
    private bool isAttacking = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        FindPlayer();
        ChangeState(State.Idle);
    }

    void FindPlayer()
    {
        if (target != null) return;

        // DontDestroyOnLoad 제외하고 같은 씬 플레이어 탐색
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

    void Update()
    {
        if (isDead) return;

        // 접촉 데미지 쿨다운 감소
        if (contactDamageCooldown > 0)
            contactDamageCooldown -= Time.deltaTime;

        if (target == null)
        {
            FindPlayer();
            return;
        }

        // 플레이어가 다른 씬에 있으면 타겟 초기화
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

    void FixedUpdate()
    {
        if (isDead) return;

        if (currentState == State.Walk && !isAttacking && target != null)
            MoveInFourDirections();
        else
            rb.linearVelocity = Vector2.zero;
    }

    void MoveInFourDirections()
    {
        Vector2 diff = (Vector2)(target.position - transform.position);

        // x, y 중 큰 축으로만 이동
        Vector2 moveDir;
        if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
            moveDir = new Vector2(diff.x > 0 ? 1 : -1, 0);
        else
            moveDir = new Vector2(0, diff.y > 0 ? 1 : -1);

        rb.linearVelocity = moveDir * speed;

        anim.SetFloat("DirX", moveDir.x);
        anim.SetFloat("DirY", moveDir.y);
    }

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

        // 플레이어 방향 4방향 스냅
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

        // 쿨다운 후 범위 안에 있으면 데미지
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

    // 몸에 닿으면 데미지
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

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        hp -= amount;

        // 피격 애니메이션 트리거
        anim.SetTrigger("IsHurt");

        if (hp <= 0)
            StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("IsDie", true);

        // 사망 애니메이션 후 삭제
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}