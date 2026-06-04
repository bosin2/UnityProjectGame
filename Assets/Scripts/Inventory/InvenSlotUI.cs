/*
 * InvenSlotUI.cs
 * 역할: 인벤토리 목록의 단일 슬롯에 아이콘, 이름, 수량 등 아이템 표시 정보를 채웁니다.
 * 연결: InventoryManager.RefreshItemList가 슬롯 프리팹을 생성한 뒤 Setup을 호출합니다.
 * 주의: 슬롯은 런타임에 계속 생성/삭제되므로 내부에 장기 상태를 저장하지 않는 편이 안전합니다.
 */using UnityEngine;
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
