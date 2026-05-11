using UnityEngine;
using TMPro;
using System.Collections;

// 플레이어가 Interactable 오브젝트와 상호작용하는 대화 시스템.
// Q키로 대화 시작, Space키로 대사 진행/스킵, 선택지(Yes/No) UI 지원.
// 대화 중에는 Time.timeScale = 0f로 게임을 일시정지한다.
public class PlayerInteract : MonoBehaviour
{
    [Header("대화 UI")]
    public GameObject dialogueBox;       // 대화창 패널
    public TextMeshProUGUI dialogueText; // 대사 텍스트
    public GameObject clickHint;         // "Space를 눌러 계속" 힌트
    public GameObject hotbar;            // 대화 중 숨길 핫바

    [Header("선택지 UI")]
    public GameObject choiceBox;               // 선택지 패널
    public UnityEngine.UI.Button yesButton;    // 예 버튼
    public UnityEngine.UI.Button noButton;     // 아니오 버튼
    public TextMeshProUGUI choiceText;         // 선택지 질문 텍스트

    private Interactable currentTarget;    // 현재 범위 안에 있는 상호작용 대상
    private bool isDialogueActive = false; // 대화 진행 중 여부
    private bool isTyping = false;         // 타이핑 연출 진행 중 여부
    private string[] currentLines;        // 현재 표시 중인 대사 배열
    private int currentIndex = 0;         // 현재 대사 인덱스
    private System.Action onComplete;     // 대사 완료 후 실행할 콜백

    void Start()
    {
        dialogueBox.SetActive(false);
        choiceBox.SetActive(false);

        yesButton.onClick.AddListener(OnChoiceYes);
        noButton.onClick.AddListener(OnChoiceNo);
    }

    void Update()
    {
        // Q키: 범위 안 Interactable과 대화 시작
        if (!isDialogueActive && currentTarget != null && Input.GetKeyDown(KeyCode.Q))
        {
            int idx = currentTarget.currentPhaseIndex;

            // 마지막 phase를 초과하면 마지막 phase 재사용
            if (idx >= currentTarget.phases.Length)
                idx = currentTarget.phases.Length - 1;

            DialoguePhase phase = currentTarget.phases[idx];

            // 선행 조건 플래그 체크
            bool flagMissing = phase.requiredFlag != "" &&
                               (GameManager.Instance == null ||
                                !GameManager.Instance.HasFlag(phase.requiredFlag));

            if (flagMissing)
            {
                // 조건 미충족 시 힌트 메시지만 출력
                StartDialogue(new string[] { phase.hintMessage });
                return;
            }

            StartDialogue(phase.dialogueLines, () =>
            {
                if (phase.setFlag != "")
                    GameManager.Instance?.SetFlag(phase.setFlag);
                phase.onComplete?.Invoke();

                if (currentTarget.currentPhaseIndex < currentTarget.phases.Length - 1)
                {
                    currentTarget.currentPhaseIndex++;
                    if (currentTarget.interactableId != "")
                        GameManager.Instance?.SetPhaseIndex(currentTarget.interactableId, currentTarget.currentPhaseIndex);
                }
            });
        }

        // Space키: 대사 스킵 또는 다음 줄 진행
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                // 타이핑 중 Space: 현재 줄 즉시 완성
                StopAllCoroutines();
                dialogueText.text = currentLines[currentIndex];
                isTyping = false;
                clickHint.SetActive(true);
            }
            else
            {
                NextLine();
            }
        }
    }

    // 플레이어가 Interactable 범위에 진입
    void OnTriggerEnter2D(Collider2D other)
    {
        Interactable target = other.GetComponent<Interactable>();
        if (target != null)
            currentTarget = target;
    }

    // OnTriggerEnter2D가 누락된 경우를 보완 (공격 콜라이더 간섭 또는 범위 안에서 활성화)
    void OnTriggerStay2D(Collider2D other)
    {
        if (currentTarget != null) return;
        Interactable target = other.GetComponent<Interactable>();
        if (target != null)
            currentTarget = target;
    }

    // 플레이어가 Interactable 범위를 벗어남
    void OnTriggerExit2D(Collider2D other)
    {
        Interactable target = other.GetComponent<Interactable>();
        if (target != null && target == currentTarget)
            currentTarget = null;
    }

    // 대화 시작: 대화창 활성화, 게임 일시정지, 첫 줄 타이핑 시작
    public void StartDialogue(string[] lines, System.Action onDone = null)
    {
        if (lines == null || lines.Length == 0) return;

        currentLines = lines;
        currentIndex = 0;
        onComplete = onDone;
        isDialogueActive = true;
        dialogueBox.SetActive(true);
        Time.timeScale = 0f;
        if (hotbar != null) hotbar.SetActive(false);
        StartCoroutine(TypeLine(lines[0]));
    }

    // 한 글자씩 타이핑 연출 (unscaledTime 사용: timeScale=0에서도 동작)
    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        clickHint.SetActive(false);
        dialogueText.text = "";

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(0.07f);
        }

        isTyping = false;
        clickHint.SetActive(true);
    }

    // 다음 줄로 이동. 마지막이면 대화 종료 후 선택지 또는 콜백 처리
    void NextLine()
    {
        currentIndex++;
        if (currentIndex >= currentLines.Length)
        {
            dialogueBox.SetActive(false);
            isDialogueActive = false;
            if (hotbar != null) hotbar.SetActive(true);

            // currentTarget이 없으면 (힌트 메시지 등) 바로 콜백
            if (currentTarget == null)
            {
                Time.timeScale = 1f;
                onComplete?.Invoke();
                return;
            }

            int idx = Mathf.Min(currentTarget.currentPhaseIndex, currentTarget.phases.Length - 1);
            DialoguePhase phase = currentTarget.phases[idx];

            // 선택지 여부 확인
            if (phase != null && phase.hasChoice)
                ShowChoiceBox(phase.choiceQuestion);
            else
            {
                Time.timeScale = 1f;
                onComplete?.Invoke();
            }
            return;
        }
        StartCoroutine(TypeLine(currentLines[currentIndex]));
    }

    // 선택지 박스 표시 (이전 클릭 이벤트 잔류 방지를 위해 한 프레임 후 버튼 활성화)
    void ShowChoiceBox(string question)
    {
        choiceText.text = question;

        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
        yesButton.interactable = false;
        noButton.interactable = false;

        choiceBox.SetActive(true);
        StartCoroutine(EnableChoiceButtons());
    }

    // 한 프레임 대기 후 버튼 활성화
    IEnumerator EnableChoiceButtons()
    {
        yield return null;

        yesButton.interactable = true;
        noButton.interactable = true;
        yesButton.onClick.AddListener(OnChoiceYes);
        noButton.onClick.AddListener(OnChoiceNo);
    }

    // 예 선택: yesLines 있으면 추가 대화, 없으면 onChoiceYes 이벤트 실행
    void OnChoiceYes()
    {
        choiceBox.SetActive(false);
        if (currentTarget == null) return;

        int idx = Mathf.Min(currentTarget.currentPhaseIndex, currentTarget.phases.Length - 1);
        DialoguePhase phase = currentTarget.phases[idx];

        phase.hasChoice = false;
        onComplete?.Invoke();

        if (phase.yesLines != null && phase.yesLines.Length > 0)
            StartDialogue(phase.yesLines, () => { phase.onChoiceYes?.Invoke(); });
        else
        {
            Time.timeScale = 1f;
            phase.onChoiceYes?.Invoke();
        }
    }

    // 아니오 선택: noLines 있으면 추가 대화, 없으면 onChoiceNo 이벤트 실행
    void OnChoiceNo()
    {
        choiceBox.SetActive(false);
        if (currentTarget == null) return;

        int idx = Mathf.Min(currentTarget.currentPhaseIndex, currentTarget.phases.Length - 1);
        DialoguePhase phase = currentTarget.phases[idx];

        phase.hasChoice = false;
        onComplete?.Invoke();

        if (phase.noLines != null && phase.noLines.Length > 0)
            StartDialogue(phase.noLines, () => { phase.onChoiceNo?.Invoke(); });
        else
        {
            Time.timeScale = 1f;
            phase.onChoiceNo?.Invoke();
        }
    }
}
