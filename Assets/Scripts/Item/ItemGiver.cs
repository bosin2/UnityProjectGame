using UnityEngine;

public class ItemGiver : MonoBehaviour
{
    public ItemData item;
    public int count = 1;

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
    }
}