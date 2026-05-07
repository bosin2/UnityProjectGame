using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InvenSlotUI : MonoBehaviour
{
    public Image icon;
    public Image background;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI countText;

    private ItemData item;

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