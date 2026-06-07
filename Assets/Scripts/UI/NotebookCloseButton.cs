using UnityEngine;
using UnityEngine.EventSystems;

public class NotebookCloseButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private NotebookUIController notebook;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (notebook != null)
        {
            notebook.CloseNotebook();
        }
    }
}
