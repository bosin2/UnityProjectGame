using UnityEngine;

// 카메라가 플레이어를 따라다니도록 하는 컴포넌트. 씬 전환 후에도 유지되는 싱글톤
public class CameraFollow : MonoBehaviour
{
    private static CameraFollow instance;

    [Header("추적 대상 및 오프셋")]
    public Transform player;
    public float offsetX = 0f;
    public float offsetY = 0f;

    void Awake()
    {
        // 중복 카메라 방지: 이미 존재하면 새 오브젝트 제거
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Inspector에 플레이어가 없으면 태그로 자동 탐색
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    // 매 프레임 플레이어 위치 + 오프셋으로 카메라 이동 (Z축 고정)
    void LateUpdate()
    {
        if (player == null) return;

        transform.position = new Vector3(
            player.position.x + offsetX,
            player.position.y + offsetY,
            transform.position.z
        );
    }
}
