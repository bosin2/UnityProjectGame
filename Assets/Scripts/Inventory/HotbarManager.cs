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
    public Button[] invenHotbarButtons;

    private int currentIdx = -1;
    private ItemStack[] items = new ItemStack[5];

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

        if (invenHotbarButtons != null)
        {
            for (int i = 0; i < invenHotbarButtons.Length; i++)
            {
                int idx = i;
                invenHotbarButtons[idx].onClick.AddListener(() => OnClickInvenHotbarSlot(idx));
            }
        }
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
        {
            if (items[currentIdx] != null)
                UseItem(currentIdx);
        }
    }

    void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return;
        if (!highlight.gameObject.activeSelf)
            highlight.gameObject.SetActive(true);
        currentIdx = index;
        highlight.position = slots[index].position;
    }

    void OnClickInvenHotbarSlot(int index)
    {
        if (items[index] == null) return;

        ItemPopup.Instance.ShowHotbarPopup(items[index].item,
            () =>
            {
                UseItem(index);
            },
            () =>
            {
                // 핫바 카운트 그대로 인벤토리로 반환냥
                InventoryManager.Instance.AddItem(items[index].item, items[index].count);
                InventoryManager.Instance.RefreshItemList();
                ClearSlot(index);
            }
        );
    }

    void UpdateCountText(int index)
    {
        if (slotCountTexts == null || index >= slotCountTexts.Length) return;
        if (items[index] == null)
        {
            slotCountTexts[index].text = "";
            slotCountTexts[index].gameObject.SetActive(false);
            return;
        }
        int count = items[index].count;
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

    public bool AddItemToSlot(ItemData item, int index, int count = 1)
    {
        if (index < 0 || index >= items.Length)
        {
            Debug.Log("인덱스 범위 초과!");
            return false;
        }
        if (items[index] != null)
        {
            Debug.Log("슬롯 찼!");
            return false;
        }
        items[index] = new ItemStack(item, count);
        slotIcons[index].sprite = item.icon;
        slotIcons[index].enabled = true;
        slotIcons[index].color = new Color(1f, 1f, 1f, 1f);
        if (index + 5 < slotIcons.Length)
        {
            slotIcons[index + 5].sprite = item.icon;
            slotIcons[index + 5].enabled = true;
            slotIcons[index + 5].color = new Color(1f, 1f, 1f, 1f);
        }
        UpdateCountText(index);
        return true;
    }

    void UseItem(int index)
    {
        if (items[index] == null) return;
        ItemData item = items[index].item;

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
        InventoryManager.Instance?.RefreshStats();
    }

    void ConsumeItem(int index)
    {
        if (items[index] == null) return;
        items[index].count--;
        if (items[index].count <= 0)
            ClearSlot(index);
        else
            UpdateCountText(index);
    }

    void ClearSlot(int index)
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

    public ItemData GetItem(int index) => items[index]?.item;
    public void RemoveItem(int index) => ClearSlot(index);
}