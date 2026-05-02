using UnityEngine;
using TMPro;
using System.Collections;

// 플레이어가 Interactable 오브젝트에 접근했을 때 대화 시스템을 처리하는 컴포넌트
public class PlayerInteract : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject dialogueBox;
    public TextMeshProUGUI dialogueText;
    public GameObject clickHint;
    public GameObject hotbar;

    private Interactable currentTarget;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private string[] currentLines;
    private int currentIndex = 0;
    private System.Action onComplete;

    void Start()
    {
        dialogueBox.SetActive(false);
    }

    void Update()
    {
        // Q키: 상호작용 대상이 있고 대화 중이 아닐 때 대화 시작
        if (!isDialogueActive && currentTarget != null && Input.GetKeyDown(KeyCode.Q))
        {
            // 선행 플래그 조건 미충족 시 힌트 메시지 출력
            // GameManager가 없거나 플래그 미보유 시 힌트 표시
            bool flagMissing = currentTarget.requiredFlag != "" &&
                               (GameManager.Instance == null ||
                                !GameManager.Instance.HasFlag(currentTarget.requiredFlag));

            if (flagMissing)
            {
                StartDialogue(new string[] { currentTarget.hintMessage });
                return;
            }

            StartDialogue(currentTarget.dialogueLines, () =>
            {
                if (currentTarget.setFlag != "")
                    GameManager.Instance?.SetFlag(currentTarget.setFlag);
                currentTarget.onComplete?.Invoke();
            });
        }

        // Space키: 대화 중 타이핑 즉시 완성 또는 다음 줄로 넘기기
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

    // 플레이어가 Interactable 콜라이더에 진입하면 타겟으로 등록
    void OnTriggerEnter2D(Collider2D other)
    {
        Interactable target = other.GetComponent<Interactable>();
        if (target != null)
            currentTarget = target;
    }

    // 플레이어가 Interactable 콜라이더에서 벗어나면 타겟 해제
    void OnTriggerExit2D(Collider2D other)
    {
        Interactable target = other.GetComponent<Interactable>();
        if (target != null && target == currentTarget)
            currentTarget = null;
    }

    // 대화 시작: 대화창 표시, 시간 정지, 핫바 숨김
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

    // 한 글자씩 타이핑하는 연출 코루틴
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

    // 다음 줄로 넘기기. 마지막 줄이면 대화 종료
    void NextLine()
    {
        currentIndex++;
        if (currentIndex >= currentLines.Length)
        {
            dialogueBox.SetActive(false);
            isDialogueActive = false;
            Time.timeScale = 1f;
            if (hotbar != null) hotbar.SetActive(true);
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(TypeLine(currentLines[currentIndex]));
    }
}
