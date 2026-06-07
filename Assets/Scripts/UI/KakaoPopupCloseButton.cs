using UnityEngine;
using UnityEngine.EventSystems;

public class KakaoPopupCloseButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject popup;
    [SerializeField] private GameObject dim;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (popup != null)
        {
            popup.SetActive(false);
        }

        if (dim != null)
        {
            dim.SetActive(false);
        }
    }
}
