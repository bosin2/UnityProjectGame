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
    public Transform hotbarOriginalParent;
    public Transform hotbarInvenParent;

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

    private enum Category { Key, Use, Equip }
    private Category currentCategory = Category.Key;

    private enum InvenState { Category, ItemList }
    private InvenState state = InvenState.Category;

    private List<ItemData> inventory = new List<ItemData>();
    private List<InvenSlotUI> slotUIs = new List<InvenSlotUI>();

    private int categoryIdx = 0;
    private int itemIdx = 0;

    private ItemData equippedArmor;
    private ItemData equippedShoes;

    public bool isOpen = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        inventoryUI.SetActive(false);
        itemListPanel.SetActive(false);
    }

    void Update()
    {
        bool popupOpen = ItemPopup.Instance != null && ItemPopup.Instance.IsOpen;

        if (Input.GetKeyDown(KeyCode.E) && !popupOpen)
            ToggleInventory();

        if (!isOpen || popupOpen) return;

        if (state == InvenState.Category)
            HandleCategoryInput();
        else
            HandleItemInput();
    }

    void HandleCategoryInput()
    {
        if (Input.GetKeyDown(KeyCode.W))
            MoveCategoryIdx(-1);
        if (Input.GetKeyDown(KeyCode.S))
            MoveCategoryIdx(1);
        if (Input.GetKeyDown(KeyCode.Return))
            EnterCategory();
    }

    void HandleItemInput()
    {
        if (Input.GetKeyDown(KeyCode.W))
            MoveItemIdx(-1);
        if (Input.GetKeyDown(KeyCode.S))
            MoveItemIdx(1);
        if (Input.GetKeyDown(KeyCode.Escape))
            ExitToCategory();
        if (Input.GetKeyDown(KeyCode.Return))
            OnItemEnter();
    }

    void MoveCategoryIdx(int dir)
    {
        categoryIdx = Mathf.Clamp(categoryIdx + dir, 0, categoryObjects.Length - 1);
        RefreshCategoryCursor();
    }

    void RefreshCategoryCursor()
    {
        currentCategory = (Category)categoryIdx;
        for (int i = 0; i < categoryObjects.Length; i++)
        {
            TextMeshProUGUI txt = categoryObjects[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.color = i == categoryIdx ? Color.white : Color.gray;
        }
    }

    void EnterCategory()
    {
        currentCategory = (Category)categoryIdx;
        state = InvenState.ItemList;
        itemIdx = 0;
        itemListPanel.SetActive(true);
        RefreshItemList();
    }

    void ExitToCategory()
    {
        state = InvenState.Category;
        itemListPanel.SetActive(false);
        foreach (var slot in slotUIs)
            Destroy(slot.gameObject);
        slotUIs.Clear();
    }

    void MoveItemIdx(int dir)
    {
        if (slotUIs.Count == 0) return;
        itemIdx = Mathf.Clamp(itemIdx + dir, 0, slotUIs.Count - 1);
        RefreshItemHighlight();
        RefreshDescription();
    }

    void OnItemEnter()
    {
        if (slotUIs.Count == 0 || itemIdx >= slotUIs.Count) return;
        ItemData item = slotUIs[itemIdx].GetItem();

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
                    inventory.Remove(item);
                    RefreshItemList();
                },
                () =>
                {
                    ItemPopup.Instance.ShowHotbarSlotPopup(item, (slotIndex) =>
                    {
                        HotbarManager.Instance.AddItemToSlot(item, slotIndex);
                        inventory.Remove(item);
                        RefreshItemList();
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
    }

    void ToggleEquip(ItemData item)
    {
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();

        if (item.type == ItemType.Armor)
        {
            if (equippedArmor == item)
                equippedArmor = null;
            else
                equippedArmor = item;
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

    void RefreshItemList()
    {
        foreach (var slot in slotUIs)
            Destroy(slot.gameObject);
        slotUIs.Clear();

        List<ItemData> items = GetCategoryItems();
        foreach (var item in items)
        {
            GameObject obj = Instantiate(itemSlotPrefab, itemGridGroup);
            InvenSlotUI slotUI = obj.GetComponent<InvenSlotUI>();
            slotUI.Setup(item, 1);
            slotUIs.Add(slotUI);
        }

        itemIdx = 0;
        RefreshItemHighlight();
        RefreshDescription();
    }

    void RefreshItemHighlight()
    {
        for (int i = 0; i < slotUIs.Count; i++)
            slotUIs[i].SetSelected(i == itemIdx);
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

    void RefreshStats()
    {
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm == null) return;
        txtHp.text = "HP : " + pm.CurrentHp + " / " + pm.maxHp;
        txtSpeed.text = "SP : " + pm.moveSpeed;
        txtDex.text = "DEX : " + (equippedArmor != null ? equippedArmor.defenseAmount : 0);
    }

    List<ItemData> GetCategoryItems()
    {
        switch (currentCategory)
        {
            case Category.Key:
                return inventory.FindAll(i => i.type == ItemType.Key);
            case Category.Use:
                return inventory.FindAll(i => i.type == ItemType.Heal ||
                                             i.type == ItemType.SpeedBoost);
            case Category.Equip:
                return inventory.FindAll(i => i.type == ItemType.Armor ||
                                             i.type == ItemType.Shoes);
            default:
                return new List<ItemData>();
        }
    }

    public void AddItem(ItemData item)
    {
        inventory.Add(item);
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;
        inventoryUI.SetActive(isOpen);

        if (isOpen)
        {
            Time.timeScale = 0f;
            hotbarUI.transform.SetParent(hotbarInvenParent);
            hotbarUI.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            state = InvenState.Category;
            categoryIdx = 0;
            itemListPanel.SetActive(false);
            RefreshCategoryCursor();
            RefreshStats();
        }
        else
        {
            hotbarUI.transform.SetParent(hotbarOriginalParent);
            hotbarUI.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            ExitToCategory();
            Time.timeScale = 1f;
        }
    }
}