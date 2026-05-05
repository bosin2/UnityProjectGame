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
        yesButton.onClick.AddListener(() => Debug.Log("YES 눌림!"));
        noButton.onClick.AddListener(() => Debug.Log("NO 눌림!"));
        yesButton.onClick.AddListener(OnChoiceYes);
        noButton.onClick.AddListener(OnChoiceNo);
        choiceBox.SetActive(false);
    }

    void Update()
    {
        if (!isDialogueActive && currentTarget != null && Input.GetKeyDown(KeyCode.Q))
        {
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

            if (currentTarget != null && currentTarget.hasChoice)
            {
                choiceText.text = currentTarget.choiceQuestion;
                choiceBox.SetActive(true);
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

    void OnChoiceYes()
    {
        choiceBox.SetActive(false);
        if (currentTarget == null) return;
        currentTarget.hasChoice = false;

        Interactable target = currentTarget;
        if (target.yesLines != null && target.yesLines.Length > 0)
        {
            StartDialogue(target.yesLines, () =>
            {
                target?.onChoiceYes?.Invoke();
            });
        }
        else
        {
            Time.timeScale = 1f;
            target?.onChoiceYes?.Invoke();
        }
    }

    void OnChoiceNo()
    {
        choiceBox.SetActive(false);
        if (currentTarget == null) return;
        currentTarget.hasChoice = false;

        if (currentTarget.noLines != null && currentTarget.noLines.Length > 0)
        {
            StartDialogue(currentTarget.noLines);
        }
        else
        {
            Time.timeScale = 1f;
        }
    }
}