/*
 * CameraFollow.cs
 * 역할: 카메라가 플레이어를 따라가도록 위치를 보간하거나 고정하는 플레이어 추적 카메라 스크립트입니다.
 * 연결: CorridorIntroManager가 인트로 카메라 팬 동안 일시적으로 비활성화하고 끝나면 다시 켭니다.
 * 주의: 씬 전환 직후 플레이어 스폰 위치가 적용된 다음 카메라가 따라붙어야 화면 튐이 줄어듭니다.
 */using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 카메라가 플레이어를 따라다니게 하는 컴포넌트.
/// Inspector에서 player를 직접 연결하거나, 비워두면 "Player" 태그로 자동 탐색.
/// DontDestroyOnLoad로 씬 간에 유지되며, 새 씬에 생긴 카메라를 비활성화해
/// AudioListener 중복과 렌더링 충돌을 방지한다.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("추적 대상")]
    public Transform player;

    [Header("오프셋")]
    public float offsetX = 0f;
    public float offsetY = 0f;

    // 씬 전환 후 DontDestroyOnLoad 카메라 중복 방지
    private static bool _exists;
    private bool _isOriginal = false; // 이 인스턴스가 진짜 원본인지 여부

    void Awake()
    {
        if (_exists)
        {
            // 복제본은 그냥 소멸 — OnDestroy에서 _exists를 건드리지 않도록 _isOriginal을 false로 둠
            Destroy(gameObject);
            return;
        }
        _exists    = true;
        _isOriginal = true;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (player == null)
            TryFindPlayer();
    }

    void LateUpdate()
    {
        // 플레이어를 잃었으면 다시 탐색
        if (player == null)
        {
            TryFindPlayer();
            return;
        }

        transform.position = new Vector3(
            player.position.x + offsetX,
            player.position.y + offsetY,
            transform.position.z
        );
    }

    // ── 씬 전환 처리 ──────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬에 생긴 카메라를 비활성화 — DontDestroyOnLoad 씬의 카메라(우리 카메라)만 유지
        // 새 씬의 AudioListener도 함께 비활성화해 "2개 AudioListener" 경고를 제거
        Camera[] allCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera cam in allCams)
        {
            // DontDestroyOnLoad 씬에 속한 카메라는 절대 건드리지 않음
            if (cam.gameObject.scene.name == "DontDestroyOnLoad") continue;

            AudioListener al = cam.GetComponent<AudioListener>();
            if (al != null) al.enabled = false;     // AudioListener 비활성화

            cam.enabled = false;                    // 카메라 렌더링 비활성화
        }

        // 씬 전환 후 플레이어를 잃었으면 다시 탐색
        if (player == null)
            TryFindPlayer();
    }

    void TryFindPlayer()
    {
        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null) player = obj.transform;
    }

    void OnDestroy()
    {
        // 원본만 _exists를 리셋 — 복제본이 소멸될 때는 건드리지 않음
        if (_isOriginal)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _exists = false;
        }
    }
}

