/*
 * ItemPopup.cs
 * 역할: 아이템 사용, 핫바 등록, 장비 착용/해제 같은 선택 팝업을 표시합니다.
 * 연결: InventoryManager.OnClickItem에서 아이템 타입에 따라 사용 팝업 또는 장비 팝업을 띄웁니다.
 * 주의: IsOpen 값은 인벤토리 E키 토글을 막는 데 쓰이므로 팝업을 닫을 때 반드시 상태를 되돌려야 합니다.
 */using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 인벤토리/핫바 슬롯 클릭 시 "사용 / 장착 / 해제" 선택지를 보여주는 팝업.
/// ShowUsePopup / ShowEquipPopup / ShowHotbarPopup 을 상황에 맞게 호출한다.
/// </summary>
public class ItemPopup : MonoBehaviour
{
    // 소프트 참조
    public static ItemPopup Instance { get; private set; }

    [Header("UI 연결")]
    public GameObject      popupPanel;
    public TextMeshProUGUI titleTxt;
    public Button[]        options;
    public TextMeshProUGUI[] optionTexts;

    private System.Action[] actions = new System.Action[3];

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        popupPanel.SetActive(false);

        for (int i = 0; i < options.Length; i++)
        {
            int idx = i;
            options[i].onClick.AddListener(() =>
            {
                actions[idx]?.Invoke();
                Hide();
            });
        }
    }

    // ── 팝업 종류별 표시 ─────────────────────────────────────────────

    /// <summary>소비 아이템: "사용" / "슬롯 장착" / "닫기"</summary>
    public void ShowUsePopup(ItemData item, System.Action onUse, System.Action onEquipToSlot)
    {
        titleTxt.text = item.itemName;

        optionTexts[0].text = "사용";
        optionTexts[1].text = "슬롯 장착";
        optionTexts[2].text = "닫기";

        options[0].gameObject.SetActive(true);
        options[1].gameObject.SetActive(true);
        options[2].gameObject.SetActive(true);

        actions[0] = onUse;
        actions[1] = onEquipToSlot;
        actions[2] = null;

        popupPanel.SetActive(true);
    }

    /// <summary>장비 아이템: "착용" 또는 "해제" / "닫기"</summary>
    public void ShowEquipPopup(ItemData item, bool isEquipped, System.Action onEquip)
    {
        titleTxt.text = item.itemName;

        optionTexts[0].text = isEquipped ? "해제" : "장착";
        optionTexts[1].text = "닫기";
        optionTexts[2].text = "";

        options[0].gameObject.SetActive(true);
        options[1].gameObject.SetActive(true);
        options[2].gameObject.SetActive(false);

        actions[0] = onEquip;
        actions[1] = null;
        actions[2] = null;

        popupPanel.SetActive(true);
    }

    /// <summary>핫바 슬롯 아이템: "사용" / "해제(인벤토리 반환)" / "닫기"</summary>
    public void ShowHotbarPopup(ItemData item, System.Action onUse, System.Action onUnequip)
    {
        titleTxt.text = item.itemName;

        optionTexts[0].text = "사용";
        optionTexts[1].text = "해제";
        optionTexts[2].text = "닫기";

        options[0].gameObject.SetActive(true);
        options[1].gameObject.SetActive(true);
        options[2].gameObject.SetActive(true);

        actions[0] = onUse;
        actions[1] = onUnequip;
        actions[2] = null;

        popupPanel.SetActive(true);
    }

    public void Hide()
    {
        popupPanel.SetActive(false);
        StopAllCoroutines();
    }

    public bool IsOpen => popupPanel != null && popupPanel.activeSelf;
}



