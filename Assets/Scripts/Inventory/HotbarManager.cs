using UnityEngine;
using UnityEngine.UI;

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance;

    public RectTransform highlight;
    public RectTransform[] slots;
    public Image[] slotIcons;  // 슬롯마다 아이콘 Image 연결



    private int currentIdx = -1;
    private ItemData[] items = new ItemData[5];

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        highlight.gameObject.SetActive(false);
    }

    void Update()
    {
        for (int i = 0; i < 5; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                SelectSlot(i);
        }

        // 슬롯 선택된 상태에서 좌클릭시 사용
        if (currentIdx != -1 && Input.GetMouseButtonDown(0))
            UseItem(currentIdx);
    }

    void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return;
        if (!highlight.gameObject.activeSelf)
            highlight.gameObject.SetActive(true);
        currentIdx = index;
        highlight.position = slots[index].position;
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
                return true;
            }
        }
        Debug.Log("핫바 꽉 찼음");
        return false;
    }
    // 특정 슬롯에 아이템 넣기
    public bool AddItemToSlot(ItemData item, int index)
    {
        if (index < 0 || index >= items.Length) return false;
        if (items[index] != null) return false; // slots → items로 바꾸냥!
        items[index] = item;
        slotIcons[index].sprite = item.icon;
        slotIcons[index].enabled = true;
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
                //DoorManager.Instance?.TryOpenDoor(item.keyId);
                ConsumeItem(index);
                break;

            case ItemType.SpeedBoost:
                PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
                pm?.StartCoroutine(pm.SpeedBoostCoroutine(item.speedAmount, item.speedDuration));
                ConsumeItem(index);
                break;

            case ItemType.Armor:
            case ItemType.Shoes:
                // 장착은 인벤토리에서 처리
                break;
        }
    }

    void ConsumeItem(int index)
    {
        items[index] = null;
        slotIcons[index].sprite = null;
        slotIcons[index].enabled = false;
    }

    public ItemData GetItem(int index) => items[index];
    public void RemoveItem(int index) => ConsumeItem(index);
}