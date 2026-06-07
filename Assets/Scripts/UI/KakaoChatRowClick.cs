using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KakaoChatRowClick : MonoBehaviour, IPointerClickHandler
{
    private const float DoubleClickInterval = 0.35f;

    [SerializeField] private GameObject selectedHighlight;
    [SerializeField] private GameObject errorPopup;
    [SerializeField] private GameObject errorDim;
    [SerializeField] private Image rowBackground;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color32(238, 238, 238, 255);

    private float lastClickTime = -10f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.unscaledTime - lastClickTime <= DoubleClickInterval)
        {
            ShowErrorPopup();
            lastClickTime = -10f;
            return;
        }

        lastClickTime = Time.unscaledTime;
        SelectRow();
    }

    public void SelectRow()
    {
        KakaoChatRowClick[] rows = transform.root.GetComponentsInChildren<KakaoChatRowClick>(true);

        foreach (KakaoChatRowClick row in rows)
        {
            row.SetSelected(row == this);
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
        {
            selectedHighlight.SetActive(selected);
        }

        if (rowBackground != null)
        {
            rowBackground.color = selected ? selectedColor : normalColor;
        }
    }

    private void ShowErrorPopup()
    {
        if (errorDim != null)
        {
            errorDim.SetActive(true);
        }

        if (errorPopup != null)
        {
            errorPopup.SetActive(true);
            errorPopup.transform.SetAsLastSibling();
        }
    }
}
