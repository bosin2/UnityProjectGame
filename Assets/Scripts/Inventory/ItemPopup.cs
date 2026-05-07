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

    private int currentOptionIdx = 0;
    private int optionCount = 0;
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

    void Update()
    {
        if (!popupPanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.W))
            MoveOption(-1);
        if (Input.GetKeyDown(KeyCode.S))
            MoveOption(1);
        if (Input.GetKeyDown(KeyCode.Return))
            SelectOption();
    }

    void MoveOption(int dir)
    {
        currentOptionIdx = Mathf.Clamp(currentOptionIdx + dir, 0, optionCount - 1);
        RefreshHighlight();
    }

    void RefreshHighlight()
    {
        for (int i = 0; i < options.Length; i++)
        {
            optionTexts[i].color = i == currentOptionIdx ? Color.black : Color.gray;
        }
    }

    void SelectOption()
    {
        actions[currentOptionIdx]?.Invoke();
        Hide();
    }

    public void ShowUsePopup(ItemData item, System.Action onUse, System.Action onEquipToSlot)
    {
        titleTxt.text = item.itemName;
        currentOptionIdx = 0;
        optionCount = 3;

        optionTexts[0].text = "사용";
        optionTexts[1].text = "슬롯 장착";
        optionTexts[2].text = "닫기";

        options[0].gameObject.SetActive(true);
        options[1].gameObject.SetActive(true);
        options[2].gameObject.SetActive(true);

        actions[0] = onUse;
        actions[1] = onEquipToSlot;
        actions[2] = null; // 닫기

        RefreshHighlight();
        popupPanel.SetActive(true);
    }

    // 장비용 팝업
    public void ShowEquipPopup(ItemData item, bool isEquipped, System.Action onEquip)
    {
        titleTxt.text = item.itemName;
        currentOptionIdx = 0;
        optionCount = 2;

        optionTexts[0].text = isEquipped ? "해제" : "장착";
        optionTexts[1].text = "닫기";
        optionTexts[2].text = "";

        options[0].gameObject.SetActive(true);
        options[1].gameObject.SetActive(true);
        options[2].gameObject.SetActive(false);

        actions[0] = onEquip;
        actions[1] = null; 
        actions[2] = null;

        RefreshHighlight();
        popupPanel.SetActive(true);
    }
    public void ShowHotbarPopup(ItemData item, System.Action onUse, System.Action onReturn)
    {
        titleTxt.text = item.itemName;
        currentOptionIdx = 0;
        optionCount = 3;

        optionTexts[0].text = "사용";
        optionTexts[1].text = "인벤토리로";
        optionTexts[2].text = "닫기";

        options[0].gameObject.SetActive(true);
        options[1].gameObject.SetActive(true);
        options[2].gameObject.SetActive(true);

        actions[0] = onUse;
        actions[1] = onReturn;
        actions[2] = null;

        RefreshHighlight();
        popupPanel.SetActive(true);
    }
    public void ShowHotbarSlotPopup(ItemData item, System.Action<int> onSlotSelect)
    {
        titleTxt.text = "슬롯 선택 (1~5)";
        currentOptionIdx = 0;
        optionCount = 0;

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