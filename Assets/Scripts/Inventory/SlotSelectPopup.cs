using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 인벤토리에서 "슬롯 장착" 선택 시 핫바 슬롯 번호를 고르는 팝업 싱글톤.
// Show()에 아이템과 콜백을 넘기면 슬롯 버튼 클릭 시 해당 인덱스로 콜백이 호출된다.
public class SlotSelectPopup : MonoBehaviour
{
    public static SlotSelectPopup Instance;

    [Header("UI 연결")]
    public GameObject panel;
    public Button[] slotButtons;
    public Button closeButton;
    public TextMeshProUGUI titleTxt;

    private System.Action<int> onSlotSelect;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        panel.SetActive(false);

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int idx = i;
            slotButtons[i].onClick.AddListener(() => OnClickSlot(idx));
        }
        closeButton.onClick.AddListener(Hide);
    }

    public void Show(ItemData item, System.Action<int> onSelect)
    {
        onSlotSelect = onSelect;
        titleTxt.text = "몇 번 슬롯에 넣으시겠습니까?";

        // 슬롯 사용중 표시냥
        for (int i = 0; i < slotButtons.Length; i++)
        {
            TextMeshProUGUI txt = slotButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            bool occupied = HotbarManager.Instance.GetItem(i) != null;
            txt.text = $"{i + 1}번 슬롯" + (occupied ? " (사용중)" : "");
        }

        panel.SetActive(true);
    }

    void OnClickSlot(int index)
    {
        onSlotSelect?.Invoke(index);
        Hide();
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    public bool IsOpen => panel != null && panel.activeSelf;
}