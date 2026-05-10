using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A* 경로탐색으로 플레이어를 추적하는 몬스터 컴포넌트.
// 장애물 레이어를 피해 그리드 기반 경로를 계산하고, LineRenderer로 경로를 시각화한다.
public class StalkerMonster : MonoBehaviour
{
    [Header("Follow")]
    public float speed = 5.5f;
    public float arriveDistance = 0.2f;

    [Header("A* Pathfinding")]
    public float gridCellSize = 0.5f;        // 그리드 한 칸 크기
    public float obstacleCheckRadius = 0.2f; // 장애물 감지 반경
    public LayerMask obstacleLayer;          // 장애물 레이어 마스크
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

    // ── A* 내부 노드 클래스 ──────────────────────────────────────────────
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

    // ── 초기화 ──────────────────────────────────────────────────────────

    // 콜라이더 크기를 자동으로 읽어 obstacleCheckRadius에 반영
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
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
        FindPlayerInSameScene();
        Debug.Log("Path count: " + currentPath.Count + " / lineRenderer positions: " + lineRenderer.positionCount);
    }

    // LineRenderer 컴포넌트를 생성하고 몬스터 스프라이트와 같은 정렬 레이어에 배치
    void SetupLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material.color = pathColor;

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

    // 현재 경로의 남은 웨이포인트를 LineRenderer로 그림
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

        Vector3 startPos = new Vector3(rb.position.x, rb.position.y, 0f);
        lineRenderer.SetPosition(0, startPos);

        for (int i = 0; i < remaining; i++)
        {
            Vector2 wp = currentPath[pathIndex + i];
            lineRenderer.SetPosition(i + 1, new Vector3(wp.x, wp.y, 0f));
        }
    }

    // 경로 재계산 타이머 처리 및 경로 시각화 갱신
    void Update()
    {
        if (player == null)
        {
            FindPlayerInSameScene();
            return;
        }

        pathUpdateTimer -= Time.deltaTime;
        if (pathUpdateTimer <= 0f)
        {
            pathUpdateTimer = pathUpdateInterval;
            RecalculatePath();
        }

        if (showPath)
            DrawPath();
        else
            lineRenderer.positionCount = 0;
    }

    // 현재 경로의 다음 웨이포인트를 향해 4방향으로 이동
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

        // 웨이포인트 도달 시 다음으로 진행
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

        // x/y 차이가 큰 축 방향으로만 이동 (4방향)
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

    // ── 경로 재계산 ─────────────────────────────────────────────────────

    // A*로 새 경로를 계산하고 currentPath를 갱신
    void RecalculatePath()
    {
        if (player == null) return;

        List<Vector2> newPath = FindPath(rb.position, player.transform.position);
        if (newPath != null && newPath.Count > 0)
        {
            currentPath = newPath;
            pathIndex = 0;
        }
        else
        {
            currentPath.Clear();
            pathIndex = 0;
        }
    }

    // ── A* 알고리즘 ─────────────────────────────────────────────────────

    // 시작~목표 사이의 장애물 회피 경로를 A*로 탐색. 경로를 찾지 못하면 목표 직진 반환
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

            // 목표 도달
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

        // 경로를 찾지 못한 경우 목표 직접 이동
        return new List<Vector2> { goalWorld };
    }

    // 도달한 목표 노드에서 부모를 역추적해 경로 리스트 반환
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

    // ── 좌표 변환 유틸 ──────────────────────────────────────────────────

    // 월드 좌표 → 그리드 좌표 변환
    Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / gridCellSize),
            Mathf.RoundToInt(worldPos.y / gridCellSize)
        );
    }

    // 그리드 좌표 → 월드 좌표 변환
    Vector2 GridToWorld(Vector2Int gridPos)
    {
        return new Vector2(gridPos.x * gridCellSize, gridPos.y * gridCellSize);
    }

    // 맨해튼 거리 휴리스틱 (4방향 이동에 적합)
    float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // 해당 월드 위치가 장애물인지 확인. 자기 자신 위치는 장애물로 판정하지 않음
    bool IsObstacle(Vector2 worldPos)
    {
        if (Vector2.Distance(worldPos, rb.position) < obstacleCheckRadius)
            return false;

        return Physics2D.OverlapCircle(worldPos, obstacleCheckRadius, obstacleLayer) != null;
    }

    // 상하좌우 4방향 이웃 반환
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

    // ── 경로 시각화 (에디터) ────────────────────────────────────────────

    // Scene 뷰에서 현재 경로 노드(노란 구)와 경로 선(cyan)을 Gizmos로 표시
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

    // ── 플레이어 탐색 ───────────────────────────────────────────────────

    // 같은 씬 또는 DontDestroyOnLoad에 있는 활성화된 플레이어를 탐색해 추적 대상으로 설정
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
