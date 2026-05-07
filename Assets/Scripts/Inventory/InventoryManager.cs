using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    private Dictionary<ItemData, int> itemCounts = new Dictionary<ItemData, int>();

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

    void Update()
    {
        bool popupOpen = ItemPopup.Instance != null && ItemPopup.Instance.IsOpen;
        if (Input.GetKeyDown(KeyCode.E) && !popupOpen)
        {
            print(isOpen ? "인벤토리 닫음" : "인벤토리 열음");
            ToggleInventory();
        }
    }

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

    public void RemoveItem(ItemData item, int count = 1)
    {
        ItemStack stack = inventory.Find(s => s.item == item);
        if (stack == null) return;
        stack.count -= count;
        if (stack.count <= 0)
            inventory.Remove(stack);
    }

    public void RemoveItemCompletely(ItemData item)
    {
        inventory.RemoveAll(s => s.item == item);
    }

    public int GetItemCount(ItemData item)
    {
        ItemStack stack = inventory.Find(s => s.item == item);
        return stack != null ? stack.count : 0;
    }

    public void ConsumeItemCount(ItemData item)
    {
        RemoveItem(item, 1);
    }

    public void OnClickCategory(int idx)
    {
        categoryIdx = idx;
        currentCategory = (Category)idx;
        RefreshCategoryCursor();
        itemListPanel.SetActive(true);
        RefreshItemList();
    }

    void RefreshCategoryCursor()
    {
        for (int i = 0; i < categoryObjects.Length; i++)
        {
            TextMeshProUGUI txt = categoryObjects[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.color = i == categoryIdx ? Color.white : Color.gray;
        }
    }

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

    public void RefreshStats()
    {
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm == null) return;
        txtHp.text = "HP : " + pm.CurrentHp + " / " + pm.maxHp;
        txtSpeed.text = "SP : " + pm.moveSpeed;
        txtDex.text = "DEX : " + (equippedArmor != null ? equippedArmor.defenseAmount : 0);
    }

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