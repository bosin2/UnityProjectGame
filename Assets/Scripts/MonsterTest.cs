using UnityEngine;

public class MonsterAnimTest : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;

    private float moveSpeed = 2f;
    private float timer = 0f;
    private int step = 0;

    // 각 동작 지속 시간
    private float walkTime = 4f;    // 걷는 시간
    private float idleTime = 2f;    // 쉬는 시간
    private float attackTime = 3f; // 공격 시간

    private Vector2 currentDirection;
    private Vector2[] directions = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };
    private int dirIndex = 0;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        currentDirection = directions[0]; // 오른쪽부터 시작
        StartIdle();
    }

    void Update()
    {
        timer += Time.deltaTime;

        int phase = step % 3; // 0:Idle, 1:Walk, 2:Attack

        if (phase == 0 && timer >= idleTime)
        {
            StartWalk();
        }
        else if (phase == 1 && timer >= walkTime)
        {
            StartAttack();
        }
        else if (phase == 2 && timer >= attackTime)
        {
            NextDirection();
            StartIdle();
        }
    }

    void FixedUpdate()
    {
        // Walk 상태일 때만 실제로 이동
        if (step % 3 == 1)
        {
            rb.linearVelocity = currentDirection * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void StartIdle()
    {
        Debug.Log($"Idle - {GetDirectionName()}");
        timer = 0f;
        step++;

        anim.SetBool("IsWalking", false);
        anim.SetBool("IsAttacking", false);
        UpdateAnimatorDirection();
    }

    void StartWalk()
    {
        Debug.Log($"Walk - {GetDirectionName()}");
        timer = 0f;
        step++;

        anim.SetBool("IsWalking", true);
        anim.SetBool("IsAttacking", false);
        UpdateAnimatorDirection();
    }

    void StartAttack()
    {
        Debug.Log($"Attack - {GetDirectionName()}");
        timer = 0f;
        step++;

        anim.SetBool("IsWalking", false);
        anim.SetBool("IsAttacking", true);
        UpdateAnimatorDirection();
    }

    void NextDirection()
    {
        dirIndex = (dirIndex + 1) % directions.Length;
        currentDirection = directions[dirIndex];
    }

    void UpdateAnimatorDirection()
    {
        anim.SetFloat("DirX", currentDirection.x);
        anim.SetFloat("DirY", currentDirection.y);
    }

    string GetDirectionName()
    {
        if (currentDirection == Vector2.right) return "오른쪽";
        if (currentDirection == Vector2.left) return "왼쪽";
        if (currentDirection == Vector2.up) return "위";
        return "아래";
    }
}