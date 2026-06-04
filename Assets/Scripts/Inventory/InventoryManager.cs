/*
 * InventoryManager.cs
 * 역할: 인벤토리 데이터, 카테고리 UI, 아이템 사용, 장비 효과, 핫바 등록을 통합 관리합니다.
 * 연결: ItemGiver, DoorTrigger, HotbarManager, PlayerHealth, PlayerMovement와 직접 연결됩니다.
 * 주의: InventoryManager는 씬마다 새로 생기는 소프트 싱글톤이므로 씬 전환 뒤 캐시된 참조를 다시 찾는 코드가 필요합니다.
 */using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 인벤토리 UI + 아이템 데이터 + 카테고리 전환 + 장비 착용/해제 통합 관리.
/// E키로 열고 닫으며, 열리면 Time.timeScale=0 으로 게임 일시정지.
///
/// [의존성 — Inspector에서 연결]
/// itemPopup, slotSelectPopup, hotbarManager 는 같은 씬의 UI 오브젝트를 드래그 연결.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    // 씬 내에서 편리하게 접근하기 위한 소프트 참조 (강제 싱글톤 아님)
    public static InventoryManager Instance { get; private set; }

    [Header("UI 연결")]
    public GameObject inventoryUI;
    public GameObject hotbarUI;

    [Header("카테고리")]
    public GameObject[] categoryObjects;

    [Header("아이템 목록")]
    public GameObject itemSlotPrefab;
    public Transform  itemGridGroup;
    public GameObject itemListPanel;

    [Header("설명창")]
    public TextMeshProUGUI descriptionTxt;

    [Header("스탯 텍스트")]
    public TextMeshProUGUI txtHp;
    public TextMeshProUGUI txtSpeed;
    public TextMeshProUGUI txtDex;

    [Header("기본 아이템")]
    public ItemData[] defaultItems;
    public int[]      defaultItemCounts;
    public ItemData   defaultArmor;
    public ItemData   defaultShoes;

    [Header("UI 팝업 참조 (인스펙터에서 연결)")]
    [SerializeField] private ItemPopup       itemPopup;
    [SerializeField] private SlotSelectPopup slotSelectPopup;
    [SerializeField] private HotbarManager   hotbarManager;

    // ── 내부 상태 ──────────────────────────────────────────────────────
    private enum Category { Key, Use, Equip }
    private Category currentCategory = Category.Key;

    private List<ItemStack>    inventory = new List<ItemStack>();
    private List<InvenSlotUI>  slotUIs   = new List<InvenSlotUI>();

    private int categoryIdx = 0;
    private int itemIdx     = 0;

    private ItemData equippedArmor;
    private ItemData equippedShoes;

    private PlayerHealth playerHealth;

    public bool IsOpen { get; private set; } = false;

    // ── 초기화 ──────────────────────────────────────────────────────

    void Awake()
    {
        // 소프트 참조: 마지막에 생성된 인스턴스가 캐싱됨 (강제 제거 없음)
        Instance = this;
        SetInventoryOpen(false);
    }

    void Start()
    {
        SetInventoryOpen(false);
        itemListPanel.SetActive(false);

        // Inspector에서 연결되지 않은 팝업은 씬에서 탐색
        if (itemPopup      == null) itemPopup      = FindFirstObjectByType<ItemPopup>();
        if (slotSelectPopup == null) slotSelectPopup = FindFirstObjectByType<SlotSelectPopup>();
        if (hotbarManager  == null) hotbarManager  = FindFirstObjectByType<HotbarManager>();

        // 기본 아이템 지급
        if (defaultItems != null)
        {
            for (int i = 0; i < defaultItems.Length; i++)
            {
                if (defaultItems[i] == null) continue;
                int count = (defaultItemCounts != null && i < defaultItemCounts.Length)
                    ? defaultItemCounts[i] : 1;
                AddItem(defaultItems[i], count);
            }
        }

        if (defaultArmor != null) { AddItem(defaultArmor, 1); ToggleEquip(defaultArmor); }
        if (defaultShoes != null)
        {
            AddItem(defaultShoes, 1);
            // ToggleEquip 대신 직접 적용 — SetEquipSpeedBonus는 절댓값이라 씬마다 호출해도 안전
            equippedShoes = defaultShoes;
            FindFirstObjectByType<PlayerMovement>()
                ?.SetEquipSpeedBonus(defaultShoes.speedAmount);
        }

        BindPlayerHealth();
    }

    void BindPlayerHealth()
    {
        var ph = FindFirstObjectByType<PlayerHealth>();
        if (ph == null || ph == playerHealth) return;
        if (playerHealth != null) playerHealth.OnHPChanged -= OnPlayerHPChanged;
        playerHealth = ph;
        playerHealth.OnHPChanged += OnPlayerHPChanged;
    }

    void OnPlayerHPChanged(int current, int max)
    {
        if (IsOpen) RefreshStats();
    }

    void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnHPChanged -= OnPlayerHPChanged;
    }

    void Update()
    {
        bool popupOpen = itemPopup != null && itemPopup.IsOpen;
        if (Input.GetKeyDown(KeyCode.E) && !popupOpen)
            ToggleInventory();
    }

    // ── 아이템 CRUD ───────────────────────────────────────────────────

    public void AddItem(ItemData item, int count = 1)
    {
        ItemStack existing = inventory.Find(s => s.item == item);
        if (existing != null)
        {
            if (item.type == ItemType.Key) return; // 열쇠 중복 무시
            existing.count += count;
        }
        else
        {
            inventory.Add(new ItemStack(item, count));
        }
        if (IsOpen) RefreshItemList();
    }

    public void RemoveItem(ItemData item, int count = 1)
    {
        ItemStack stack = inventory.Find(s => s.item == item);
        if (stack == null) return;
        stack.count -= count;
        if (stack.count <= 0) inventory.Remove(stack);
        if (IsOpen) RefreshItemList();
    }

    public void RemoveItemCompletely(ItemData item)
    {
        inventory.RemoveAll(s => s.item == item);
        if (IsOpen) RefreshItemList();
    }

    public int GetItemCount(ItemData item)
    {
        ItemStack stack = inventory.Find(s => s.item == item);
        return stack != null ? stack.count : 0;
    }

    public void ConsumeItemCount(ItemData item) => RemoveItem(item, 1);

    // ── 카테고리 ──────────────────────────────────────────────────────

    public void OnClickCategory(int idx)
    {
        categoryIdx      = idx;
        currentCategory  = (Category)idx;
        RefreshCategoryCursor();
        itemListPanel.SetActive(true);
        RefreshItemList();
    }

    void RefreshCategoryCursor()
    {
        for (int i = 0; i < categoryObjects.Length; i++)
        {
            var txt = categoryObjects[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.color = i == categoryIdx ? Color.white : Color.gray;
        }
    }

    // ── 슬롯 클릭 ─────────────────────────────────────────────────────

    public void OnClickItem(int idx)
    {
        if (idx >= slotUIs.Count) return;
        itemIdx = idx;
        RefreshDescription();

        ItemData item = slotUIs[idx].GetItem();
        if (item.type == ItemType.Key) return;

        if (currentCategory == Category.Equip)
        {
            bool equipped = equippedArmor == item || equippedShoes == item;
            itemPopup?.ShowEquipPopup(item, equipped, () =>
            {
                ToggleEquip(item);
                RefreshStats();
            });
        }
        else
        {
            itemPopup?.ShowUsePopup(item,
                () => UseItemDirectly(item),
                () =>
                {
                    slotSelectPopup?.Show(item, (slotIndex) =>
                    {
                        int count = GetItemCount(item);
                        if (hotbarManager != null && hotbarManager.AddItemToSlot(item, slotIndex, count))
                        {
                            RemoveItemCompletely(item);
                        }
                    });
                }
            );
        }
    }

    void UseItemDirectly(ItemData item)
    {
        // 플레이어 컴포넌트를 씬에서 탐색
        switch (item.type)
        {
            case ItemType.Heal:
                FindFirstObjectByType<PlayerHealth>()?.Heal(item.healAmount);
                break;
            case ItemType.SpeedBoost:
                PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
                ph?.StartCoroutine(ph.SpeedBoostCoroutine(item.speedAmount, item.speedDuration));
                break;
            case ItemType.Key:
                break;
        }
        RemoveItem(item, 1);
    }

    void ToggleEquip(ItemData item)
    {
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();

        if (item.type == ItemType.Armor)
        {
            equippedArmor = equippedArmor == item ? null : item;
        }
        else if (item.type == ItemType.Shoes)
        {
            if (equippedShoes == item)
            {
                // 해제: 보너스 0으로 (절댓값 설정이라 누적 없음)
                equippedShoes = null;
                pm?.SetEquipSpeedBonus(0f);
            }
            else
            {
                // 장착: 새 신발의 보너스로 교체 (절댓값이라 이전 값 덮어씀)
                equippedShoes = item;
                pm?.SetEquipSpeedBonus(item.speedAmount);
            }
        }
    }

    // ── UI 갱신 ───────────────────────────────────────────────────────

    public void RefreshItemList()
    {
        foreach (var slot in slotUIs) Destroy(slot.gameObject);
        slotUIs.Clear();

        List<ItemStack> stacks = GetCategoryStacks();
        for (int i = 0; i < stacks.Count; i++)
        {
            int idx = i;
            GameObject obj    = Instantiate(itemSlotPrefab, itemGridGroup);
            InvenSlotUI slotUI = obj.GetComponent<InvenSlotUI>();
            slotUI.Setup(stacks[i].item, stacks[i].count);

            Button btn = obj.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => OnClickItem(idx));
            slotUIs.Add(slotUI);
        }

        itemIdx = 0;
        RefreshDescription();
    }

    void RefreshDescription()
    {
        if (slotUIs.Count == 0 || itemIdx >= slotUIs.Count)
        { descriptionTxt.text = ""; return; }

        ItemData item = slotUIs[itemIdx].GetItem();
        string desc   = item.itemName + "\n" + item.description + "\n";

        switch (item.type)
        {
            case ItemType.Heal:       desc += "HP +" + item.healAmount; break;
            case ItemType.SpeedBoost: desc += "속도 +" + item.speedAmount + " / " + item.speedDuration + "초"; break;
            case ItemType.Armor:      desc += "방어력 +" + item.defenseAmount; break;
            case ItemType.Shoes:      desc += "이동속도 +" + item.speedAmount; break;
            case ItemType.Key:        desc += "어딘가에 쓸 수 있을 것 같다"; break;
        }

        descriptionTxt.text = desc;
    }

    public void RefreshStats()
    {
        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (ph == null || pm == null) return;

        txtHp.text    = "HP : " + ph.CurrentHp + " / " + ph.maxHp;
        txtSpeed.text = "SP : " + pm.moveSpeed;
        txtDex.text   = "DEX : " + (equippedArmor != null ? equippedArmor.defenseAmount : 0);
    }

    List<ItemStack> GetCategoryStacks()
    {
        switch (currentCategory)
        {
            case Category.Key:
                return inventory.FindAll(s => s.item.type == ItemType.Key);
            case Category.Use:
                return inventory.FindAll(s => s.item.type == ItemType.Heal
                                           || s.item.type == ItemType.SpeedBoost);
            case Category.Equip:
                return inventory.FindAll(s => s.item.type == ItemType.Armor
                                           || s.item.type == ItemType.Shoes);
            default:
                return new List<ItemStack>();
        }
    }

    // ── 열기/닫기 ─────────────────────────────────────────────────────

    public void ToggleInventory()
    {
        SetInventoryOpen(!IsOpen);

        if (IsOpen)
        {
            Time.timeScale = 0f;
            BindPlayerHealth();
            RefreshStats();
            OnClickCategory(0);
        }
        else
        {
            foreach (var slot in slotUIs) Destroy(slot.gameObject);
            slotUIs.Clear();
            Time.timeScale = 1f;
        }
    }

    void SetInventoryOpen(bool open)
    {
        IsOpen = open;
        if (inventoryUI != null) inventoryUI.SetActive(open);
        if (hotbarUI    != null) hotbarUI.SetActive(!open);
        if (!open) Time.timeScale = 1f;
    }
}



