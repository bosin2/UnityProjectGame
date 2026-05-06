using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
}