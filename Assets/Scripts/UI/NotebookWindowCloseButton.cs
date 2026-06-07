using UnityEngine;
using UnityEngine.EventSystems;

public class NotebookWindowCloseButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject window;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (window != null)
        {
            window.SetActive(false);
        }
    }
}
