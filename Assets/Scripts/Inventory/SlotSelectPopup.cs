/*
 * SlotSelectPopup.cs
 * 역할: 아이템을 핫바 몇 번 슬롯에 넣을지 선택하는 팝업 UI를 관리합니다.
 * 연결: InventoryManager가 소비 아이템을 핫바에 등록할 때 콜백을 넘겨 표시합니다.
 * 주의: 팝업이 열린 동안 인벤토리 선택 상태와 Time.timeScale 상태가 꼬이지 않도록 닫기/콜백 순서를 유지해야 합니다.
 */using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// "슬롯 장착" 선택 시 핫바 슬롯 번호를 고르는 팝업.
/// Show()에 아이템과 콜백을 넘기면 슬롯 버튼 클릭 시 콜백이 호출된다.
/// </summary>
public class SlotSelectPopup : MonoBehaviour
{
    // 소프트 참조
    public static SlotSelectPopup Instance { get; private set; }

    [Header("UI 연결")]
    public GameObject      panel;
    public Button[]        slotButtons;
    public Button          closeButton;
    public TextMeshProUGUI titleTxt;

    [Header("핫바 참조 (인스펙터 연결 또는 자동 탐색)")]
    [SerializeField] private HotbarManager hotbarManager;

    private System.Action<int> onSlotSelect;

    void Awake()
    {
        Instance = this;
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

        if (hotbarManager == null)
            hotbarManager = FindFirstObjectByType<HotbarManager>();
    }

    public void Show(ItemData item, System.Action<int> onSelect)
    {
        onSlotSelect = onSelect;
        titleTxt.text = "몇 번 슬롯에 넣으시겠습니까?";

        for (int i = 0; i < slotButtons.Length; i++)
        {
            var txt       = slotButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            bool occupied = hotbarManager != null && hotbarManager.GetItem(i) != null;
            txt.text      = $"{i + 1}번 슬롯" + (occupied ? " (사용중)" : "");
        }

        panel.SetActive(true);
    }

    void OnClickSlot(int index)
    {
        onSlotSelect?.Invoke(index);
        Hide();
    }

    public void Hide() => panel.SetActive(false);

    public bool IsOpen => panel != null && panel.activeSelf;
}



