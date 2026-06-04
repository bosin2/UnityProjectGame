/*
 * HotbarManager.cs
 * 역할: 소비 아이템을 빠른 슬롯에 등록하고 단축키/선택 UI로 사용할 수 있게 관리합니다.
 * 연결: InventoryManager의 슬롯 선택 팝업, PlayerHealth 회복/속도 버프, UICanvas 표시 흐름과 연결됩니다.
 * 주의: 인벤토리 열림, 대화, 컷씬 중에는 핫바 입력과 표시가 잠겨야 다른 UI와 충돌하지 않습니다.
 */using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 핫바 슬롯 (1~5번 키) 관리.
/// 숫자키로 슬롯 선택, 우클릭으로 아이템 사용.
/// 인벤토리에서 슬롯으로 아이템 이동 가능.
/// </summary>
public class HotbarManager : MonoBehaviour
{
    // 소프트 참조 (강제 싱글톤 아님)
    public static HotbarManager Instance { get; private set; }

    [Header("UI")]
    public RectTransform   highlight;
    public RectTransform[] slots;
    public Image[]         slotIcons;
    public TextMeshProUGUI[] slotCountTexts;
    public Button[]        invenHotbarButtons;

    [Header("팝업 참조 (인스펙터 연결 또는 자동 탐색)")]
    [SerializeField] private ItemPopup       itemPopup;
    [SerializeField] private InventoryManager inventoryManager;

    private int        currentIdx = -1;
    private ItemStack[] items     = new ItemStack[5];

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        highlight.gameObject.SetActive(false);

        if (slotCountTexts != null)
            foreach (var txt in slotCountTexts)
                txt.gameObject.SetActive(false);

        if (invenHotbarButtons != null)
            for (int i = 0; i < invenHotbarButtons.Length; i++)
            {
                int idx = i;
                invenHotbarButtons[idx].onClick.AddListener(() => OnClickInvenHotbarSlot(idx));
            }

        // Inspector에서 연결되지 않은 경우 씬에서 탐색
        if (itemPopup        == null) itemPopup        = FindFirstObjectByType<ItemPopup>();
        if (inventoryManager == null) inventoryManager = FindFirstObjectByType<InventoryManager>();
    }

    void Update()
    {
        if (inventoryManager != null && inventoryManager.IsOpen) return;

        for (int i = 0; i < 5; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                SelectSlot(i);

        if (currentIdx != -1 && Input.GetMouseButtonDown(1))
            if (items[currentIdx] != null)
                UseItem(currentIdx);
    }

    // ── 슬롯 선택 ─────────────────────────────────────────────────────

    void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return;
        if (!highlight.gameObject.activeSelf) highlight.gameObject.SetActive(true);
        currentIdx = index;
        highlight.position = slots[index].position;
    }

    // ── 인벤토리 내 핫바 슬롯 클릭 ───────────────────────────────────

    void OnClickInvenHotbarSlot(int index)
    {
        if (items[index] == null) return;

        itemPopup?.ShowHotbarPopup(items[index].item,
            () => UseItem(index),
            () =>
            {
                inventoryManager?.AddItem(items[index].item, items[index].count);
                inventoryManager?.RefreshItemList();
                ClearSlot(index);
            }
        );
    }

    // ── 아이템 사용 ───────────────────────────────────────────────────

    void UseItem(int index)
    {
        if (items[index] == null) return;
        ItemData item = items[index].item;

        switch (item.type)
        {
            case ItemType.Heal:
                FindFirstObjectByType<PlayerHealth>()?.Heal(item.healAmount);
                ConsumeItem(index);
                break;
            case ItemType.Key:
                ConsumeItem(index);
                break;
            case ItemType.SpeedBoost:
                PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
                ph?.StartCoroutine(ph.SpeedBoostCoroutine(item.speedAmount, item.speedDuration));
                ConsumeItem(index);
                break;
            case ItemType.Armor:
            case ItemType.Shoes:
                break;
        }

        inventoryManager?.RefreshStats();
    }

    void ConsumeItem(int index)
    {
        if (items[index] == null) return;
        items[index].count--;
        if (items[index].count <= 0) ClearSlot(index);
        else                         UpdateCountText(index);
    }

    // ── 슬롯 관리 ─────────────────────────────────────────────────────

    public bool AddItemToSlot(ItemData item, int index, int count = 1)
    {
        if (index < 0 || index >= items.Length) return false;
        if (items[index] != null) return false;

        items[index]              = new ItemStack(item, count);
        slotIcons[index].sprite   = item.icon;
        slotIcons[index].enabled  = true;
        slotIcons[index].color    = Color.white;
        slotIcons[index].preserveAspect = true;

        // 인벤토리 내 핫바 미러 슬롯 (index + 5)
        if (index + 5 < slotIcons.Length)
        {
            slotIcons[index + 5].sprite  = item.icon;
            slotIcons[index + 5].enabled = true;
            slotIcons[index + 5].color   = Color.white;
            slotIcons[index + 5].preserveAspect = true;
        }

        UpdateCountText(index);
        return true;
    }

    void ClearSlot(int index)
    {
        items[index]             = null;
        slotIcons[index].sprite  = null;
        slotIcons[index].enabled = false;
        slotIcons[index].color   = new Color(1f, 1f, 1f, 0f);

        if (index + 5 < slotIcons.Length)
        {
            slotIcons[index + 5].sprite  = null;
            slotIcons[index + 5].enabled = false;
        }

        UpdateCountText(index);
    }

    void UpdateCountText(int index)
    {
        if (slotCountTexts == null || index >= slotCountTexts.Length) return;

        if (items[index] == null || items[index].count <= 1)
        {
            slotCountTexts[index].text = "";
            slotCountTexts[index].gameObject.SetActive(false);
        }
        else
        {
            slotCountTexts[index].text = items[index].count.ToString();
            slotCountTexts[index].gameObject.SetActive(true);
        }
    }

    public ItemData GetItem(int index) => items[index]?.item;
    public void RemoveItem(int index)  => ClearSlot(index);

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}



