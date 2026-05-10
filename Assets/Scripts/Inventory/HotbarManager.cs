using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 핫바 슬롯(1~5번 키)을 관리하는 싱글톤.
// 숫자키로 슬롯 선택, 우클릭으로 아이템 사용. 인벤토리에서 슬롯으로 아이템 이동 가능.
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

    // 슬롯 하이라이트를 선택된 슬롯 위치로 이동
    void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return;
        if (!highlight.gameObject.activeSelf)
            highlight.gameObject.SetActive(true);
        currentIdx = index;
        highlight.position = slots[index].position;
    }

    // 인벤토리 내 핫바 슬롯 클릭 시 사용/해제 팝업 표시
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

    // 슬롯 수량 텍스트 갱신. 2개 이상일 때만 표시
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

    // 지정 슬롯에 아이템 추가. 빈 슬롯에만 배치 가능하며 아이콘도 함께 갱신
    public bool AddItemToSlot(ItemData item, int index, int count = 1)
    {
        slotIcons[index].preserveAspect = true;
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
            slotIcons[index + 5].preserveAspect = true;
        }
        UpdateCountText(index);
        return true;
    }

    // 슬롯의 아이템을 타입에 따라 즉시 사용하고 1개 소모
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

    // 아이템 수량 1 감소. 0 이하가 되면 슬롯 초기화
    void ConsumeItem(int index)
    {
        if (items[index] == null) return;
        items[index].count--;
        if (items[index].count <= 0)
            ClearSlot(index);
        else
            UpdateCountText(index);
    }

    // 슬롯 아이템, 아이콘, 수량 텍스트를 모두 초기화
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

    // HotbarUI.cs 에서 이관 — 핫바 GameObject 표시/숨김
    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}