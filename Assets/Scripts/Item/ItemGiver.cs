using UnityEngine;

// Interactable.onComplete 등에 연결해 플레이어 인벤토리에 아이템을 지급하는 컴포넌트.
// 열쇠 아이템이면 GameManager 플래그도 함께 설정한다.
public class ItemGiver : MonoBehaviour
{
    public ItemData item;
    public int count = 1;

    // 인벤토리에 아이템 추가. 열쇠이면 keyId 플래그와 hasRightCorridorKey 플래그도 설정
    public void GiveItem()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.Log("InventoryManager 없음!");
            return;
        }
        InventoryManager.Instance.AddItem(item, count);
        InventoryManager.Instance.RefreshItemList();
        Debug.Log("아이템 추가됨: " + item.itemName + " x" + count);

        if (item.type == ItemType.Key && GameManager.Instance != null)
        {
            GameManager.Instance.SetFlag(item.keyId);

            if (item.keyId == "RightCorridorKey")
                GameManager.Instance.hasRightCorridorKey = true;
        }
    }
}