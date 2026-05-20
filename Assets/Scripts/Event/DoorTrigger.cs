using UnityEngine;

/// <summary>
/// 플레이어가 문 트리거에 진입하면 지정 씬으로 전환하는 컴포넌트.
/// 스폰 위치는 SceneLoader를 통해 전달 (PlayerPrefs 사용하지 않음).
/// requiredKey 설정 시 해당 열쇠 보유 여부 확인 후 진입 허가.
/// </summary>
public class DoorTrigger : MonoBehaviour
{
    [Header("전환할 씬")]
    [SerializeField] private string targetSceneName;

    [Header("스폰 위치 / 방향")]
    [SerializeField] private Vector2 spawnPosition;
    [SerializeField] private Vector2 spawnDirection = Vector2.down;

    [Header("열쇠 조건 (null이면 자유 진입)")]
    [SerializeField] private ItemData requiredKey;
    [SerializeField] private bool     consumeKey   = true;
    [SerializeField] private string   noKeyMessage = "열쇠가 필요합니다.";

    [Header("공유 잠금 해제 플래그")]
    [Tooltip("같은 플래그를 공유하는 문들은 하나만 열면 모두 개방됩니다.")]
    [SerializeField] private string unlockFlag = "";

    [Header("진입 차단")]
    [SerializeField] private bool   isBlocked       = false;
    [SerializeField] private string blockedMessage  = " ";

    private InventoryManager inventory;

    void Start()
    {
        inventory = FindFirstObjectByType<InventoryManager>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // 차단된 문
        if (isBlocked)
        {
            other.GetComponent<PlayerInteract>()
                 ?.StartDialogue(new string[] { blockedMessage });
            return;
        }

        // 열쇠 조건 확인
        if (requiredKey != null)
        {
            bool alreadyUnlocked = unlockFlag != ""
                && GameManager.Instance != null
                && GameManager.Instance.HasFlag(unlockFlag);

            if (!alreadyUnlocked)
            {
                // InventoryManager 캐시 갱신 (씬 전환 후 새 인스턴스가 있을 수 있음)
                if (inventory == null)
                    inventory = FindFirstObjectByType<InventoryManager>();

                int count = inventory != null ? inventory.GetItemCount(requiredKey) : 0;

                if (count <= 0)
                {
                    other.GetComponent<PlayerInteract>()
                         ?.StartDialogue(new string[] { noKeyMessage });
                    return;
                }

                if (consumeKey)
                    inventory.RemoveItem(requiredKey, 1);

                if (unlockFlag != "")
                    GameManager.Instance?.SetFlag(unlockFlag);
            }
        }

        // SceneLoader를 통해 씬 전환 (PlayerPrefs 대신 메모리 전달)
        SceneLoader.LoadScene(targetSceneName, spawnPosition, spawnDirection);
    }
}
