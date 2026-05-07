using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance;

    public RectTransform highlight;
    public RectTransform[] slots;
    public Image[] slotIcons;
    public TextMeshProUGUI[] slotCountTexts;

    private int currentIdx = -1;
    private ItemData[] items = new ItemData[5];

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        highlight.gameObject.SetActive(false);
        if (slotCountTexts != null)
            foreach (var txt in slotCountTexts)
                txt.gameObject.SetActive(false);
    }

    void Update()
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.isOpen) return;

        for (int i = 0; i < 5; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                SelectSlot(i);
        }

        if (currentIdx != -1 && Input.GetMouseButtonDown(1))
            ShowHotbarPopup(currentIdx);
    }

    void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return;
        if (!highlight.gameObject.activeSelf)
            highlight.gameObject.SetActive(true);
        currentIdx = index;
        highlight.position = slots[index].position;
    }

    void ShowHotbarPopup(int index)
    {
        ItemData item = items[index];
        if (item == null) return;

        ItemPopup.Instance.ShowHotbarPopup(item,
            () =>
            {
                UseItem(index);
            },
            () =>
            {
                InventoryManager.Instance.AddItem(item);
                InventoryManager.Instance.RefreshItemList();
                ConsumeItem(index);
            }
        );
    }

    void UpdateCountText(int index)
    {
        if (slotCountTexts == null || index >= slotCountTexts.Length) return;
        ItemData item = items[index];
        if (item == null)
        {
            slotCountTexts[index].text = "";
            slotCountTexts[index].gameObject.SetActive(false);
            return;
        }
        int count = InventoryManager.Instance.GetItemCount(item);
        if (count > 1)
        {
            slotCountTexts[index].text = count.ToString();
            slotCountTexts[index].gameObject.SetActive(true);
        }
        else
        {
            slotCountTexts[index].gameObject.SetActive(false);
        }
    }

    public bool AddItem(ItemData item)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                items[i] = item;
                slotIcons[i].sprite = item.icon;
                slotIcons[i].enabled = true;
                UpdateCountText(i);
                return true;
            }
        }
        Debug.Log("핫바 꽉 찼음");
        return false;
    }

    public bool AddItemToSlot(ItemData item, int index)
    {
        Debug.Log($"슬롯 {index}에 {item.itemName} 시도");
        if (index < 0 || index >= items.Length)
        {
            Debug.Log("인덱스 범위 초과!");
            return false;
        }
        if (items[index] != null)
        {
            Debug.Log("슬롯 참!");
            return false;
        }
        items[index] = item;
        slotIcons[index].sprite = item.icon;
        slotIcons[index].enabled = true;
        slotIcons[index].color = new Color(1f, 1f, 1f, 1f);
        if (index + 5 < slotIcons.Length)
        {
            slotIcons[index + 5].sprite = item.icon;
            slotIcons[index + 5].enabled = true;
            slotIcons[index + 5].color = new Color(1f, 1f, 1f, 1f);
        }
        UpdateCountText(index); // 추가냥
        return true;
    }

    void UseItem(int index)
    {
        ItemData item = items[index];
        if (item == null) return;

        switch (item.type)
        {
            case ItemType.Heal:
                FindFirstObjectByType<PlayerMovement>()?.Heal(item.healAmount);
                ConsumeItem(index);
                break;
            case ItemType.Key:
                ConsumeItem(index);
                break;
            case ItemType.SpeedBoost:
                PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
                pm?.StartCoroutine(pm.SpeedBoostCoroutine(item.speedAmount, item.speedDuration));
                ConsumeItem(index);
                break;
            case ItemType.Armor:
            case ItemType.Shoes:
                break;
        }

        InventoryManager.Instance?.ConsumeItemCount(item);
        InventoryManager.Instance?.RefreshStats();
    }

    void ConsumeItem(int index)
    {
        items[index] = null;
        slotIcons[index].sprite = null;
        slotIcons[index].enabled = false;
        slotIcons[index].color = new Color(1f, 1f, 1f, 0f);
        if (index + 5 < slotIcons.Length)
        {
            slotIcons[index + 5].sprite = null;
            slotIcons[index + 5].enabled = false;
        }
        UpdateCountText(index);
    }

    public ItemData GetItem(int index) => items[index];
    public void RemoveItem(int index) => ConsumeItem(index);
}