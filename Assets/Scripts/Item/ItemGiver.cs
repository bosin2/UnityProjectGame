using UnityEngine;

public class ItemGiver : MonoBehaviour
{
    public ItemData item;

    public void GiveItem()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.Log("InventoryManager 없음!");
            return;
        }
        InventoryManager.Instance.AddItem(item);
        Debug.Log("아이템 추가됨: " + item.itemName);
    }
}