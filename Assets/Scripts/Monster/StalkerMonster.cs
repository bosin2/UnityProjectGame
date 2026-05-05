using System.Collections.Generic;
using UnityEngine;

public class StalkerMonster : MonoBehaviour
{
    [Header("Follow")]
    public float speed = 5.5f;
    public float recordDistance = 0.3f;
    public float arriveDistance = 0.2f;

    private PlayerMovement player;
    private Rigidbody2D rb;
    private Animator anim;

    private Queue<Vector2> trail = new Queue<Vector2>();
    private Vector2 lastRecordedPos;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        FindPlayerInSameScene();

        if (player != null)
            lastRecordedPos = player.transform.position;
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayerInSameScene();
            return;
        }

        if (!player.IsMoving)
            return;

        Vector2 playerPos = player.transform.position;

        if (Vector2.Distance(lastRecordedPos, playerPos) >= recordDistance)
        {
            trail.Enqueue(playerPos);
            lastRecordedPos = playerPos;
        }
    }

    void FixedUpdate()
    {
        if (player == null || !player.IsMoving || trail.Count == 0)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool("IsWalking", false);
            return;
        }
        SkipUnreachableOldTrailPoints();
        Vector2 targetPos = trail.Peek();
        Vector2 diff = targetPos - rb.position;

        if (diff.magnitude <= arriveDistance)
        {
            trail.Dequeue();
            return;
        }

        Vector2 moveDir;

        if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
            moveDir = new Vector2(diff.x > 0 ? 1 : -1, 0);
        else
            moveDir = new Vector2(0, diff.y > 0 ? 1 : -1);

        rb.linearVelocity = moveDir * speed;

        anim.SetFloat("DirX", moveDir.x);
        anim.SetFloat("DirY", moveDir.y);
        anim.SetBool("IsWalking", true);
    }

    void SkipUnreachableOldTrailPoints()
    {
        while (trail.Count > 1)
        {
            Vector2 next = trail.Peek();
            float distanceToNext = Vector2.Distance(transform.position, next);

            if (distanceToNext < 3f)
                break;

            trail.Dequeue();
        }
    }

    void FindPlayerInSameScene()
    {
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        foreach (PlayerMovement candidate in players)
        {
            if (!candidate.gameObject.activeInHierarchy)
                continue;

            bool isSameScene = candidate.gameObject.scene == gameObject.scene;
            bool isDontDestroyPlayer = candidate.gameObject.scene.name == "DontDestroyOnLoad";

            if (!isSameScene && !isDontDestroyPlayer)
                continue;

            player = candidate;

            trail.Clear();
            lastRecordedPos = player.transform.position;

            return;
        }
    }

}

