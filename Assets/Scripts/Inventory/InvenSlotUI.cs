using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 인벤토리 목록에 표시되는 개별 아이템 슬롯 UI.
// InventoryManager가 동적으로 생성하고 Setup()으로 데이터를 주입한다.
public class InvenSlotUI : MonoBehaviour
{
    public Image icon;
    public Image background;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI countText;

    private ItemData item;

    // 아이템 데이터와 수량을 UI에 반영
    public void Setup(ItemData data, int count)
    {
        icon.preserveAspect = true;
        item = data;
        icon.sprite = data.icon;
        nameText.text = data.itemName;
        countText.text = count > 0 ? "" + count : "";
    }

    public ItemData GetItem() => item;
}