/*
 * TitleClick.cs
 * 역할: 타이틀 화면에서 클릭/입력으로 다음 UI 또는 씬 흐름을 시작하는 간단한 진입 스크립트입니다.
 * 연결: MainMenu 씬 UI 오브젝트에 붙어 메인 메뉴 컨트롤러 또는 씬 로드 흐름과 함께 사용됩니다.
 * 주의: 타이틀 입력은 게임플레이 입력과 다르게 Time.timeScale이나 플레이어 상태에 의존하지 않아야 합니다.
 */using UnityEngine;
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

