using UnityEngine;

// 파일명(Fogeffect.cs)과 클래스명 일치 — 이전 클래스명 FogOfWarController에서 변경
public class Fogeffect : MonoBehaviour
{
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

    void OnDestroy()
    {
        if (_isOriginal)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            _exists = false;
        }
    }

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