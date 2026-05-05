using UnityEngine;
using UnityEngine.Events;

// 상호작용 가능한 오브젝트의 대사, 조건, 완료 이벤트를 정의하는 데이터 컴포넌트.
// PlayerInteract가 이 컴포넌트를 읽어 대화 시스템을 구동한다.
public class Interactable : MonoBehaviour
{
    [Header("대사")]
    public string[] dialogueLines;

    [Header("선행 조건")]
    public string requiredFlag = "";    // 이 플래그를 보유해야 상호작용 가능
    public string hintMessage = "...";  // 조건 미충족 시 표시할 메시지

    [Header("완료 후 동작")]
    public UnityEvent onComplete;       // 대화 완료 후 실행할 이벤트
    public string setFlag = "";         // 완료 시 등록할 플래그

    [Header("선택지")]
    public bool hasChoice = false;
    public string choiceQuestion = "";
    public string[] yesLines;         // 예 눌렀을 때 후속 대사
    public string[] noLines;          // 아니오 눌렀을 때 후속 대사
    public UnityEvent onChoiceYes;    // 예 눌렀을 때 이벤트
}
