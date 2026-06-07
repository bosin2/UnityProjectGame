using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotebookQuizController : MonoBehaviour
{
    private struct QuizPerson
    {
        public readonly string Name;
        public readonly int CorrectChoice;
        public readonly System.Func<GameManager, bool> WasKilled;

        public QuizPerson(string name, int correctChoice, System.Func<GameManager, bool> wasKilled)
        {
            Name = name;
            CorrectChoice = correctChoice;
            WasKilled = wasKilled;
        }
    }

    [Header("Reward")]
    [SerializeField] private ItemGiver itemGiver;
    [SerializeField] private string rewardFlag = "RooftopKeyGet";
    [SerializeField] private string failedFlag = "G418QuizFailed";

    [Header("Typing")]
    [SerializeField] private float characterDelay = 0.035f;

    [Header("Font")]
    [SerializeField] private TMP_FontAsset fontAsset;

    private readonly QuizPerson[] people =
    {
        new QuizPerson("정범석", 1, gm => gm.JKilled),
        new QuizPerson("박윤하", 2, gm => gm.PKilled),
        new QuizPerson("김우진", 3, gm => gm.KKilled),
    };

    private GameObject root;
    private TextMeshProUGUI dialogueText;
    private TextMeshProUGUI inputText;

    private int personIndex;
    private string currentInput = "";
    private bool isOpen;
    private bool isTyping;
    private bool waitingForAdvance;
    private bool waitingForAnswer;
    private bool pendingAdvanceAfterTyping;
    private bool pendingAnswerAfterTyping;
    private bool pendingRetryIntro;
    private bool finished;
    private string fullTypingMessage = "";
    private Coroutine typingRoutine;

    public void OpenQuiz()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.HasFlag(rewardFlag))
        {
            return;
        }

        bool isRetry = gm != null && gm.HasFlag(failedFlag);

        EnsureUI();

        personIndex = 0;
        currentInput = "";
        waitingForAdvance = false;
        waitingForAnswer = false;
        pendingRetryIntro = false;
        finished = false;
        isOpen = true;

        root.SetActive(true);
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (isRetry)
        {
            pendingRetryIntro = true;
            TypeMessage("(알 수 없음)\n다시 왔네? 죽은 친구 생일이라도 기억났어?", true);
            return;
        }

        ShowCurrentPerson();
    }

    public void CloseQuiz()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        isOpen = false;
        isTyping = false;
        waitingForAdvance = false;
        waitingForAnswer = false;
        pendingRetryIntro = false;
        currentInput = "";

        if (root != null)
        {
            root.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    private void Awake()
    {
        if (itemGiver == null)
        {
            itemGiver = GetComponent<ItemGiver>();
        }
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseQuiz();
            return;
        }

        if (isTyping)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                CompleteTyping();
            }

            return;
        }

        if (waitingForAnswer)
        {
            ReadAnswerInput();
            return;
        }

        if (waitingForAdvance && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            waitingForAdvance = false;

            if (finished)
            {
                return;
            }

            if (pendingRetryIntro)
            {
                pendingRetryIntro = false;
                ShowCurrentPerson();
                return;
            }

            personIndex++;
            ShowCurrentPerson();
        }
    }

    private void ShowCurrentPerson()
    {
        currentInput = "";
        SetInputLine("");

        if (personIndex >= people.Length)
        {
            finished = true;
            GrantReward();
            TypeMessage("너는 살아남을 자격이 있는 아이구나. 컴퓨터 본체 아래를 보면 옥상 열쇠가 있을거다. 기회가 된다면 다음에 또 만나지.\n(옥상 열쇠를 얻었습니다.)");
            return;
        }

        GameManager gm = GameManager.Instance;
        QuizPerson person = people[personIndex];
        bool killed = gm != null && person.WasKilled(gm);

        if (!killed)
        {
            TypeMessage($"좋아, {person.Name}{GetTopicParticle(person.Name)} 안죽였네? 끝내주는게 좋지 않아?", true);
            return;
        }

        TypeMessage($"(알 수 없음)\n{person.Name}{GetTopicParticle(person.Name)} 왜 죽였어? 생일은 알아?\n\n1. 0302\n2. 0521\n3. 0828\n\n번호를 입력해.", false, true);
    }

    private void ReadAnswerInput()
    {
        int choice = 0;

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) choice = 1;
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) choice = 2;
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) choice = 3;

        if (choice == 0)
        {
            return;
        }

        currentInput = choice.ToString();
        SetInputLine($"> {currentInput}");
        waitingForAnswer = false;

        QuizPerson person = people[personIndex];
        if (choice == person.CorrectChoice)
        {
            TypeMessage("그래도 사람의 도리는 지키는군.", true);
            return;
        }

        GameManager.Instance?.SetFlag(failedFlag);
        finished = true;
        TypeMessage("너는 네가 죽인 사람들의 생일도 모르는거야? 사람이라고도 생각안했군. 어디 혼자 잘 탈출해봐.\n\n(대화가 종료되었습니다)");
    }

    private void GrantReward()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && !string.IsNullOrEmpty(rewardFlag))
        {
            if (gm.HasFlag(rewardFlag))
            {
                return;
            }

            gm.SetFlag(rewardFlag);
        }

        itemGiver?.GiveItem();
    }

    private void TypeMessage(string message, bool advanceAfter = false, bool answerAfter = false)
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        typingRoutine = StartCoroutine(TypeRoutine(message, advanceAfter, answerAfter));
    }

    private IEnumerator TypeRoutine(string message, bool advanceAfter, bool answerAfter)
    {
        isTyping = true;
        waitingForAdvance = false;
        waitingForAnswer = false;
        pendingAdvanceAfterTyping = advanceAfter;
        pendingAnswerAfterTyping = answerAfter;
        fullTypingMessage = message;
        dialogueText.text = "";

        foreach (char c in message)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(characterDelay);
        }

        FinishTyping(advanceAfter, answerAfter);
    }

    private void CompleteTyping()
    {
        if (typingRoutine == null)
        {
            return;
        }

        StopCoroutine(typingRoutine);
        dialogueText.text = fullTypingMessage;
        FinishTyping(pendingAdvanceAfterTyping, pendingAnswerAfterTyping);
    }

    private void FinishTyping(bool advanceAfter, bool answerAfter)
    {
        typingRoutine = null;
        isTyping = false;
        waitingForAdvance = advanceAfter;
        waitingForAnswer = answerAfter;
    }

    private void SetInputLine(string value)
    {
        if (inputText != null)
        {
            inputText.text = value;
        }
    }

    private void EnsureUI()
    {
        if (root != null)
        {
            return;
        }

        root = new GameObject("NotebookQuizCanvas");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        Image background = root.AddComponent<Image>();
        background.color = Color.black;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        dialogueText = CreateText("DialogueText", root.transform, new Vector2(120f, 150f), new Vector2(-120f, -180f), 38);
        dialogueText.alignment = TextAlignmentOptions.TopLeft;

        inputText = CreateText("InputText", root.transform, new Vector2(120f, 90f), new Vector2(-120f, -930f), 34);
        inputText.alignment = TextAlignmentOptions.Left;

        Button closeButton = CreateCloseButton(root.transform);
        closeButton.onClick.AddListener(CloseQuiz);

        root.SetActive(false);
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, Vector2 offsetMin, Vector2 offsetMax, float size)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null)
        {
            text.font = fontAsset;
        }

        text.fontSize = size;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateCloseButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("CloseButton");
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-40f, -35f);
        rect.sizeDelta = new Vector2(120f, 46f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        Button button = buttonObject.AddComponent<Button>();

        TextMeshProUGUI label = CreateText("Label", buttonObject.transform, Vector2.zero, Vector2.zero, 26);
        label.text = "Close";
        label.alignment = TextAlignmentOptions.Center;

        return button;
    }

    private string GetTopicParticle(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "는";
        }

        char last = name[name.Length - 1];
        bool hasBatchim = (last - 0xAC00) % 28 != 0;
        return hasBatchim ? "은" : "는";
    }
}
