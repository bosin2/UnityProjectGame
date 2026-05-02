using UnityEngine;

// 파이프 아이템 획득 이벤트를 처리하는 컴포넌트. Interactable의 onComplete에서 호출된다.
public class PipePickup : MonoBehaviour
{
    // GameManager에 파이프 소지 상태를 등록
    public void GivePipe()
    {
        GameManager.Instance.hasPipe = true;
    }
}