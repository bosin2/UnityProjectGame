using UnityEngine;

/// <summary>
/// 카메라가 플레이어를 따라다니도록 하는 컴포넌트
/// 중복 생성 방지 싱글톤 사용
public class CameraFollow : MonoBehaviour
{
    // 씬 전체에서 카메라가 하나만 존재하도록 관리하는 싱글톤 인스턴스
    private static CameraFollow instance;

    [Header("추적 대상 및 오프셋")]
    public Transform player;    // 추적할 플레이어의 Transform
    public float offsetX = 0f;  // 플레이어 기준 카메라 X축 오프셋
    public float offsetY = 0f;  // 플레이어 기준 카메라 Y축 오프셋

    /// <summary>
    /// 싱글톤 초기화. 이미 카메라가 존재하면 자신을 제거하고,
    /// 없으면 씬 전환 후에도 파괴되지 않도록 등록
    /// </summary>
    void Awake()
    {
        // 이미 카메라 인스턴스가 있으면 중복이므로 이 오브젝트를 제거
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 이 카메라 오브젝트를 유지
    }

    /// <summary>
    /// 시작 시 player가 비어있으면 "Player" 태그로 자동 탐색해서 할당
    /// </summary>
    void Start()
    {
        if (player == null)
        {
            // Inspector에 플레이어가 등록되지 않은 경우, 태그로 자동 검색
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    /// <summary>
    /// 매 프레임 플레이어 위치 + 오프셋으로 카메라 위치를 갱신
    /// </summary>
    void LateUpdate()
    {
        if (player == null) return;

        // Z축은 카메라 고유 깊이값 유지, X/Y만 플레이어 위치 기준으로 이동
        Vector3 newPos = new Vector3(
            player.position.x + offsetX,
            player.position.y + offsetY,
            transform.position.z  // 2D이므로 Z는 그대로 유지
        );
        transform.position = newPos;
    }
}