using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 플레이어가 Interactable 오브젝트와 대화하는 시스템.
/// Q키로 대화 시작, Space키로 진행/스킵, Yes/No 선택지 지원.
/// 대화 중에는 Time.timeScale = 0 으로 게임 일시정지.
/// </summary>
public class PlayerInteract : MonoBehaviour
{
    [Header("대화 UI")]
    public GameObject       dialogueBox;
    public TextMeshProUGUI  dialogueText;
    public GameObject       clickHint;     // "Space를 눌러 계속" 힌트
    public GameObject       hotbar;        // 대화 중 숨길 핫바

    [Header("선택지 UI")]
    public GameObject              choiceBox;
    public UnityEngine.UI.Button   yesButton;
    public UnityEngine.UI.Button   noButton;
    public TextMeshProUGUI         choiceText;

    // ── 내부 상태 ──────────────────────────────────────────────────────
    private Interactable currentTarget;
    private bool   isDialogueActive = false;
    private bool   isTyping         = false;
    private string[] currentLines;
    private int    currentIndex     = 0;
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
        var ph = GetComponent<PlayerHealth>();
        if (ph != null && ph.IsDead) return;

        // Q키: 범위 안 Interactable과 대화 시작
        if (!isDialogueActive && currentTarget != null && Input.GetKeyDown(KeyCode.Q))
        {
            int idx = currentTarget.currentPhaseIndex;
            if (idx >= currentTarget.phases.Length)
                idx = currentTarget.phases.Length - 1;

            DialoguePhase phase = currentTarget.phases[idx];

            // 선행 조건 플래그 체크
            bool flagMissing = phase.requiredFlag != ""
                && (GameManager.Instance == null || !GameManager.Instance.HasFlag(phase.requiredFlag));

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
                if (currentTarget != null
                    && currentTarget.currentPhaseIndex < currentTarget.phases.Length - 1)
                {
                    currentTarget.currentPhaseIndex++;
                    if (currentTarget.interactableId != "")
                        GameManager.Instance?.SetPhaseIndex(
                            currentTarget.interactableId,
                            currentTarget.currentPhaseIndex);
                }
            });
        }

        // Space키: 스킵 또는 다음 줄
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

    // ── 충돌 감지 ─────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        Interactable target = other.GetComponent<Interactable>();
        if (target != null) currentTarget = target;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Interactable target = other.GetComponent<Interactable>();
        if (target != null && target == currentTarget)
            currentTarget = null;
    }

    // ── 대화 시스템 ───────────────────────────────────────────────────

    /// <summary>대화 시작: 대화창 활성화, 게임 일시정지, 첫 줄 타이핑</summary>
    public void StartDialogue(string[] lines, System.Action onDone = null)
    {
        if (lines == null || lines.Length == 0) return;

        currentLines   = lines;
        currentIndex   = 0;
        onComplete     = onDone;
        isDialogueActive = true;
        dialogueBox.SetActive(true);
        Time.timeScale = 0f;
        if (hotbar != null) hotbar.SetActive(false);
        StartCoroutine(TypeLine(lines[0]));
    }

    // 한 글자씩 타이핑 (unscaledTime: timeScale=0에서도 동작)
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

            if (currentTarget == null)
            {
                Time.timeScale = 1f;
                onComplete?.Invoke();
                return;
            }

            int idx = Mathf.Min(currentTarget.currentPhaseIndex, currentTarget.phases.Length - 1);
            DialoguePhase phase = currentTarget.phases[idx];

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

    void ShowChoiceBox(string question)
    {
        choiceText.text = question;
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
        yesButton.interactable = false;
        noButton.interactable  = false;
        choiceBox.SetActive(true);
        StartCoroutine(EnableChoiceButtons());
    }

    IEnumerator EnableChoiceButtons()
    {
        yield return null;
        yesButton.interactable = true;
        noButton.interactable  = true;
        yesButton.onClick.AddListener(OnChoiceYes);
        noButton.onClick.AddListener(OnChoiceNo);
    }

    void OnChoiceYes()
    {
        choiceBox.SetActive(false);
        if (currentTarget == null) return;

        int idx = Mathf.Min(currentTarget.currentPhaseIndex, currentTarget.phases.Length - 1);
        DialoguePhase phase = currentTarget.phases[idx];
        phase.hasChoice = false;
        onComplete?.Invoke();

        if (phase.yesLines != null && phase.yesLines.Length > 0)
            StartDialogue(phase.yesLines, () => phase.onChoiceYes?.Invoke());
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
        phase.hasChoice = false;
        onComplete?.Invoke();

        if (phase.noLines != null && phase.noLines.Length > 0)
            StartDialogue(phase.noLines, () => phase.onChoiceNo?.Invoke());
        else
        {
            Time.timeScale = 1f;
            phase.onChoiceNo?.Invoke();
        }
    }
}
