using UnityEngine;
using UnityEngine.EventSystems;

// 타이틀 UI를 클릭하면 타이틀 애니메이션을 처음부터 재생하는 컴포넌트
public class TitleClick : MonoBehaviour, IPointerClickHandler
{
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // 클릭 시 "Title" 애니메이션을 0초(처음)부터 재재생
    public void OnPointerClick(PointerEventData eventData)
    {
        anim.Play("Title", 0, 0f);
    }
}
