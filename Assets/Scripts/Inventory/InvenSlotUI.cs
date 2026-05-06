using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 인벤토리 슬롯 하나의 UI를 담당하는 컴포넌트
public class InvenSlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI countText;

    private ItemData item;

    public void Setup(ItemData data, int count)
    {
        item = data;
        icon.sprite = data.icon;
        nameText.text = data.itemName;
        countText.text = count > 1 ? "x" + count : "";
    }

    public ItemData GetItem() => item;
}