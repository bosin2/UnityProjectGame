using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    public void ShowHotbarSlotPopup(ItemData item, System.Action<int> onSlotSelect)
    {
        titleTxt.text = "슬롯 선택 (1~5)";

        options[0].gameObject.SetActive(false);
        options[1].gameObject.SetActive(false);
        options[2].gameObject.SetActive(false);

        popupPanel.SetActive(true);
        StartCoroutine(WaitForSlotInput(onSlotSelect));
    }

    System.Collections.IEnumerator WaitForSlotInput(System.Action<int> onSlotSelect)
    {
        while (true)
        {
            for (int i = 0; i < 5; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    onSlotSelect?.Invoke(i);
                    Hide();
                    yield break;
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.01f);
        }
    }

    public void Hide()
    {
        popupPanel.SetActive(false);
        StopAllCoroutines();
    }

    public bool IsOpen => popupPanel.activeSelf;
}