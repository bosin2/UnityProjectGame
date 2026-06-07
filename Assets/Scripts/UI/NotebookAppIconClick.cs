using UnityEngine;
using UnityEngine.EventSystems;

public class NotebookAppIconClick : MonoBehaviour, IPointerClickHandler
{
    private const float DoubleClickInterval = 0.35f;

    [SerializeField] private NotebookUIController notebook;
    [SerializeField] private GameObject selectedHighlight;
    [SerializeField] private NotebookApp app;

    private float lastClickTime = -10f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.unscaledTime - lastClickTime <= DoubleClickInterval)
        {
            OpenApp();
            lastClickTime = -10f;
            return;
        }

        lastClickTime = Time.unscaledTime;
        SelectIcon();
    }

    private void SelectIcon()
    {
        NotebookAppIconClick[] icons = transform.root.GetComponentsInChildren<NotebookAppIconClick>(true);

        foreach (NotebookAppIconClick icon in icons)
        {
            icon.SetSelected(icon == this);
        }
    }

    private void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
        {
            selectedHighlight.SetActive(selected);
        }
    }

    private void OpenApp()
    {
        if (notebook == null)
        {
            return;
        }

        if (app == NotebookApp.KakaoTalk)
        {
            notebook.OpenKakaoTalk();
        }
        else if (app == NotebookApp.Memo)
        {
            notebook.OpenMemo();
        }
    }
}

public enum NotebookApp
{
    KakaoTalk,
    Memo
}
