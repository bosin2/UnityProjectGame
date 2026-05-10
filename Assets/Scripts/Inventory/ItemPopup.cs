using UnityEngine;
using TMPro;
using UnityEngine.UI;

// 인벤토리/핫바 슬롯 클릭 시 "사용/장착/해제" 등의 선택지를 보여주는 팝업 싱글톤.
// ShowUsePopup / ShowEquipPopup / ShowHotbarPopup 중 상황에 맞는 메서드를 호출한다.
public class ItemPopup : MonoBehaviour
{
    public static ItemPopup Instance;

    [Header("UI 연결")]
    public GameObject popupPanel;
    public TextMeshProUGUI titleTxt;
    public Button[] options;
    public TextMeshProUGUI[] optionTexts;

    private System.Action[] actions = new System.Action[3];

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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

    // 소비 아이템용 팝업: "사용" / "슬롯 장착" / "닫기"
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

    // 장비 아이템용 팝업: 착용 중이면 "해제", 아니면 "장착" / "닫기"
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

    // 핫바 슬롯 아이템용 팝업: "사용" / "해제(인벤토리 반환)" / "닫기"
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

    public bool IsOpen => popupPanel.activeSelf;
}