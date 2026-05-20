#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// [에디터 전용] 씬을 단독 실행할 때 플레이어가 없으면 playerPrefab을 자동 스폰.
/// DefaultExecutionOrder(-1000) 으로 다른 스크립트보다 먼저 실행해 초기화 순서를 보장한다.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class DebugPlayerSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform  spawnPoint;   // null이면 이 오브젝트 위치에 스폰

    void Awake()
    {
        // 이미 플레이어가 있으면 스킵
        if (FindAnyObjectByType<PlayerMovement>() != null) return;

        if (playerPrefab == null)
        {
            Debug.LogWarning("[DebugPlayerSpawner] playerPrefab이 연결되지 않았습니다.");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;

        GameObject player = Instantiate(playerPrefab, pos, Quaternion.identity);
        player.tag = "Player";

        // CameraFollow 타겟 자동 연결
        CameraFollow cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.player = player.transform;

        Debug.Log("[DebugPlayerSpawner] 디버그용 플레이어 스폰 완료.");
    }
}
#endif
