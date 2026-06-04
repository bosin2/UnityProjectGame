/*
 * StalkerMonster.cs
 * 역할: A* 경로탐색으로 플레이어를 추적하고 접촉 데미지를 주는 특수 추적 몬스터입니다.
 * 연결: MonsterBase의 HP/사망/시체 저장 로직, AudioManager의 추적 BGM, PlayerHealth 피격 처리와 연결됩니다.
 * 주의: 이 몬스터는 씬을 나갔다 돌아와도 상태가 저장되며, 사망 시 BGM 복귀와 시체 위치 복원이 함께 동작합니다.
 */using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A* 경로탐색으로 플레이어를 추적하는 몬스터.
/// MonsterBase에서 HP / 피격 / 사망 / HP바를 상속한다.
/// showPath = true 로 설정하면 LineRenderer로 경로 시각화.
/// </summary>
public class StalkerMonster : MonsterBase
{
    // 씬을 나가도 사망/HP 상태 유지 (플레이어를 씬 넘어 쫓아오는 공포 연출)
    protected override bool PersistsStateAcrossScenes => true;

    [Header("추적")]
    public float speed          = 5.5f;
    public float arriveDistance = 0.2f; // 경로 웨이포인트에 도착했다고 판단하는 거리

    [Header("접촉 데미지")]
    public int   contactDamage         = 15;
    public float contactDamageInterval = 1.5f; // 플레이어와 접촉 중 데미지 주기 (초)

    [Header("경로 탐색")]
    public float     gridCellSize        = 0.5f;   // A* 그리드 한 칸 크기 (월드 단위)
    public float     obstacleCheckRadius = 0.2f;   // Awake에서 콜라이더 크기로 자동 설정됨
    public LayerMask obstacleLayer;                // 장애물로 취급할 레이어
    public float     pathUpdateInterval  = 0.3f;   // 경로 재계산 주기 (초) – 너무 짧으면 CPU 부하

    [Header("경로 시각화")]
    public bool  showPath  = false;    // true이면 LineRenderer로 경로를 그림 (디버그용)
    public Color pathColor = Color.cyan;
    public Color nodeColor = Color.yellow;
    public float nodeRadius = 0.08f;

    [Header("BGM")]
    [SerializeField] private string activeBgm = "stoker"; // 몬스터가 살아있는 동안 재생할 BGM
    [SerializeField] private string revertBgm = "corridor"; // 사망/비활성화 후 복귀할 BGM
    private bool bgmStarted = false; // BGM을 시작했는지 추적 (중복 복귀 방지)

    // ── 내부 상태 ──────────────────────────────────────────────────────
    private float        contactCooldown = 0f;     // 접촉 데미지 쿨다운 타이머
    private Transform    playerTransform;
    private PlayerHealth playerHealth;
    private List<Vector2> currentPath  = new List<Vector2>(); // 현재 A* 경로 (월드 좌표 리스트)
    private int          pathIndex     = 0;        // 현재 향하고 있는 웨이포인트 인덱스
    private float        pathTimer     = 0f;       // 경로 재계산 누적 타이머
    private LineRenderer lineRenderer;

    // ── A* 내부 노드 ──────────────────────────────────────────────────
    // 노드 1개 = 그리드 한 칸 (gridPos: 정수 좌표, worldPos: 월드 좌표)
    private class Node
    {
        public Vector2Int gridPos;
        public Vector2    worldPos;
        public float      gCost, hCost; // g = 시작→현재 실제 비용, h = 현재→목표 예상 비용
        public Node       parent;       // 경로 역추적용 부모 노드
        public float      fCost => gCost + hCost; // 정렬 기준 (낮을수록 우선)

        public Node() { }
        public Node(Vector2Int g, Vector2 w) { gridPos = g; worldPos = w; }
    }

    // ── A* 컬렉션 재사용 (GC 방지) ────────────────────────────────────
    // A*는 매 pathUpdateInterval마다 호출되므로 매번 new List<>() 하면 GC 압박이 큼.
    // 고정 크기로 미리 할당한 컬렉션을 Clear()해서 재사용한다.
    private readonly List<Node>                   _openList    = new List<Node>(128);    // 탐색 예정 노드
    private readonly HashSet<Vector2Int>           _openSet     = new HashSet<Vector2Int>(); // openList 빠른 포함 여부 확인
    private readonly HashSet<Vector2Int>           _closedSet   = new HashSet<Vector2Int>(); // 이미 탐색된 노드
    private readonly Dictionary<Vector2Int, Node>  _nodeMap     = new Dictionary<Vector2Int, Node>(256); // 그리드 좌표 → 노드 매핑
    private readonly Stack<Node>                   _nodePool    = new Stack<Node>(256);   // 반환된 노드 재사용 풀
    private readonly List<Vector2>                 _retraceBuf  = new List<Vector2>(64);  // RetracePath 임시 버퍼

    // 4방향 (상/하/좌/우) – 대각선 이동 없음
    private static readonly Vector2Int[] _dirs =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    // ── 노드 풀 관리 ─────────────────────────────────────────────────

    /// <summary>
    /// 노드 풀에서 노드를 꺼내 초기화하여 반환.
    /// 풀이 비어 있으면 new Node()를 생성 (첫 실행 시에만 발생).
    /// </summary>
    Node RentNode(Vector2Int g, Vector2 w)
    {
        var n = _nodePool.Count > 0 ? _nodePool.Pop() : new Node();
        n.gridPos = g; n.worldPos = w;
        n.gCost = float.MaxValue; n.hCost = 0f; n.parent = null;
        return n;
    }

    /// <summary>_nodeMap의 모든 노드를 풀에 반환 (다음 탐색에서 재사용).</summary>
    void ReturnAllNodes()
    {
        foreach (var kvp in _nodeMap) _nodePool.Push(kvp.Value);
    }

    // ── 초기화 ──────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();

        // 콜라이더 크기로 obstacleCheckRadius 자동 설정
        // → 몬스터 몸체가 장애물에 겹치지 않도록 최소 반경을 보장
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

        // 몬스터가 살아 있을 때 공포 BGM 재생
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

        // 스프라이트 렌더러보다 한 레이어 위에 그려야 경로가 보임
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

        // 플레이어가 arriveDistance + 0.3f 이내에 있으면 접촉 데미지
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= arriveDistance + 0.3f && contactCooldown <= 0f && playerHealth != null)
        {
            Vector2 knockDir = (playerTransform.position - transform.position).normalized;
            playerHealth.TakeHit(knockDir);
            playerHealth.TakeDamage(contactDamage);
            contactCooldown = contactDamageInterval;
        }

        // 경로 재계산 타이머
        pathTimer -= Time.deltaTime;
        if (pathTimer <= 0f)
        {
            pathTimer = pathUpdateInterval;
            RecalculatePath();
        }

        // 경로 시각화 갱신
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

        // 경로가 없거나 모두 통과했으면 정지
        if (playerTransform == null || currentPath.Count == 0 || pathIndex >= currentPath.Count)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool("IsWalking", false);
            return;
        }

        // 현재 목표 웨이포인트를 향해 이동
        Vector2 targetPos = currentPath[pathIndex];
        Vector2 diff      = targetPos - rb.position;

        // 웨이포인트에 충분히 가까워지면 다음 웨이포인트로 진행
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

        // 4방향 이동: X/Y 중 더 큰 성분 방향으로만 이동
        Vector2 moveDir = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y)
            ? new Vector2(diff.x > 0 ? 1 : -1, 0)
            : new Vector2(0, diff.y > 0 ? 1 : -1);

        rb.linearVelocity = moveDir * speed;
        anim.SetFloat("DirX", moveDir.x);
        anim.SetFloat("DirY", moveDir.y);
        anim.SetBool("IsWalking", true);
    }

    // ── 경로탐색 ──────────────────────────────────────────────────────

    /// <summary>현재 위치 → 플레이어 위치로 A* 경로 재계산 후 pathIndex를 0으로 초기화.</summary>
    void RecalculatePath()
    {
        if (playerTransform == null) return;
        FindPath(rb.position, playerTransform.position);
        pathIndex = 0;
    }

    /// <summary>
    /// A* 알고리즘으로 startWorld → goalWorld 경로를 탐색하여 currentPath에 저장.
    ///
    /// 알고리즘 개요:
    /// 1. 월드 좌표 → 그리드 정수 좌표로 변환
    /// 2. openList에서 fCost(=gCost+hCost)가 가장 낮은 노드를 꺼냄
    /// 3. 목표 노드에 도달하면 RetracePath로 경로 복원
    /// 4. 4방향 이웃 노드를 검사: 장애물이면 skip, gCost가 개선되면 업데이트
    /// 5. maxIter=500 회 초과 시 강제 종료 → 목표 직진(fallback)
    /// </summary>
    void FindPath(Vector2 startWorld, Vector2 goalWorld)
    {
        // 이전 탐색 결과를 노드 풀에 반환하고 컬렉션 초기화
        ReturnAllNodes();
        _openList.Clear(); _openSet.Clear(); _closedSet.Clear(); _nodeMap.Clear();
        currentPath.Clear();

        Vector2Int startGrid = WorldToGrid(startWorld);
        Vector2Int goalGrid  = WorldToGrid(goalWorld);
        if (startGrid == goalGrid) return; // 이미 목표 위치에 있으면 경로 불필요

        // 시작 노드 설정: gCost=0, hCost=맨해튼 거리
        Node startNode = RentNode(startGrid, GridToWorld(startGrid));
        startNode.gCost = 0;
        startNode.hCost = Heuristic(startGrid, goalGrid);
        _openList.Add(startNode);
        _openSet.Add(startGrid);
        _nodeMap[startGrid] = startNode;

        int maxIter = 500; // 무한 루프 방지 (복잡한 맵에서 완전 탐색을 막음)
        while (_openList.Count > 0 && maxIter-- > 0)
        {
            // O(n) 최솟값 탐색 (우선순위 큐 없이 단순 선형 탐색)
            // fCost가 같으면 hCost가 낮은 것 우선 (목표에 더 가까운 노드 선택)
            int bestIdx = 0;
            for (int i = 1; i < _openList.Count; i++)
                if (_openList[i].fCost < _openList[bestIdx].fCost ||
                   (_openList[i].fCost == _openList[bestIdx].fCost &&
                    _openList[i].hCost  < _openList[bestIdx].hCost))
                    bestIdx = i;

            Node current = _openList[bestIdx];

            // swap-remove: 마지막 요소와 교체 후 RemoveAt(last) → O(1)
            int last = _openList.Count - 1;
            _openList[bestIdx] = _openList[last];
            _openList.RemoveAt(last);
            _openSet.Remove(current.gridPos);
            _closedSet.Add(current.gridPos);

            // 목표 도달 → 경로 역추적
            if (current.gridPos == goalGrid) { RetracePath(current); return; }

            // 4방향 이웃 탐색
            foreach (Vector2Int dir in _dirs)
            {
                Vector2Int neighborGrid = current.gridPos + dir;
                if (_closedSet.Contains(neighborGrid)) continue; // 이미 탐색됨 → skip

                Vector2 neighborWorld = GridToWorld(neighborGrid);
                if (IsObstacle(neighborWorld)) continue;          // 장애물 → skip

                // 이웃까지의 새 gCost = 현재 gCost + 1 (모든 이동 비용 균일)
                float newG = current.gCost + 1f;
                if (!_nodeMap.TryGetValue(neighborGrid, out Node neighbor))
                {
                    // 처음 발견한 노드: 풀에서 꺼내 nodeMap에 등록
                    neighbor = RentNode(neighborGrid, neighborWorld);
                    _nodeMap[neighborGrid] = neighbor;
                }

                // 더 좋은 경로를 발견했을 때만 업데이트
                if (newG < neighbor.gCost)
                {
                    neighbor.gCost  = newG;
                    neighbor.hCost  = Heuristic(neighborGrid, goalGrid);
                    neighbor.parent = current; // 역추적을 위한 부모 연결

                    // openSet에 없으면 탐색 대기 목록에 추가
                    if (!_openSet.Contains(neighborGrid))
                    {
                        _openList.Add(neighbor);
                        _openSet.Add(neighborGrid);
                    }
                }
            }
        }

        // 경로 탐색 실패(장애물로 막힘 또는 maxIter 초과) → 목표 직진 fallback
        currentPath.Add(goalWorld);
    }

    /// <summary>
    /// 도달 노드에서 parent를 따라 역추적하여 경로를 복원.
    /// parent 체인은 목표→시작 순서이므로 역순으로 currentPath에 기록한다.
    /// </summary>
    void RetracePath(Node endNode)
    {
        _retraceBuf.Clear();
        Node current = endNode;
        // parent 연결을 따라 시작 노드까지 거슬러 올라감
        while (current != null) { _retraceBuf.Add(current.worldPos); current = current.parent; }

        // 역순으로 currentPath에 기록 → 시작 방향으로 진행되는 경로
        for (int i = _retraceBuf.Count - 1; i >= 0; i--)
            currentPath.Add(_retraceBuf[i]);
    }

    // ── 좌표 변환 유틸 ────────────────────────────────────────────────

    /// <summary>월드 좌표 → 그리드 정수 좌표 (반올림).</summary>
    Vector2Int WorldToGrid(Vector2 w) => new Vector2Int(
        Mathf.RoundToInt(w.x / gridCellSize),
        Mathf.RoundToInt(w.y / gridCellSize));

    /// <summary>그리드 정수 좌표 → 그리드 셀 중심 월드 좌표.</summary>
    Vector2 GridToWorld(Vector2Int g) =>
        new Vector2(g.x * gridCellSize, g.y * gridCellSize);

    /// <summary>맨해튼 거리 휴리스틱 (4방향 이동 → 대각선 없음).</summary>
    float Heuristic(Vector2Int a, Vector2Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    /// <summary>
    /// 해당 위치가 장애물인지 판단.
    /// 몬스터 자신이 있는 위치는 장애물로 취급하지 않음 (자기 발판 blockage 방지).
    /// </summary>
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

        // 현재 위치부터 남은 웨이포인트까지를 LineRenderer로 연결
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
        // 씬 뷰에서 웨이포인트 구체와 경로 선 시각화
        Gizmos.color = nodeColor;
        foreach (Vector2 p in currentPath) Gizmos.DrawSphere(p, nodeRadius);
        Gizmos.color = pathColor;
        for (int i = 0; i < currentPath.Count - 1; i++)
            Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
    }

    // ── BGM 복귀 ──────────────────────────────────────────────────────

    /// <summary>몬스터 사망 또는 비활성화 시 원래 복도 BGM으로 복귀.</summary>
    void RevertBGM()
    {
        if (!bgmStarted) return; // 이미 복귀했거나 BGM을 시작하지 않았으면 skip
        if (!string.IsNullOrEmpty(revertBgm))
            AudioManager.Instance?.PlayBGM(revertBgm);
        bgmStarted = false;
    }

    // 사망 코루틴 앞에 BGM 복귀 삽입 (MonsterBase.DieRoutine 전에 실행)
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
            currentPath.Clear(); // 새 씬 진입 시 이전 경로 초기화
        }
    }

}




