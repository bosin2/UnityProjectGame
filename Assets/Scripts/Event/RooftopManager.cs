using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 옥상 씬 컷씬 전체 시퀀스 관리.
/// 시퀀스: 트리거 → 플레이어 잠금 → 시네마틱 바 + 카메라 팬업 (병렬)
///         → 교수님 등장 + 워킹 → 대사 (8개 분기) → 페이드아웃 → 엔딩
/// </summary>
public class RooftopManager : MonoBehaviour
{
    [Header("UI 참조")]
    public Image fadePanel;
    public GameObject dialogueBox;
    public TextMeshProUGUI dialogueText;
    public GameObject clickHint;

    [Header("엔딩 화면")]
    public GameObject Ending1;
    public GameObject Ending2;

    [Header("8가지 대사 세트 (0~7)")]
    public DialogueSet[] dialogueSets = new DialogueSet[8];

    [System.Serializable]
    public class DialogueSet
    {
        [TextArea(2, 5)] public string[] lines;
    }

    // ===== [NEW] 시네마틱 효과 =====

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

    // ============================

    [Header("타이밍")]
    public float typingSpeed = 0.05f;
    public float fadeDuration = 1.5f;

    private bool cutsceneStarted = false;

    void Start()
    {
        if (fadePanel != null) fadePanel.color = new Color(0, 0, 0, 0);
        dialogueBox?.SetActive(false);
        Ending1?.SetActive(false);
        Ending2?.SetActive(false);

        // 시네마틱 바를 화면 밖으로 초기 위치
        if (topBar != null) topBar.anchoredPosition = new Vector2(0, barHeight);
        if (bottomBar != null) bottomBar.anchoredPosition = new Vector2(0, -barHeight);

        // 교수님 숨김
        professorObject?.SetActive(false);

        // 메인 카메라 자동 탐색
        if (mainCamera == null) mainCamera = Camera.main;
    }

    public void StartCutscene()
    {
        if (cutsceneStarted) return;
        cutsceneStarted = true;
        StartCoroutine(CutsceneSequence());
    }

    IEnumerator CutsceneSequence()
    {
        // 1. 플레이어 잠금 + HUD 숨김
        SetPlayerControl(false);
        UICanvas.Instance?.HideUI();
        yield return new WaitForSeconds(0.3f);

        // 2. 시네마틱 바 + 카메라 팬업 (병렬 실행)
        Coroutine bars = StartCoroutine(SlideBarsIn());
        Coroutine cam = StartCoroutine(CameraPanUp());
        yield return bars;
        yield return cam;

        // 3. 교수님 등장 + 워킹
        yield return StartCoroutine(ProfessorEnter());

        // 4. 대사 인덱스 결정
        int idx = 7;
        if (GameManager.Instance != null)
            idx = GameManager.Instance.GetDialogueIndex();
        Debug.Log($"[Rooftop] 대사 인덱스: {idx}");

        // 5. 대사 표시
        dialogueBox.SetActive(true);
        DialogueSet set = dialogueSets[idx];
        if (set != null && set.lines != null)
        {
            foreach (string line in set.lines)
            {
                yield return StartCoroutine(TypeLine(line));
                yield return StartCoroutine(WaitForSpace());
            }
        }
        dialogueBox.SetActive(false);

        // 6. 페이드아웃
        yield return StartCoroutine(FadeTo(1f));

        // 7. 엔딩 화면
        bool companion = GameManager.Instance != null
                       && GameManager.Instance.IsCompanionEnding();
        if (companion)
        {
            Ending1?.SetActive(true);
            Debug.Log("[Rooftop] 동행 엔딩");
        }
        else
        {
            Ending2?.SetActive(true);
            Debug.Log("[Rooftop] 비동행 엔딩");
        }

        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(FadeTo(0f));
    }

    // ===== [NEW] 시네마틱 바 슬라이드 =====

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

    // ===== [NEW] 카메라 팬업 =====

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

    // ===== [NEW] 교수님 등장 + 워킹 =====

    IEnumerator ProfessorEnter()
    {
        if (professorObject == null || professorTargetPos == null) yield break;

        professorObject.SetActive(true);
        professorAnimator?.SetBool("IsWalking", true);

        Vector3 start = professorObject.transform.position;
        Vector3 target = professorTargetPos.position;
        target.z = start.z;

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

        professorAnimator?.SetBool("IsWalking", false);
        yield return new WaitForSeconds(0.5f);
    }

    // ===== 기존 코드 (변경 없음) =====

    IEnumerator TypeLine(string text)
    {
        clickHint?.SetActive(false);
        dialogueText.text = "";

        foreach (char c in text)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                dialogueText.text = text;
                break;
            }
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        clickHint?.SetActive(true);
    }

    IEnumerator WaitForSpace()
    {
        yield return new WaitUntil(() => !Input.GetKey(KeyCode.Space));
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
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
    }
}