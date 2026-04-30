using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TitleClick : MonoBehaviour, IPointerClickHandler
{
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        anim.Play("Title", 0, 0f);
    }
}