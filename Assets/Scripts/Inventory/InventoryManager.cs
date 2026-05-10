using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// 인벤토리 UI, 아이템 데이터, 카테고리 전환, 장비 착용/해제를 통합 관리하는 싱글톤.
// E키로 열고 닫으며, 열리면 Time.timeScale=0f로 게임을 일시정지한다.
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("UI 연결")]
    public GameObject inventoryUI;
    public GameObject hotbarUI;

    [Header("카테고리")]
    public GameObject[] categoryObjects;

    [Header("아이템 목록")]
    public GameObject itemSlotPrefab;
    public Transform itemGridGroup;
    public GameObject itemListPanel;

    [Header("설명창")]
    public TextMeshProUGUI descriptionTxt;

    [Header("스탯")]
    public TextMeshProUGUI txtHp;
    public TextMeshProUGUI txtSpeed;
    public TextMeshProUGUI txtDex;

    [Header("기본 아이템")]
    public ItemData[] defaultItems;
    public int[] defaultItemCounts;
    public ItemData defaultArmor;
    public ItemData defaultShoes;

    private enum Category { Key, Use, Equip }
    private Category currentCategory = Category.Key;

    private List<ItemStack> inventory = new List<ItemStack>();
    private List<InvenSlotUI> slotUIs = new List<InvenSlotUI>();

    private int categoryIdx = 0;
    private int itemIdx = 0;

    private ItemData equippedArmor;
    private ItemData equippedShoes;

    public bool isOpen = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        SetInventoryOpen(false);
    }

    // 기본 아이템·방어구·신발을 인벤토리에 추가하고 장비 슬롯에 자동 착용
    void Start()
    {
        SetInventoryOpen(false);
        itemListPanel.SetActive(false);

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
        if (defaultShoes != null) { AddItem(defaultShoes, 1); ToggleEquip(defaultShoes); }
    }

    // E키로 인벤토리 열기/닫기. 팝업이 열려 있으면 무시
    void Update()
    {
        bool popupOpen = ItemPopup.Instance != null && ItemPopup.Instance.IsOpen;
        if (Input.GetKeyDown(KeyCode.E) && !popupOpen)
        {
            print(isOpen ? "인벤토리 닫음" : "인벤토리 열음");
            ToggleInventory();
        }
    }

    // 아이템을 인벤토리에 추가. 열쇠는 중복 획득 무시, 그 외는 수량 누적
    public void AddItem(ItemData item, int count = 1)
    {
        ItemStack existing = inventory.Find(s => s.item == item);
        if (existing != null)
        {
            // 열쇠면 중복 획득 무시
            if (item.type == ItemType.Key) return;
            existing.count += count;
        }
        else
        {
            inventory.Add(new ItemStack(item, count));
        }
    }

    // 지정 수량만큼 제거. 수량이 0 이하면 스택 삭제
    public void RemoveItem(ItemData item, int count = 1)
    {
        ItemStack stack = inventory.Find(s => s.item == item);
        if (stack == null) return;
        stack.count -= count;
        if (stack.count <= 0)
            inventory.Remove(stack);
    }

    // 수량과 무관하게 해당 아이템 스택 전체 삭제
    public void RemoveItemCompletely(ItemData item)
    {
        inventory.RemoveAll(s => s.item == item);
    }

    // 인벤토리에서 해당 아이템의 현재 보유 수량 반환
    public int GetItemCount(ItemData item)
    {
        ItemStack stack = inventory.Find(s => s.item == item);
        return stack != null ? stack.count : 0;
    }

    // 아이템 1개 소모 (RemoveItem 래퍼)
    public void ConsumeItemCount(ItemData item)
    {
        RemoveItem(item, 1);
    }

    // 카테고리 버튼 클릭 시 현재 카테고리 전환 및 아이템 목록 갱신
    public void OnClickCategory(int idx)
    {
        categoryIdx = idx;
        currentCategory = (Category)idx;
        RefreshCategoryCursor();
        itemListPanel.SetActive(true);
        RefreshItemList();
    }

    // 선택된 카테고리 버튼 텍스트만 흰색, 나머지는 회색으로 강조 표시
    void RefreshCategoryCursor()
    {
        for (int i = 0; i < categoryObjects.Length; i++)
        {
            TextMeshProUGUI txt = categoryObjects[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.color = i == categoryIdx ? Color.white : Color.gray;
        }
    }

    // 아이템 슬롯 클릭: 장비 카테고리면 착용/해제 팝업, 그 외에는 사용/슬롯 장착 팝업 표시
    public void OnClickItem(int idx)
    {
        if (idx >= slotUIs.Count) return;
        itemIdx = idx;
        RefreshDescription();

        ItemData item = slotUIs[idx].GetItem();

        if (currentCategory == Category.Equip)
        {
            bool isEquipped = equippedArmor == item || equippedShoes == item;
            ItemPopup.Instance.ShowEquipPopup(item, isEquipped, () =>
            {
                ToggleEquip(item);
                RefreshStats();
            });
        }
        else
        {
            ItemPopup.Instance.ShowUsePopup(item,
                () =>
                {
                    UseItemDirectly(item);
                },
                () =>
                {
                    SlotSelectPopup.Instance.Show(item, (slotIndex) =>
                    {
                        int count = GetItemCount(item);
                        if (HotbarManager.Instance.AddItemToSlot(item, slotIndex, count))
                        {
                            RemoveItemCompletely(item);
                            RefreshItemList();
                        }
                    });
                }
            );
        }
    }

    // 인벤토리에서 아이템을 직접 사용 (핫바 거치지 않음). 사용 후 목록 갱신
    void UseItemDirectly(ItemData item)
    {
        switch (item.type)
        {
            case ItemType.Heal:
                FindFirstObjectByType<PlayerMovement>()?.Heal(item.healAmount);
                break;
            case ItemType.SpeedBoost:
                PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
                pm?.StartCoroutine(pm.SpeedBoostCoroutine(item.speedAmount, item.speedDuration));
                break;
            case ItemType.Key:
                break;
        }
        RemoveItem(item, 1);
        RefreshItemList();
    }

    // 방어구/신발 착용 토글. 신발은 이동속도에 즉시 반영
    void ToggleEquip(ItemData item)
    {
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();

        if (item.type == ItemType.Armor)
        {
            if (equippedArmor == item) equippedArmor = null;
            else equippedArmor = item;
        }
        else if (item.type == ItemType.Shoes)
        {
            if (equippedShoes == item)
            {
                equippedShoes = null;
                if (pm != null) pm.moveSpeed -= item.speedAmount;
            }
            else
            {
                equippedShoes = item;
                if (pm != null) pm.moveSpeed += item.speedAmount;
            }
        }
    }

    // 현재 카테고리 아이템 슬롯 UI를 전부 재생성
    public void RefreshItemList()
    {
        foreach (var slot in slotUIs)
            Destroy(slot.gameObject);
        slotUIs.Clear();

        List<ItemStack> stacks = GetCategoryStacks();
        for (int i = 0; i < stacks.Count; i++)
        {
            int idx = i;
            GameObject obj = Instantiate(itemSlotPrefab, itemGridGroup);
            InvenSlotUI slotUI = obj.GetComponent<InvenSlotUI>();
            slotUI.Setup(stacks[i].item, stacks[i].count);

            Button btn = obj.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => OnClickItem(idx));
            slotUIs.Add(slotUI);
        }

        itemIdx = 0;
        RefreshDescription();
    }

    // 선택된 슬롯의 아이템 설명을 하단 텍스트에 업데이트
    void RefreshDescription()
    {
        if (slotUIs.Count == 0 || itemIdx >= slotUIs.Count)
        {
            descriptionTxt.text = "";
            return;
        }

        ItemData item = slotUIs[itemIdx].GetItem();
        string desc = item.itemName + "\n" + item.description + "\n";

        switch (item.type)
        {
            case ItemType.Heal:
                desc += "HP +" + item.healAmount; break;
            case ItemType.SpeedBoost:
                desc += "속도 +" + item.speedAmount + " / " + item.speedDuration + "초"; break;
            case ItemType.Armor:
                desc += "방어력 +" + item.defenseAmount; break;
            case ItemType.Shoes:
                desc += "이동속도 +" + item.speedAmount; break;
            case ItemType.Key:
                desc += "어딘가에 쓸 수 있을 것 같다"; break;
        }

        descriptionTxt.text = desc;
    }

    // 플레이어 현재 스탯(HP, 속도, 방어력)을 인벤토리 스탯 텍스트에 반영
    public void RefreshStats()
    {
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm == null) return;
        txtHp.text = "HP : " + pm.CurrentHp + " / " + pm.maxHp;
        txtSpeed.text = "SP : " + pm.moveSpeed;
        txtDex.text = "DEX : " + (equippedArmor != null ? equippedArmor.defenseAmount : 0);
    }

    // 현재 카테고리에 해당하는 ItemStack 목록 반환
    List<ItemStack> GetCategoryStacks()
    {
        switch (currentCategory)
        {
            case Category.Key:
                return inventory.FindAll(s => s.item.type == ItemType.Key);
            case Category.Use:
                return inventory.FindAll(s => s.item.type == ItemType.Heal ||
                                             s.item.type == ItemType.SpeedBoost);
            case Category.Equip:
                return inventory.FindAll(s => s.item.type == ItemType.Armor ||
                                             s.item.type == ItemType.Shoes);
            default:
                return new List<ItemStack>();
        }
    }

    // 인벤토리 열기/닫기. 열리면 timeScale=0, 닫히면 슬롯 UI 정리 후 timeScale=1
    public void ToggleInventory()
    {
        SetInventoryOpen(!isOpen);

        if (isOpen)
        {
            Time.timeScale = 0f;
            categoryIdx = 0;
            currentCategory = Category.Key;
            itemListPanel.SetActive(false);
            RefreshCategoryCursor();
            RefreshStats();
        }
        else
        {
            foreach (var slot in slotUIs)
                Destroy(slot.gameObject);
            slotUIs.Clear();
            Time.timeScale = 1f;
        }
    }

    // 인벤토리 UI 표시/숨김 및 핫바 역전 처리
    void SetInventoryOpen(bool open)
    {
        isOpen = open;

        if (inventoryUI != null)
            inventoryUI.SetActive(open);

        if (hotbarUI != null)
            hotbarUI.SetActive(!open);

        if (!open)
            Time.timeScale = 1f;
    }
}