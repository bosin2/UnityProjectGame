using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class DialoguePhase
{
    [Header("대사")]
    public string[] dialogueLines;

    [Header("선행 조건")]
    public string requiredFlag = "";
    public string hintMessage = "...";

    [Header("완료 후 동작")]
    public UnityEvent onComplete;
    public string setFlag = "";

    [Header("선택지")]
    public bool hasChoice = false;
    public string choiceQuestion = "";
    public string[] yesLines;
    public string[] noLines;
    public UnityEvent onChoiceYes;
    public UnityEvent onChoiceNo;
}

// 상호작용 가능한 오브젝트의 대사, 조건, 완료 이벤트를 정의하는 데이터 컴포넌트.
// PlayerInteract가 이 컴포넌트를 읽어 대화 시스템을 구동한다.
public class Interactable : MonoBehaviour
{
    public DialoguePhase[] phases;

    [HideInInspector]
    public int currentPhaseIndex = 0;  // PlayerInteract가 관리
}