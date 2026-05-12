using UnityEngine;
using UnityEngine.SceneManagement;

// 플레이어가 문 트리거에 진입하면 지정 씬으로 전환하고 스폰 위치를 저장하는 컴포넌트.
// requiredKey를 설정하면 해당 열쇠 보유 시에만 진입 가능하고 선택적으로 열쇠를 소모한다.
public class DoorTrigger : MonoBehaviour
{
    [Header("씬 이름")]
    [SerializeField] private string targetSceneName;

    [Header("스폰 위치")]
    [SerializeField] private Vector2 spawnPosition;
    [SerializeField] private Vector2 spawnDirection = Vector2.down;

    [Header("열쇠 조건 (없으면 자유 진입)")]
    [SerializeField] private ItemData requiredKey;       // 필요한 열쇠 아이템 (null이면 조건 없음)
    [SerializeField] private bool consumeKey = true;     // true면 진입 시 열쇠 1개 소모
    [SerializeField] private string noKeyMessage = "열쇠가 필요합니다."; // 열쇠 없을 때 표시할 메시지

    [Header("공유 잠금 해제 플래그 (같은 구역 문 여러 개일 때)")]
    [SerializeField] private string unlockFlag = ""; // 문 여러 개가 같은 플래그를 공유하면 하나만 열어도 모두 개방

    [Header("진입 불가")]
    [SerializeField] private bool isBlocked = false;
    [SerializeField] private string blockedMessage = "이쪽은 갈 수 없습니다.";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (isBlocked)
        {
            PlayerInteract pi = other.GetComponent<PlayerInteract>();
            if (pi != null)
                pi.StartDialogue(new string[] { blockedMessage });
            return;
        }

        // 열쇠 조건 확인
        if (requiredKey != null)
        {
            // 공유 플래그가 이미 설정돼 있으면 (다른 문에서 이미 열쇠 사용) 바로 통과
            bool alreadyUnlocked = unlockFlag != "" &&
                                   GameManager.Instance != null &&
                                   GameManager.Instance.HasFlag(unlockFlag);

            if (!alreadyUnlocked)
            {
                int count = InventoryManager.Instance != null
                    ? InventoryManager.Instance.GetItemCount(requiredKey)
                    : 0;

                if (count <= 0)
                {
                    // 열쇠 없음: PlayerInteract로 안내 메시지 표시
                    PlayerInteract pi = other.GetComponent<PlayerInteract>();
                    if (pi != null)
                        pi.StartDialogue(new string[] { noKeyMessage });
                    return;
                }

                // 열쇠 소모
                if (consumeKey)
                    InventoryManager.Instance.RemoveItem(requiredKey, 1);

                // 공유 플래그 저장 → 같은 플래그를 가진 다른 문도 영구 개방
                if (unlockFlag != "")
                    GameManager.Instance?.SetFlag(unlockFlag);
            }
        }

        // PlayerMovement.OnSceneLoaded에서 읽어 스폰 위치에 적용
        PlayerPrefs.SetFloat("SpawnX", spawnPosition.x);
        PlayerPrefs.SetFloat("SpawnY", spawnPosition.y);
        PlayerPrefs.SetFloat("SpawnDirX", spawnDirection.x);
        PlayerPrefs.SetFloat("SpawnDirY", spawnDirection.y);

        SceneManager.LoadScene(targetSceneName);
    }
}
