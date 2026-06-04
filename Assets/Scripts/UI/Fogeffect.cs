/*
 * Fogeffect.cs
 * 역할: 화면 안개/어둠 효과를 관리하는 UI 또는 렌더링 보조 싱글톤입니다.
 * 연결: CorridorIntroManager가 복도 인트로 카메라 팬 동안 안개를 끄고 끝나면 다시 켭니다.
 * 주의: 컷씬 도중 강제로 비활성화된 뒤 복구되지 않으면 시야 효과가 계속 사라질 수 있습니다.
 */using UnityEngine;

// 파일명(Fogeffect.cs)과 클래스명 일치 — 이전 클래스명 FogOfWarController에서 변경
public class Fogeffect : MonoBehaviour
{
    public static Fogeffect Instance { get; private set; }

    [SerializeField] private Material fogMaterial;
    [SerializeField] private Transform player;

    [Header("시야 범위 (타일 수 기준)")]
    [SerializeField] private float visibleTiles = 32f;
    [SerializeField] private float gradientTiles = 8f;

    private Camera _cam;

    // 중복 방지 (CameraFollow, PlayerMovement 와 동일한 패턴)
    private static bool _exists;
    private bool _isOriginal = false;

    void Awake()
    {
        if (_exists)
        {
            Destroy(gameObject);
            return;
        }
        _exists     = true;
        _isOriginal = true;
        Instance    = this;
        DontDestroyOnLoad(transform.root.gameObject);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        _cam = FindActiveCamera();
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
                       UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Camera.main은 씬 전환 후 DontDestroyOnLoad 카메라 태그 미설정 시 null일 수 있어
        // 활성화된 카메라를 폴백으로 탐색
        _cam = FindActiveCamera();
    }

    Camera FindActiveCamera()
    {
        Camera cam = Camera.main;
        if (cam != null) return cam;
        return FindFirstObjectByType<Camera>();
    }

    void OnDisable()
    {
        // 포그 비활성화 시 반경을 크게 설정 → 전체 화면이 밝아짐
        if (fogMaterial == null) return;
        fogMaterial.SetFloat("_InnerRadius", 10f);
        fogMaterial.SetFloat("_OuterRadius", 10f);
    }

    void OnDestroy()
    {
        if (_isOriginal)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            _exists   = false;
            Instance  = null;
        }
    }

    // ── 외부 호출용 ──────────────────────────────────────────────────

    /// <summary>포그 효과를 켜거나 끈다. false면 전체 화면이 밝아짐.</summary>
    public void SetFogActive(bool active) => enabled = active;

    void Update()
    {
        if (player == null || fogMaterial == null || _cam == null) return;

        Vector3 vp = _cam.WorldToViewportPoint(player.position);
        fogMaterial.SetVector("_Center", new Vector4(vp.x, vp.y, 0, 0));

        // orthographicSize 기준으로 단순 계산냥
        float camHeight = _cam.orthographicSize * 2f;
        float innerRadius = visibleTiles / camHeight * 0.5f;
        float outerRadius = (visibleTiles + gradientTiles) / camHeight * 0.5f;

        fogMaterial.SetFloat("_InnerRadius", innerRadius);
        fogMaterial.SetFloat("_OuterRadius", outerRadius);
    }
}
