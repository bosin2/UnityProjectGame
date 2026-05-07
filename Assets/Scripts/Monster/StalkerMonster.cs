using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StalkerMonster : MonoBehaviour
{
    [Header("Follow")]
    public float speed = 5.5f;
    public float arriveDistance = 0.2f;

    [Header("A* Pathfinding")]
    public float gridCellSize = 0.5f;       // 격자 한 칸 크기
    public float obstacleCheckRadius = 0.2f; // 장애물 판정 반지름
    public LayerMask obstacleLayer;          // 장애물 레이어 설정
    public float pathUpdateInterval = 0.3f;  // 경로 재계산 주기 (초)

    [Header("Path Visualization")]
    public bool showPath = true;
    public Color pathColor = Color.cyan;
    public Color nodeColor = Color.yellow;
    public float nodeRadius = 0.08f;

    private PlayerMovement player;
    private Rigidbody2D rb;
    private Animator anim;

    private List<Vector2> currentPath = new List<Vector2>();
    private int pathIndex = 0;
    private float pathUpdateTimer = 0f;

    // 경로 시각화용 LineRenderer
    private LineRenderer lineRenderer;

    // ─── A* 노드 ───────────────────────────────────────────
    private class Node
    {
        public Vector2Int gridPos;
        public Vector2 worldPos;
        public float gCost, hCost;
        public Node parent;
        public float fCost => gCost + hCost;

        public Node(Vector2Int gridPos, Vector2 worldPos)
        {
            this.gridPos = gridPos;
            this.worldPos = worldPos;
        }
    }

    // ───────────────────────────────────────────────────────
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // 콜라이더 크기 자동으로 읽어서 장애물 판정 반경에 반영
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            // 콜라이더의 절반 크기 + 여유값(0.05f)을 반경으로 사용
            if (col is CircleCollider2D circle)
                obstacleCheckRadius = circle.radius + 0.05f;
            else if (col is BoxCollider2D box)
                obstacleCheckRadius = Mathf.Max(box.size.x, box.size.y) * 0.5f + 0.05f;
            else if (col is CapsuleCollider2D capsule)
                obstacleCheckRadius = Mathf.Max(capsule.size.x, capsule.size.y) * 0.5f + 0.05f;

            Debug.Log($"obstacleCheckRadius 자동 설정: {obstacleCheckRadius}");
        }

        SetupLineRenderer();
    }

    void Start()
    {
        // rb, anim, SetupLineRenderer 여기서 제거
        FindPlayerInSameScene();
        Debug.Log("Path count: " + currentPath.Count + " / lineRenderer positions: " + lineRenderer.positionCount);
    }

    void SetupLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        // 셰이더 확실하게
        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material.color = pathColor;

        // 몬스터 자신의 Sorting Layer/Order 그대로 가져와서 그 위에 그리기
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            lineRenderer.sortingLayerName = sr.sortingLayerName;
            lineRenderer.sortingOrder = sr.sortingOrder + 1;
            Debug.Log($"Sorting Layer: {sr.sortingLayerName}, Order: {sr.sortingOrder + 1}");
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 0;
    }

    void DrawPath()
    {

        Debug.Log($"DrawPath: currentPath={currentPath.Count}, pathIndex={pathIndex}");

        if (currentPath == null || currentPath.Count == 0 || pathIndex >= currentPath.Count)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        int remaining = currentPath.Count - pathIndex;
        lineRenderer.positionCount = remaining + 1;

        // z = 0으로 고정 (2D에서 z값이 카메라 뒤로 가면 안 보임)
        Vector3 startPos = new Vector3(rb.position.x, rb.position.y, 0f);
        lineRenderer.SetPosition(0, startPos);

        for (int i = 0; i < remaining; i++)
        {
            Vector2 wp = currentPath[pathIndex + i];
            lineRenderer.SetPosition(i + 1, new Vector3(wp.x, wp.y, 0f));
        }
    }


    void Update()
    {
        if (player == null)
        {
            FindPlayerInSameScene();
            return;
        }

        // 경로 재계산 타이머
        pathUpdateTimer -= Time.deltaTime;
        if (pathUpdateTimer <= 0f)
        {
            pathUpdateTimer = pathUpdateInterval;
            RecalculatePath();
        }

        // 경로 시각화 업데이트
        if (showPath)
            DrawPath();
        else
            lineRenderer.positionCount = 0;
    }

    void FixedUpdate()
    {
        if (player == null || currentPath.Count == 0 || pathIndex >= currentPath.Count)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool("IsWalking", false);
            return;
        }

        Vector2 targetPos = currentPath[pathIndex];
        Vector2 diff = targetPos - rb.position;

        // 현재 웨이포인트 도착 시 다음으로
        if (diff.magnitude <= arriveDistance)
        {
            pathIndex++;
            if (pathIndex >= currentPath.Count)
            {
                rb.linearVelocity = Vector2.zero;
                anim.SetBool("IsWalking", false);
                return;
            }
            targetPos = currentPath[pathIndex];
            diff = targetPos - rb.position;
        }

        // 4방향 이동 (기존 코드 스타일 유지)
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

    // ─── 경로 재계산 ────────────────────────────────────────

    void RecalculatePath()
    {
        if (player == null) return;

        List<Vector2> newPath = FindPath(rb.position, player.transform.position);
        if (newPath != null && newPath.Count > 0)
        {
            currentPath = newPath;
            pathIndex = 0; //이미 있지만, 혹시 없다면 추가
        }
        // 경로 못 찾아도 기존 경로 날리기
        else
        {
            currentPath.Clear();
            pathIndex = 0;
        }
    }

    // ─── A* 알고리즘 ────────────────────────────────────────

    List<Vector2> FindPath(Vector2 startWorld, Vector2 goalWorld)
    {
        Vector2Int startGrid = WorldToGrid(startWorld);
        Vector2Int goalGrid = WorldToGrid(goalWorld);

        Debug.Log($"FindPath 시작: {startGrid} → {goalGrid}"); 

        if (startGrid == goalGrid)
        {
            Debug.Log("startGrid == goalGrid, 경로 없음");       
            return null;
        }

        var openSet = new List<Node>();
        var closedSet = new HashSet<Vector2Int>();
        var nodeMap = new Dictionary<Vector2Int, Node>();

        Node startNode = new Node(startGrid, GridToWorld(startGrid));
        startNode.gCost = 0;
        startNode.hCost = Heuristic(startGrid, goalGrid);
        openSet.Add(startNode);
        nodeMap[startGrid] = startNode;

        int maxIterations = 500; // 무한루프 방지
        int iterations = 0;

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            // fCost가 가장 낮은 노드 선택
            Node current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
                if (openSet[i].fCost < current.fCost ||
                   (openSet[i].fCost == current.fCost && openSet[i].hCost < current.hCost))
                    current = openSet[i];

            openSet.Remove(current);
            closedSet.Add(current.gridPos);

            // 도착
            if (current.gridPos == goalGrid)
                return RetracePath(current);

            // 상하좌우 이웃 탐색
            foreach (Vector2Int dir in GetNeighborDirs())
            {
                Vector2Int neighborGrid = current.gridPos + dir;
                if (closedSet.Contains(neighborGrid)) continue;
                if (IsObstacle(GridToWorld(neighborGrid))) continue;

                float newG = current.gCost + 1f;

                if (!nodeMap.TryGetValue(neighborGrid, out Node neighbor))
                {
                    neighbor = new Node(neighborGrid, GridToWorld(neighborGrid));
                    nodeMap[neighborGrid] = neighbor;
                    neighbor.gCost = float.MaxValue;
                }

                if (newG < neighbor.gCost)
                {
                    neighbor.gCost = newG;
                    neighbor.hCost = Heuristic(neighborGrid, goalGrid);
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        // 경로를 찾지 못한 경우 직선 이동
        return new List<Vector2> { goalWorld };
    }

    List<Vector2> RetracePath(Node endNode)
    {
        var path = new List<Vector2>();
        Node current = endNode;
        while (current != null)
        {
            path.Add(current.worldPos);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    // ─── 유틸 ───────────────────────────────────────────────

    Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / gridCellSize),
            Mathf.RoundToInt(worldPos.y / gridCellSize)
        );
    }

    Vector2 GridToWorld(Vector2Int gridPos)
    {
        return new Vector2(gridPos.x * gridCellSize, gridPos.y * gridCellSize);
    }

    float Heuristic(Vector2Int a, Vector2Int b)
    {
        // 맨해튼 거리 (4방향 이동에 최적)
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    bool IsObstacle(Vector2 worldPos)
    {
        // 몬스터 자신 위치 근처는 장애물 판정 제외 (시작점 막힘 방지)
        if (Vector2.Distance(worldPos, rb.position) < obstacleCheckRadius)
            return false;

        return Physics2D.OverlapCircle(worldPos, obstacleCheckRadius, obstacleLayer) != null;
    }

    Vector2Int[] GetNeighborDirs()
    {
        return new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };
    }

    // ─── 경로 시각화 ────────────────────────────────────────

    // 노드 위치를 Scene 뷰에서도 확인할 수 있도록 Gizmos 표시
    void OnDrawGizmosSelected()
    {
        if (currentPath == null) return;

        Gizmos.color = nodeColor;
        foreach (Vector2 point in currentPath)
            Gizmos.DrawSphere(point, nodeRadius);

        Gizmos.color = pathColor;
        for (int i = 0; i < currentPath.Count - 1; i++)
            Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
    }

    // ─── 플레이어 탐색 (기존 유지) ──────────────────────────

    void FindPlayerInSameScene()
    {
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        foreach (PlayerMovement candidate in players)
        {
            if (!candidate.gameObject.activeInHierarchy) continue;

            bool isSameScene = candidate.gameObject.scene == gameObject.scene;
            bool isDontDestroyPlayer = candidate.gameObject.scene.name == "DontDestroyOnLoad";

            if (!isSameScene && !isDontDestroyPlayer) continue;

            player = candidate;
            currentPath.Clear();
            return;
        }
    }
}