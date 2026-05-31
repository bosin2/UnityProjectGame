using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class G421IntroManager : MonoBehaviour
{
    [Header("인트로 UI")]
    public GameObject introRoot;     // 인트로 전체를 감싸는 루트 오브젝트
    public TextMeshProUGUI dialogueText;
    public GameObject clickHint;     // "Space를 눌러 계속" 힌트
    public Image fadePanel;     // 화면 페이드용 검정 패널
    public Image cutscene;      // 컷씬 이미지 (없으면 null)

    [System.Serializable]
    public class DialogueLine
    {
        public string text;
        public Sprite image;
        public bool clearImage;
    }
    public DialogueLine[] lines;

    [Header("개발용 스킵")]
    [SerializeField] private bool skipIntro = false;

    [Header("플레이 UI")]
    [SerializeField] private WeaponSlotUI weaponSlotUI;
    [SerializeField] private HotbarManager hotbarManager;

    private int currentLine = 0;
    private bool isTyping = false;
    private bool canClick = false;
    private bool introActive = true;
    private float lastSpaceTime = -1f;
    private const float spaceCooldown = 0.3f;

    void Start()
    {
        if (weaponSlotUI == null) weaponSlotUI = FindFirstObjectByType<WeaponSlotUI>();
        if (hotbarManager == null) hotbarManager = FindFirstObjectByType<HotbarManager>();

        // 이미 본 적 있으면 스킵
        if (GameManager.Instance != null && GameManager.Instance.HasFlag("g421IntroDone"))
        {
            introRoot?.SetActive(false);
            return;
        }

        // 에디터 스킵
        if (skipIntro)
        {
            introRoot?.SetActive(false);
            GameManager.Instance?.SetFlag("g421IntroDone");
            return;
        }

        // 인트로 시작
        introRoot?.SetActive(true);
        SetPlayerControl(false);
        UICanvas.Instance?.HideUI();
        weaponSlotUI?.Hide();
        hotbarManager?.Hide();

        if (cutscene != null) cutscene.color = new Color(1, 1, 1, 0);
        if (clickHint != null) clickHint.SetActive(false);

        StartCoroutine(FadeIn());
    }

    void Update()
    {
        if (!introActive) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.unscaledTime - lastSpaceTime < spaceCooldown) return;
            lastSpaceTime = Time.unscaledTime;

            if (isTyping)
            {
                StopAllCoroutines();
                ApplyLineImage(lines[currentLine]);
                dialogueText.text = lines[currentLine].text;
                isTyping = false;
                canClick = true;
                clickHint?.SetActive(true);
            }
            else if (canClick)
            {
                NextLine();
            }
        }
    }

    void SetPlayerControl(bool enabled)
    {
        GameObject player = GameObject.FindWithTag("Player");
        var movement = player?.GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = enabled;
    }

    IEnumerator FadeIn()
    {
        float t = 1f;
        while (t > 0) { t -= Time.deltaTime; fadePanel.color = new Color(0, 0, 0, t); yield return null; }
        fadePanel.color = new Color(0, 0, 0, 0);
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(TypeLine(lines[currentLine]));
    }

    IEnumerator TypeLine(DialogueLine line)
    {
        isTyping = true;
        canClick = false;
        clickHint?.SetActive(false);
        dialogueText.text = "";

        if (line.clearImage && cutscene != null)
            yield return StartCoroutine(FadeImage(cutscene, 0f));
        else if (line.image != null && cutscene != null)
        {
            yield return StartCoroutine(FadeImage(cutscene, 0f));
            cutscene.sprite = line.image;
            yield return StartCoroutine(FadeImage(cutscene, 1f));
        }

        foreach (char c in line.text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.07f);
        }

        isTyping = false;
        canClick = true;
        clickHint?.SetActive(true);
    }

    IEnumerator FadeImage(Image img, float targetAlpha)
    {
        if (img == null) yield break;

        float start = img.color.a;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            img.color = new Color(1, 1, 1, Mathf.Lerp(start, targetAlpha, t));
            yield return null;
        }
        img.color = new Color(1, 1, 1, targetAlpha);
    }

    void ApplyLineImage(DialogueLine line)
    {
        if (cutscene == null) return;

        if (line.clearImage)
        {
            cutscene.color = new Color(1, 1, 1, 0);
            return;
        }

        if (line.image == null) return;

        cutscene.sprite = line.image;
        cutscene.color = Color.white;
    }

    void NextLine()
    {
        currentLine++;
        if (currentLine >= lines.Length)
        {
            StartCoroutine(FinishIntro());
            return;
        }
        StartCoroutine(TypeLine(lines[currentLine]));
    }

    IEnumerator FinishIntro()
    {
        canClick = false;
        introActive = false;
        clickHint?.SetActive(false);

        // 페이드 아웃
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime; fadePanel.color = new Color(0, 0, 0, t); yield return null; }

        introRoot?.SetActive(false);

        // 페이드 인
        t = 1f;
        while (t > 0f) { t -= Time.deltaTime; fadePanel.color = new Color(0, 0, 0, t); yield return null; }
        fadePanel.color = new Color(0, 0, 0, 0);

        GameManager.Instance?.SetFlag("g421IntroDone");
        UICanvas.Instance?.ShowUI();
        weaponSlotUI?.Show();
        hotbarManager?.Show();
        SetPlayerControl(true);
    }
}