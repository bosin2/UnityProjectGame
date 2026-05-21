using UnityEngine;

/// <summary>
/// Interactable.onComplete 등에 연결해 플레이어 인벤토리에 아이템을 지급하는 컴포넌트.
/// 열쇠 아이템이면 GameManager 플래그도 함께 설정한다.
/// InventoryManager는 씬에서 자동 탐색하므로 직접 연결 불필요.
/// </summary>
public class ItemGiver : MonoBehaviour
{
    [Header("지급할 아이템")]
    public ItemData item;
    public int      count = 1;

    private InventoryManager inventory;

    void Start()
    {
        // 씬 내 InventoryManager 탐색 및 캐싱
        inventory = FindFirstObjectByType<InventoryManager>();
    }

    /// <summary>인벤토리에 아이템 추가. Interactable.onComplete 이벤트에 연결해 사용</summary>
    public void GiveItem()
    {
        if (item == null) { Debug.LogWarning("[ItemGiver] item이 설정되지 않았습니다!", this); return; }

        if (inventory == null)
            inventory = FindFirstObjectByType<InventoryManager>();

        // ── 아이템 지급 ──────────────────────────────────────────────
        if (inventory != null)
            inventory.AddItem(item, count);
        else
            Debug.LogWarning("[ItemGiver] InventoryManager를 찾을 수 없습니다!", this);

        Debug.Log($"[ItemGiver] {item.itemName} x{count} 지급");

        // ── 열쇠 플래그 설정 (RefreshItemList 이전에 처리 — 예외로 인한 누락 방지) ──
        if (item.type == ItemType.Key && GameManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(item.keyId))
                GameManager.Instance.SetFlag(item.keyId);

            if (item.keyId == "RightCorridorKey")
                GameManager.Instance.hasRightCorridorKey = true;
        }

        // ── UI 갱신 (인벤토리가 열려 있을 때만) ──────────────────────
        if (inventory != null && inventory.IsOpen)
            inventory.RefreshItemList();
    }
}
