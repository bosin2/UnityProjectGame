using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 인벤토리 슬롯 하나의 UI를 담당하는 컴포넌트
public class InvenSlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI countText;
    public GameObject cursor; // 선택 표시

    private ItemData item;

    public void Setup(ItemData data, int count)
    {
        item = data;
        icon.sprite = data.icon;
        nameText.text = data.itemName;
        countText.text = count > 1 ? "x" + count : "";
    }

    public ItemData GetItem() => item;

    public void SetSelected(bool selected)
    {
        cursor.SetActive(selected);
        GetComponent<Image>().color = selected ?
            new Color(1f, 1f, 0f, 0.3f) :  // 선택시 노란빛냥
            new Color(1f, 1f, 1f, 0f);      // 미선택시 투명냥
    }
}