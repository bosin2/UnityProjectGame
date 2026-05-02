using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Events;

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



    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("Q 눌림 / currentTarget: " + currentTarget);
        }
       
        // 상호작용
        if (!isDialogueActive && currentTarget != null
       && Input.GetKeyDown(KeyCode.Q))
        {
            // 조건 안 맞으면 힌트 대사 띄우기
            if (currentTarget.requiredFlag != "" &&
                !GameManager.instance.HasFlag(currentTarget.requiredFlag))
            {
                StartDialogue(new string[] { currentTarget.hintMessage });
                return;
            }

            StartDialogue(currentTarget.dialogueLines, () =>
            {
                if (currentTarget.setFlag != "")
                    GameManager.instance.SetFlag(currentTarget.setFlag);
                currentTarget.onComplete?.Invoke();
            });
        }

        // 대사 넘기기
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
        Debug.Log("트리거 감지: " + other.gameObject.name);
        Interactable target = other.GetComponent<Interactable>();
        if (target != null)
        {
            Debug.Log("Interactable 찾음!");
            currentTarget = target;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Interactable target = other.GetComponent<Interactable>();
        if (target != null && target == currentTarget)
            currentTarget = null;
    }

    public void StartDialogue(string[] lines, System.Action onDone = null)
    {
        currentLines = lines;
        currentIndex = 0;
        onComplete = onDone;
        isDialogueActive = true;
        dialogueBox.SetActive(true);
        Time.timeScale = 0f;
        if (hotbar != null) hotbar.SetActive(false); // 핫바 끄기
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
            Time.timeScale = 1f;
            if (hotbar != null) hotbar.SetActive(true); // 핫바 켜기
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(TypeLine(currentLines[currentIndex]));
    }

    void Start()
    {
        dialogueBox.SetActive(false);
    }

}