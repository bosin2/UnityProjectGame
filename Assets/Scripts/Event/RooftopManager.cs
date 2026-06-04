using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 옥상 씬 컷씬 전체 시퀀스 관리.
/// 시퀀스: 트리거 → 플레이어 잠금 → 시네마틱 바 + 카메라 팬업 (병렬)
///         → 교수님 등장 → 분기 대사 → 엔딩 시퀀스 (헬기 + 독백 + 크레딧)
///         → 메인 메뉴
/// </summary>
public class RooftopManager : MonoBehaviour
{
    [Header("UI 참조")]
    public Image fadePanel;
    public GameObject dialogueBox;
    public TextMeshProUGUI dialogueText;
    public GameObject clickHint;
    // ───────────────────────────────────────────────────────────────
    // 대사 인덱스 매핑 (GameManager.GetDialogueIndex())
    // 비트: K(우진)=1, J(정범석)=2, P(박윤하)=4 / 1=죽음, 0=살음
    // ───────────────────────────────────────────────────────────────
    // idx | 우진(K) | 정범석(J) | 박윤하(P) | 살아있음 | 엔딩
    //  0  |  살음   |   살음    |   살음    |   3명    | 배드 (비동행)
    //  1  |  죽음   |   살음    |   살음    |   2명    | 배드 (비동행)
    //  2  |  살음   |   죽음    |   살음    |   2명    | 배드 (비동행)
    //  3  |  죽음   |   죽음    |   살음    |   1명    | 굿  (동행)
    //  4  |  살음   |   살음    |   죽음    |   2명    | 배드 (비동행)
    //  5  |  죽음   |   살음    |   죽음    |   1명    | 굿  (동행)
    //  6  |  살음   |   죽음    |   죽음    |   1명    | 굿  (동행)
    //  7  |  죽음   |   죽음    |   죽음    |   0명    | 굿  (동행)
    // ───────────────────────────────────────────────────────────────
    [Header("공통 기본 대사")]
    [TextArea(2, 5)] public string[] commonDialogueLines;

    [Header("8가지 대사 세트 (0~7)")]
    public DialogueSet[] dialogueSets = new DialogueSet[8];

    [System.Serializable]
    public class DialogueSet
    {
        [TextArea(2, 5)] public string[] lines;
    }

    [Header("시네마틱 바")]
    public RectTransform topBar;
    public RectTransform bottomBar;
    public float barHeight = 100f;
    public float barSlideDuration = 1f;

    [Header("카메라 팬업")]
    public Camera mainCamera;
    public float cameraPanUpDistance = 2f;
    public float cameraPanDuration = 1.5f;

    [Header("교수님 등장")]
    public GameObject professorObject;
    public Transform professorTargetPos;
    public Animator professorAnimator;
    public float professorWalkSpeed = 1.5f;

    [Header("헬기")]
    public GameObject helicopterObject;

    [Header("엔딩 대사 (헬기 도착 후)")]
    [TextArea(2, 5)] public string[] endingDialogueGood;
    [TextArea(2, 5)] public string[] endingDialogueBad;

    [Header("독백")]
    public GameObject monologuePanel;
    public TextMeshProUGUI monologueText;
    [TextArea(3, 8)] public string[] monologueGood;   // 주인공 독백
    [TextArea(3, 8)] public string[] monologueBad;    // 교수님 독백
    public float monologueTypingSpeed = 0.07f;
    public float monologueReadDelay = 2f;

    [Header("크레딧")]
    public GameObject creditsPanel;
    public TextMeshProUGUI creditsText;
    [TextArea(5, 15)] public string creditsContent = "프레이스홀더 크레딧\n\n...\n\n감사합니다.";
    public float creditsDuration = 6f;

    [Header("씬 전환")]
    public string mainMenuSceneName = "MainMenu";

    [Header("타이밍")]
    public float typingSpeed = 0.05f;
    public float fadeDuration = 1.5f;

    // ── 내부 상태 ──
    private bool cutsceneStarted = false;
    private GameObject playerRef;

    // 대화 입력 상태 (Update ↔ 코루틴 공유)
    private bool dialogueActive = false;
    private bool isTyping       = false;
    private bool skipTyping     = false;
    private bool advanceLine    = false;


    // ===================================================================
    // 초기화
    // ===================================================================

    void Update()
    {
        if (!dialogueActive) return;
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        if (isTyping)
            skipTyping = true;
        else
            advanceLine = true;
    }

    void Start()
    {
        if (fadePanel != null) fadePanel.color = new Color(0, 0, 0, 0);
        dialogueBox?.SetActive(false);

        // 시네마틱 바를 화면 밖으로 초기 위치
        if (topBar != null) topBar.anchoredPosition = new Vector2(0, barHeight);
        if (bottomBar != null) bottomBar.anchoredPosition = new Vector2(0, -barHeight);

        // 교수님 숨김
        professorObject?.SetActive(false);

        // 메인 카메라 자동 탐색
        if (mainCamera == null) mainCamera = Camera.main;
    }


    // ===================================================================
    // 컷씬 진입점
    // ===================================================================

    public void StartCutscene()
    {
        if (cutsceneStarted) return;
        cutsceneStarted = true;
        StartCoroutine(CutsceneSequence());
    }

    IEnumerator CutsceneSequence()
    {
        // 1. 플레이어 잠금 + HUD 숨김 + 타이머 정지
        TimerManager.Instance?.PauseTimer();
        SetPlayerControl(false);
        UICanvas.Instance?.HideUI();
        WeaponSlotUI.Instance?.Hide();
        HotbarManager.Instance?.Hide();
        InventoryManager.Instance?.inventoryUI?.SetActive(false);
        InventoryManager.Instance?.hotbarUI?.SetActive(false);
        yield return new WaitForSeconds(0.3f);

        // 2. 시네마틱 바 + 카메라 팬업 (병렬)
        Coroutine bars = StartCoroutine(SlideBarsIn());
        Coroutine cam = StartCoroutine(CameraPanUp());
        yield return bars;
        yield return cam;

        // 3. 교수님 등장
        yield return StartCoroutine(ProfessorEnter());

        // 4. 분기 대사 인덱스 결정
        int idx = 7;
        if (GameManager.Instance != null)
            idx = GameManager.Instance.GetDialogueIndex();
        Debug.Log($"[Rooftop] 대사 인덱스: {idx}");

        // 5. 공통 기본 대사 + 분기 대사 표시
        yield return StartCoroutine(PlayDialogueLines(commonDialogueLines));
        yield return StartCoroutine(PlayDialogueSet(dialogueSets[idx]));

        // 6. 엔딩 시퀀스
        bool companion = GameManager.Instance != null
                       && GameManager.Instance.IsCompanionEnding();
        yield return StartCoroutine(EndingSequence(companion));
    }


    // ===================================================================
    // 시네마틱 효과
    // ===================================================================

    IEnumerator SlideBarsIn()
    {
        Vector2 topStart = new Vector2(0, barHeight);
        Vector2 topEnd = Vector2.zero;
        Vector2 bottomStart = new Vector2(0, -barHeight);
        Vector2 bottomEnd = Vector2.zero;

        float t = 0f;
        while (t < barSlideDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0, 1, t / barSlideDuration);
            if (topBar != null) topBar.anchoredPosition = Vector2.Lerp(topStart, topEnd, p);
            if (bottomBar != null) bottomBar.anchoredPosition = Vector2.Lerp(bottomStart, bottomEnd, p);
            yield return null;
        }
    }

    IEnumerator CameraPanUp()
    {
        if (mainCamera == null) yield break;

        Vector3 start = mainCamera.transform.position;
        Vector3 end = start + Vector3.up * cameraPanUpDistance;

        float t = 0f;
        while (t < cameraPanDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0, 1, t / cameraPanDuration);
            mainCamera.transform.position = Vector3.Lerp(start, end, p);
            yield return null;
        }
    }


    // ===================================================================
    // 교수님 등장
    // ===================================================================

    IEnumerator ProfessorEnter()
    {
        if (professorObject == null || professorTargetPos == null) yield break;

        professorObject.SetActive(true);

        Vector3 start = professorObject.transform.position;
        Vector3 target = professorTargetPos.position;
        target.z = start.z;

        // Blend Tree: 위로 이동
        professorAnimator?.SetFloat("DirX", 0f);
        professorAnimator?.SetFloat("DirY", 1f);
        professorAnimator?.SetBool("IsWalking", true);

        float dist = Vector3.Distance(start, target);
        float duration = dist / professorWalkSpeed;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            professorObject.transform.position = Vector3.Lerp(start, target, t / duration);
            yield return null;
        }
        professorObject.transform.position = target;

        // 도착 → 주인공 마주보기 (아래) + 정지
        professorAnimator?.SetFloat("DirX", 0f);
        professorAnimator?.SetFloat("DirY", -1f);
        professorAnimator?.SetBool("IsWalking", false);
        yield return new WaitForSeconds(0.5f);
    }


    // ===================================================================
    // 엔딩 시퀀스
    // ===================================================================

    IEnumerator EndingSequence(bool isGoodEnding)
    {
        playerRef = GameObject.FindWithTag("Player");

        // ── 1. 페이드아웃 (검정 동안 헬기 활성화) ──
        yield return StartCoroutine(FadeTo(1f));
        helicopterObject?.SetActive(true);
        AudioManager.Instance?.PlaySFX("helicopter1");
        yield return new WaitForSeconds(0.3f);

        // ── 2. 페이드인 (헬기가 옥상에 짠!) ──
        yield return StartCoroutine(FadeTo(0f));
        yield return new WaitForSeconds(0.5f);

        // ── 3. 엔딩 대사 ──
        string[] lines = isGoodEnding ? endingDialogueGood : endingDialogueBad;
        yield return StartCoroutine(PlayDialogueLines(lines));

        // ── 4. 페이드아웃 (탑승 처리) ──
        yield return StartCoroutine(FadeTo(1f));
        yield return new WaitForSeconds(0.3f);

        if (isGoodEnding)
        {
            // 굿엔딩: 주인공 + 교수님 둘 다 탑승 (숨김)
            if (playerRef != null) playerRef.SetActive(false);
            if (professorObject != null) professorObject.SetActive(false);
        }
        else
        {
            // 배드엔딩: 주인공만 탑승, 교수님은 남음
            if (playerRef != null) playerRef.SetActive(false);
        }

        // ── 5. 페이드인 (헬기 + 남은 사람만 보임) ──
        yield return StartCoroutine(FadeTo(0f));
        yield return new WaitForSeconds(1f);

        // ── 6. 페이드아웃 (이륙 = 헬기 숨김) ──
        yield return StartCoroutine(FadeTo(1f));
        yield return new WaitForSeconds(0.3f);
        helicopterObject?.SetActive(false);
        yield return new WaitForSeconds(0.5f);

        // ── 7. 독백 표시 ──
        string[] monoLines = isGoodEnding ? monologueGood : monologueBad;
        yield return StartCoroutine(PlayMonologue(monoLines));

        // ── 8. 독백 끝 → 크레딧 ──
        monologuePanel?.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(ShowCredits());

        // ── 9. 메인 메뉴 ──
        GoToMainMenu();
    }


    // ===================================================================
    // 대사 / 독백 / 크레딧
    // ===================================================================

    /// <summary>분기 대사용 (DialogueSet 객체)</summary>
    IEnumerator PlayDialogueSet(DialogueSet set)
    {
        if (set == null || set.lines == null) yield break;

        dialogueBox.SetActive(true);
        dialogueActive = true;
        foreach (string line in set.lines)
        {
            yield return StartCoroutine(TypeLine(line));
            yield return StartCoroutine(WaitForAdvance());
        }
        dialogueActive = false;
        dialogueBox.SetActive(false);
    }

    /// <summary>엔딩 대사용 (string 배열)</summary>
    IEnumerator PlayDialogueLines(string[] lines)
    {
        if (lines == null || lines.Length == 0) yield break;

        dialogueBox.SetActive(true);
        dialogueActive = true;
        foreach (string line in lines)
        {
            yield return StartCoroutine(TypeLine(line));
            yield return StartCoroutine(WaitForAdvance());
        }
        dialogueActive = false;
        dialogueBox.SetActive(false);
    }

    /// <summary>검정 화면 위 독백 (타이핑 후 자동 진행)</summary>
    IEnumerator PlayMonologue(string[] lines)
    {
        if (lines == null || lines.Length == 0) yield break;
        if (monologuePanel == null || monologueText == null) yield break;

        monologuePanel.SetActive(true);

        foreach (string line in lines)
        {
            monologueText.text = "";
            foreach (char c in line)
            {
                monologueText.text += c;
                yield return new WaitForSeconds(monologueTypingSpeed);
            }

            yield return new WaitForSeconds(monologueReadDelay);
            yield return new WaitForSeconds(0.3f);
        }
    }

    /// <summary>크레딧 표시 (정적 텍스트)</summary>
    IEnumerator ShowCredits()
    {
        if (creditsPanel == null || creditsText == null) yield break;

        creditsText.text = creditsContent;
        creditsPanel.SetActive(true);

        yield return StartCoroutine(FadeTo(0f));
        yield return new WaitForSeconds(creditsDuration);
        yield return StartCoroutine(FadeTo(1f));

        creditsPanel.SetActive(false);
    }


    // ===================================================================
    // 공용 유틸 (타이핑 / 페이드 / 플레이어 제어)
    // ===================================================================

    IEnumerator TypeLine(string text)
    {
        clickHint?.SetActive(false);
        dialogueText.text = "";
        isTyping   = true;
        skipTyping = false;

        foreach (char c in text)
        {
            if (skipTyping) break;
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        dialogueText.text = text; // 스킵 시에도 전체 텍스트 보장

        isTyping = false;
        clickHint?.SetActive(true);
    }

    IEnumerator WaitForAdvance()
    {
        advanceLine = false;
        while (!advanceLine)
            yield return null;
    }

    IEnumerator FadeTo(float targetAlpha)
    {
        if (fadePanel == null) yield break;

        float startAlpha = fadePanel.color.a;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
            fadePanel.color = new Color(0, 0, 0, a);
            yield return null;
        }
        fadePanel.color = new Color(0, 0, 0, targetAlpha);
    }

    void SetPlayerControl(bool enabled)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = enabled;

        if (!enabled)
        {
            // Rigidbody2D 속도 0
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            // Animator 정리 + 위쪽 강제
            var anim = player.GetComponent<Animator>()
                    ?? player.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsWalking", false);
                anim.SetBool("IsAttacking", false);
                anim.SetBool("IsHurt", false);
                anim.SetFloat("DirX", 0f);
                anim.SetFloat("DirY", 1f);
            }
        }
    }


    // ===================================================================
    // 씬 전환
    // ===================================================================

    void GoToMainMenu()
    {
        GameManager.Instance?.ResetGame();
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }
}