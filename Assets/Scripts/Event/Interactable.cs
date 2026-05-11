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
    [Tooltip("씬 간 phase 진행 상태를 유지하기 위한 고유 ID. 씬 내에서 중복 없이 설정할 것.")]
    public string interactableId = "";

    public DialoguePhase[] phases;

    [HideInInspector]
    public int currentPhaseIndex = 0;  // PlayerInteract가 관리

    void Start()
    {
        // GameManager에 저장된 phase 인덱스를 복원 (씬 재로드 후에도 진행 상태 유지)
        if (interactableId != "" && GameManager.Instance != null)
            currentPhaseIndex = GameManager.Instance.GetPhaseIndex(interactableId);
    }
}