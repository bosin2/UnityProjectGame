using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A* 경로탐색으로 플레이어를 추적하는 몬스터.
/// MonsterBase에서 HP / 피격 / 사망 / HP바를 상속한다.
/// showPath = true 로 설정하면 LineRenderer로 경로 시각화.
/// </summary>
public class StalkerMonster : MonsterBase
{
    protected override bool PersistsStateAcrossScenes => true;

    [Header("추적")]
    public float speed          = 5.5f;
    public float arriveDistance = 0.2f;

    [Header("접촉 데미지")]
    public int   contactDamage         = 15;
    public float contactDamageInterval = 1.5f;

    [Header("A* 경로탐색")]
    public float     gridCellSize        = 0.5f;
    public float     obstacleCheckRadius = 0.2f;
    public LayerMask obstacleLayer;
    public float     pathUpdateInterval  = 0.3f;

    [Header("경로 시각화")]
    public bool  showPath  = false;
    public Color pathColor = Color.cyan;
    public Color nodeColor = Color.yellow;
    public float nodeRadius = 0.08f;

    [Header("BGM")]
    [SerializeField] private string activeBgm = "stoker";
    [SerializeField] private string revertBgm = "corridor";
    private bool bgmStarted = false;

    // ── 내부 상태 ──────────────────────────────────────────────────────
    private float        contactCooldown = 0f;
    private Transform    playerTransform;
    private PlayerHealth playerHealth;
    private List<Vector2> currentPath  = new List<Vector2>();
    private int          pathIndex     = 0;
    private float        pathTimer     = 0f;
    private LineRenderer lineRenderer;

    // ── A* 내부 노드 ──────────────────────────────────────────────────
    private class Node
    {
        public Vector2Int gridPos;
        public Vector2    worldPos;
        public float      gCost, hCost;
        public Node       parent;
        public float      fCost => gCost + hCost;

        public Node() { }
        public Node(Vector2Int g, Vector2 w) { gridPos = g; worldPos = w; }
    }

    // ── A* 컬렉션 재사용 (GC 방지) ────────────────────────────────────
    private readonly List<Node>                   _openList    = new List<Node>(128);
    private readonly HashSet<Vector2Int>           _openSet     = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int>           _closedSet   = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, Node>  _nodeMap     = new Dictionary<Vector2Int, Node>(256);
    private readonly Stack<Node>                   _nodePool    = new Stack<Node>(256);
    private readonly List<Vector2>                 _retraceBuf  = new List<Vector2>(64);

    private static readonly Vector2Int[] _dirs =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    Node RentNode(Vector2Int g, Vector2 w)
    {
        var n = _nodePool.Count > 0 ? _nodePool.Pop() : new Node();
        n.gridPos = g; n.worldPos = w;
        n.gCost = float.MaxValue; n.hCost = 0f; n.parent = null;
        return n;
    }

    void ReturnAllNodes()
    {
        foreach (var kvp in _nodeMap) _nodePool.Push(kvp.Value);
    }

    // ── 초기화 ──────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();

        // 콜라이더 크기로 obstacleCheckRadius 자동 설정
        Collider2D col = GetComponent<Collider2D>();
        if (col is CircleCollider2D circle)
            obstacleCheckRadius = circle.radius + 0.05f;
        else if (col is BoxCollider2D box)
            obstacleCheckRadius = Mathf.Max(box.size.x, box.size.y) * 0.5f + 0.05f;
        else if (col is CapsuleCollider2D capsule)
            obstacleCheckRadius = Mathf.Max(capsule.size.x, capsule.size.y) * 0.5f + 0.05f;

        SetupLineRenderer();
    }

    protected override void Start()
    {
        base.Start();
        FindPlayerInScene();

        if (!isDead && !string.IsNullOrEmpty(activeBgm))
        {
            AudioManager.Instance?.PlayBGM(activeBgm);
            bgmStarted = true;
        }
    }

    void SetupLineRenderer()
    {
        if (!showPath) return; // 경로 시각화 비활성 시 LineRenderer 자체를 추가하지 않음

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth  = 0.1f;
        lineRenderer.endWidth    = 0.1f;

        Shader shader = Shader.Find("Unlit/Color");
        if (shader != null)
        {
            lineRenderer.material = new Material(shader);
            lineRenderer.material.color = pathColor;
        }

        lineRenderer.useWorldSpace  = true;
        lineRenderer.positionCount  = 0;

        if (sr != null)
        {
            lineRenderer.sortingLayerName = sr.sortingLayerName;
            lineRenderer.sortingOrder     = sr.sortingOrder + 1;
        }
    }

    // ── Update / FixedUpdate ──────────────────────────────────────────

    void Update()
    {
        if (isDead) return;
        if (contactCooldown > 0) contactCooldown -= Time.deltaTime;

        if (playerTransform == null) { FindPlayerInScene(); return; }

        // 거리 기반 접촉 데미지 (MonsterAI attackRange 패턴 동일)
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= arriveDistance + 0.3f && contactCooldown <= 0f && playerHealth != null)
        {
            Vector2 knockDir = (playerTransform.position - transform.position).normalized;
            playerHealth.TakeHit(knockDir);
            playerHealth.TakeDamage(contactDamage);
            contactCooldown = contactDamageInterval;
        }

        pathTimer -= Time.deltaTime;
        if (pathTimer <= 0f)
        {
            pathTimer = pathUpdateInterval;
            RecalculatePath();
        }

        if (lineRenderer != null)
        {
            if (showPath) DrawPath();
            else          lineRenderer.positionCount = 0;
        }
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            anim.SetBool("IsWalking", false);
            return;
        }

        if (playerTransform == null || currentPath.Count == 0 || pathIndex >= currentPath.Count)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool("IsWalking", false);
            return;
        }

        Vector2 targetPos = currentPath[pathIndex];
        Vector2 diff      = targetPos - rb.position;

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
            diff      = targetPos - rb.position;
        }

        Vector2 moveDir = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y)
            ? new Vector2(diff.x > 0 ? 1 : -1, 0)
            : new Vector2(0, diff.y > 0 ? 1 : -1);

        rb.linearVelocity = moveDir * speed;
        anim.SetFloat("DirX", moveDir.x);
        anim.SetFloat("DirY", moveDir.y);
        anim.SetBool("IsWalking", true);
    }

    // ── 경로탐색 ──────────────────────────────────────────────────────

    void RecalculatePath()
    {
        if (playerTransform == null) return;
        FindPath(rb.position, playerTransform.position);
        pathIndex = 0;
    }

    void FindPath(Vector2 startWorld, Vector2 goalWorld)
    {
        // 노드 풀 반환 후 컬렉션 초기화
        ReturnAllNodes();
        _openList.Clear(); _openSet.Clear(); _closedSet.Clear(); _nodeMap.Clear();
        currentPath.Clear();

        Vector2Int startGrid = WorldToGrid(startWorld);
        Vector2Int goalGrid  = WorldToGrid(goalWorld);
        if (startGrid == goalGrid) return;

        Node startNode = RentNode(startGrid, GridToWorld(startGrid));
        startNode.gCost = 0;
        startNode.hCost = Heuristic(startGrid, goalGrid);
        _openList.Add(startNode);
        _openSet.Add(startGrid);
        _nodeMap[startGrid] = startNode;

        int maxIter = 500;
        while (_openList.Count > 0 && maxIter-- > 0)
        {
            // O(n) 최솟값 탐색 (인덱스 추적 → swap-remove)
            int bestIdx = 0;
            for (int i = 1; i < _openList.Count; i++)
                if (_openList[i].fCost < _openList[bestIdx].fCost ||
                   (_openList[i].fCost == _openList[bestIdx].fCost &&
                    _openList[i].hCost  < _openList[bestIdx].hCost))
                    bestIdx = i;

            Node current = _openList[bestIdx];

            // swap-remove: O(1)
            int last = _openList.Count - 1;
            _openList[bestIdx] = _openList[last];
            _openList.RemoveAt(last);
            _openSet.Remove(current.gridPos);
            _closedSet.Add(current.gridPos);

            if (current.gridPos == goalGrid) { RetracePath(current); return; }

            foreach (Vector2Int dir in _dirs)
            {
                Vector2Int neighborGrid = current.gridPos + dir;
                if (_closedSet.Contains(neighborGrid)) continue;

                Vector2 neighborWorld = GridToWorld(neighborGrid);
                if (IsObstacle(neighborWorld)) continue;

                float newG = current.gCost + 1f;
                if (!_nodeMap.TryGetValue(neighborGrid, out Node neighbor))
                {
                    neighbor = RentNode(neighborGrid, neighborWorld);
                    _nodeMap[neighborGrid] = neighbor;
                }

                if (newG < neighbor.gCost)
                {
                    neighbor.gCost  = newG;
                    neighbor.hCost  = Heuristic(neighborGrid, goalGrid);
                    neighbor.parent = current;
                    if (!_openSet.Contains(neighborGrid))
                    {
                        _openList.Add(neighbor);
                        _openSet.Add(neighborGrid);
                    }
                }
            }
        }

        // 경로 없으면 목표 직진
        currentPath.Add(goalWorld);
    }

    void RetracePath(Node endNode)
    {
        _retraceBuf.Clear();
        Node current = endNode;
        while (current != null) { _retraceBuf.Add(current.worldPos); current = current.parent; }

        // 역순으로 currentPath에 직접 기록
        for (int i = _retraceBuf.Count - 1; i >= 0; i--)
            currentPath.Add(_retraceBuf[i]);
    }

    // ── 좌표 변환 유틸 ────────────────────────────────────────────────

    Vector2Int WorldToGrid(Vector2 w) => new Vector2Int(
        Mathf.RoundToInt(w.x / gridCellSize),
        Mathf.RoundToInt(w.y / gridCellSize));

    Vector2 GridToWorld(Vector2Int g) =>
        new Vector2(g.x * gridCellSize, g.y * gridCellSize);

    float Heuristic(Vector2Int a, Vector2Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    bool IsObstacle(Vector2 worldPos)
    {
        if (Vector2.Distance(worldPos, rb.position) < obstacleCheckRadius) return false;
        return Physics2D.OverlapCircle(worldPos, obstacleCheckRadius, obstacleLayer) != null;
    }

    // ── 경로 시각화 ──────────────────────────────────────────────────

    void DrawPath()
    {
        if (currentPath == null || currentPath.Count == 0 || pathIndex >= currentPath.Count)
        { lineRenderer.positionCount = 0; return; }

        int remaining = currentPath.Count - pathIndex;
        lineRenderer.positionCount = remaining + 1;
        lineRenderer.SetPosition(0, new Vector3(rb.position.x, rb.position.y, 0f));

        for (int i = 0; i < remaining; i++)
        {
            Vector2 wp = currentPath[pathIndex + i];
            lineRenderer.SetPosition(i + 1, new Vector3(wp.x, wp.y, 0f));
        }
    }

    void OnDrawGizmosSelected()
    {
        if (currentPath == null) return;
        Gizmos.color = nodeColor;
        foreach (Vector2 p in currentPath) Gizmos.DrawSphere(p, nodeRadius);
        Gizmos.color = pathColor;
        for (int i = 0; i < currentPath.Count - 1; i++)
            Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
    }

    // ── BGM 복귀 ──────────────────────────────────────────────────────

    void RevertBGM()
    {
        if (!bgmStarted) return;
        if (!string.IsNullOrEmpty(revertBgm))
            AudioManager.Instance?.PlayBGM(revertBgm);
        bgmStarted = false;
    }

    protected override System.Collections.IEnumerator DieRoutine()
    {
        RevertBGM();
        yield return StartCoroutine(base.DieRoutine());
    }

    protected override void OnDisable()
    {
        base.OnDisable(); // SavePersistentState 호출
        RevertBGM();      // 씬 전환 시 BGM 복귀
    }

    // ── 플레이어 탐색 ─────────────────────────────────────────────────

    void FindPlayerInScene()
    {
        PlayerHealth ph = FindPlayerHealth();
        if (ph != null)
        {
            playerTransform = ph.transform;
            playerHealth    = ph;
            currentPath.Clear();
        }
    }

}
