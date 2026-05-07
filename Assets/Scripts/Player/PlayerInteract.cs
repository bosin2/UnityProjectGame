using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerInteract : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject dialogueBox;
    public TextMeshProUGUI dialogueText;
    public GameObject clickHint;
    public GameObject hotbar;

    [Header("선택지 UI")]
    public GameObject choiceBox;
    public UnityEngine.UI.Button yesButton;
    public UnityEngine.UI.Button noButton;
    public TextMeshProUGUI choiceText;

    private Interactable currentTarget;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private string[] currentLines;
    private int currentIndex = 0;
    private System.Action onComplete;

    void Start()
    {
        dialogueBox.SetActive(false);
        choiceBox.SetActive(false);

        yesButton.onClick.AddListener(OnChoiceYes);
        noButton.onClick.AddListener(OnChoiceNo);
    }

    void Update()
    {
        if (!isDialogueActive && currentTarget != null && Input.GetKeyDown(KeyCode.Q))
        {
            // 현재 phase 가져오기
            int idx = currentTarget.currentPhaseIndex;

            // phase가 더 없으면 마지막 phase 재사용
            if (idx >= currentTarget.phases.Length)
                idx = currentTarget.phases.Length - 1;

            DialoguePhase phase = currentTarget.phases[idx];

            // 선행 조건 체크
            bool flagMissing = phase.requiredFlag != "" &&
                               (GameManager.Instance == null ||
                                !GameManager.Instance.HasFlag(phase.requiredFlag));

            if (flagMissing)
            {
                StartDialogue(new string[] { phase.hintMessage });
                return;
            }

            StartDialogue(phase.dialogueLines, () =>
            {
                if (phase.setFlag != "")
                    GameManager.Instance?.SetFlag(phase.setFlag);
                phase.onComplete?.Invoke();

                // 다음 phase로 진행 (마지막이면 유지)
                if (currentTarget.currentPhaseIndex < currentTarget.phases.Length - 1)
                    currentTarget.currentPhaseIndex++;
            });
        }

        if (isDialogueActive && Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
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

    void OnTriggerEnter2D(Collider2D other)
    {
        Interactable target = other.GetComponent<Interactable>();
        if (target != null)
            currentTarget = target;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Interactable target = other.GetComponent<Interactable>();
        if (target != null && target == currentTarget)
            currentTarget = null;
    }

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

    void NextLine()
    {
        currentIndex++;
        if (currentIndex >= currentLines.Length)
        {
            dialogueBox.SetActive(false);
            isDialogueActive = false;
            if (hotbar != null) hotbar.SetActive(true);

            // 현재 phase에서 선택지 여부 확인
            int idx = Mathf.Min(currentTarget.currentPhaseIndex, currentTarget.phases.Length - 1);
            DialoguePhase phase = currentTarget != null ? currentTarget.phases[idx] : null;

            if (phase != null && phase.hasChoice)
            {
                ShowChoiceBox(phase.choiceQuestion);
            }
            else
            {
                Time.timeScale = 1f;
                onComplete?.Invoke();
            }
            return;
        }
        StartCoroutine(TypeLine(currentLines[currentIndex]));
    }
    void ShowChoiceBox(string question)
    {
        choiceText.text = question;

        // 버튼 상태 강제 리셋
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();

        yesButton.interactable = false;
        noButton.interactable = false;

        choiceBox.SetActive(true);

        // 한 프레임 뒤에 활성화 (이전 클릭 이벤트 잔류 방지)
        StartCoroutine(EnableChoiceButtons());
    }

    IEnumerator EnableChoiceButtons()
    {
        yield return null; // 한 프레임 대기

        yesButton.interactable = true;
        noButton.interactable = true;

        yesButton.onClick.AddListener(OnChoiceYes);
        noButton.onClick.AddListener(OnChoiceNo);
    }
    void OnChoiceYes()
    {
        choiceBox.SetActive(false);
        if (currentTarget == null) return;

        int idx = Mathf.Min(currentTarget.currentPhaseIndex, currentTarget.phases.Length - 1);
        DialoguePhase phase = currentTarget.phases[idx];

        phase.hasChoice = false; // ← 추가!
        onComplete?.Invoke();

        if (phase.yesLines != null && phase.yesLines.Length > 0)
        {
            StartDialogue(phase.yesLines, () => { phase.onChoiceYes?.Invoke(); });
        }
        else
        {
            Time.timeScale = 1f;
            phase.onChoiceYes?.Invoke();
        }
    }

    void OnChoiceNo()
    {
        choiceBox.SetActive(false);
        if (currentTarget == null) return;

        int idx = Mathf.Min(currentTarget.currentPhaseIndex, currentTarget.phases.Length - 1);
        DialoguePhase phase = currentTarget.phases[idx];

        phase.hasChoice = false; // ← 추가!
        onComplete?.Invoke();

        if (phase.noLines != null && phase.noLines.Length > 0)
        {
            StartDialogue(phase.noLines, () => { phase.onChoiceNo?.Invoke(); });
        }
        else
        {
            Time.timeScale = 1f;
            phase.onChoiceNo?.Invoke();
        }
    }
}