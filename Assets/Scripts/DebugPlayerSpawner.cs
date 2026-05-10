using UnityEngine;

// [에디터 전용] 씬을 단독 실행할 때 PlayerMovement가 없으면 playerPrefab을 자동 스폰하는 디버그 컴포넌트.
// DefaultExecutionOrder(-1000)으로 다른 스크립트보다 먼저 실행되어 싱글톤 초기화 순서를 보장한다.
#if UNITY_EDITOR
[DefaultExecutionOrder(-1000)]
public class DebugPlayerSpawner : MonoBehaviour
{
    [Header("Debug Spawn")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint; // null이면 이 오브젝트 위치에 스폰

    // 이미 플레이어가 있으면 스킵. 없으면 spawnPoint 위치에 플레이어 프리팹을 생성하고
    // CameraFollow 타겟도 함께 연결한다.
    void Awake()
    {
        PlayerMovement existingPlayer = FindAnyObjectByType<PlayerMovement>();

        if (existingPlayer != null)
            return;

        if (playerPrefab == null)
        {
            Debug.LogWarning("[DebugPlayerSpawner] playerPrefab이 연결되지 않았습니다.");
            return;
        }

        Vector3 spawnPosition = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        player.tag = "Player";

        CameraFollow cameraFollow = FindAnyObjectByType<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.player = player.transform;
        }

        Debug.Log("[DebugPlayerSpawner] 디버그용 플레이어를 생성했습니다.");
    }
}
#endif
