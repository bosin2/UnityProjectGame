using UnityEngine;
using UnityEngine.AI; // NavMesh 사용을 위해 필수
using System.Collections;

public class EnhancedMonsterAI : MonoBehaviour
{
    [Header("Stats")]
    public int hp = 60;
    public int damage = 15;
    public float speed = 2f;

    [Header("Ranges")]
    public float detectionRange = 18f;
    public float attackRange = 1.2f;    // 2D 타일맵 기준 1.2~1.5 추천
    public float attackTime = 1.0f;

    [Header("Targets")]
    public Transform target;

    // NavMesh 관련 컴포넌트
    private NavMeshAgent agent;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    private enum State { Idle, Walk, Attack }
    private State currentState = State.Idle;
    private bool isAttacking = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 1. NavMeshAgent 설정
        agent = GetComponent<NavMeshAgent>();

        // 2D에서 필수적인 설정 (코드로 강제 고정)
        agent.updateRotation = false; // 에이전트가 오브젝트를 회전시키지 못하게 함
        agent.updateUpAxis = false;   // 2D 평면(XY)을 사용하도록 설정
        agent.speed = speed;

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        ChangeState(State.Idle);
    }

    void Update()
    {
        if (target == null || isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, target.position);

        // 상태 결정 로직
        if (distanceToPlayer <= attackRange)
        {
            ChangeState(State.Attack);
        }
        else if (distanceToPlayer <= detectionRange)
        {
            ChangeState(State.Walk);
        }
        else
        {
            ChangeState(State.Idle);
        }

        // Walk 상태일 때 실시간으로 목적지 갱신
        if (currentState == State.Walk)
        {
            agent.SetDestination(target.position);
            UpdateAnimatorByVelocity();
        }
    }

    void ChangeState(State newState)
    {
        if (currentState == newState && newState != State.Attack) return;

        currentState = newState;

        switch (currentState)
        {
            case State.Idle:
                agent.isStopped = true; // 이동 중지
                anim.SetBool("IsWalking", false);
                break;

            case State.Walk:
                agent.isStopped = false; // 이동 재개
                anim.SetBool("IsWalking", true);
                anim.SetBool("IsAttacking", false);
                break;

            case State.Attack:
                if (!isAttacking) StartCoroutine(AttackRoutine());
                break;
        }
    }

    // 에이전트의 실제 이동 속도(velocity)를 바탕으로 애니메이션 방향 결정
    void UpdateAnimatorByVelocity()
    {
        // 이동 속도가 아주 작을 때는 방향을 갱신하지 않음 (떨림 방지)
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector2 moveDir = agent.velocity.normalized;
            anim.SetFloat("DirX", moveDir.x);
            anim.SetFloat("DirY", moveDir.y);
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        agent.isStopped = true; // 공격 시작 시 이동 멈춤
        agent.velocity = Vector3.zero;

        // 공격 방향을 위해 플레이어 위치 확인
        Vector2 attackDir = (target.position - transform.position).normalized;
        anim.SetFloat("DirX", attackDir.x);
        anim.SetFloat("DirY", attackDir.y);

        anim.SetBool("IsWalking", false);
        anim.SetBool("IsAttacking", true);

        yield return new WaitForSeconds(attackTime);

        // 실제 데미지 판정
        if (target != null && Vector2.Distance(transform.position, target.position) <= attackRange + 0.3f)
        {
            // 플레이어의 스크립트 이름을 확인하여 호출하세요
            //target.GetComponent<PlayerMovement>()?.TakeDamage(damage);
            //Debug.Log($"몬스터가 플레이어에게 {damage} 데미지를 입힘");
        }

        anim.SetBool("IsAttacking", false);
        isAttacking = false;
        ChangeState(State.Idle);
    }

    public void TakeDamage(int amount)
    {
        hp -= amount;
        if (hp <= 0) Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}