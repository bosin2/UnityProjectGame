using UnityEngine;

#if UNITY_EDITOR
[DefaultExecutionOrder(-1000)]
public class DebugPlayerSpawner : MonoBehaviour
{
    [Header("Debug Spawn")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;

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
