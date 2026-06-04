/*
 * RooftopManager.cs
 * 역할: 옥상 최종 컷씬, 교수 등장, 8분기 대사, 굿/배드 엔딩 대사, 독백, 크레딧을 순차 실행합니다.
 * 연결: GameManager의 K/J/P 사망 플래그와 GetDialogueIndex/IsCompanionEnding 결과로 엔딩 분기를 결정합니다.
 * 주의: dialogueSets는 0~7 인덱스가 엔딩 분기 비트와 직접 매핑되므로 배열 순서를 바꾸면 대사가 어긋납니다.
 */
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
    // ── 대화 UI ─────────────────────────────────────────────────────────
    [Header("UI 참조")]
    public Image           fadePanel;    // 화면 전환용 검정 패널 (알파 0~1)
    public GameObject      dialogueBox;  // 대사 창 루트 오브젝트 (배경 포함)
    public TextMeshProUGUI dialogueText; // 대사 텍스트가 타이핑되는 TMP 컴포넌트
    public GameObject      clickHint;    // "스페이스바" 힌트 오브젝트

    // ── 대사 인덱스 매핑 ────────────────────────────────────────────────
    // GameManager.GetDialogueIndex()의 반환값으로 8가지 대사 세트를 선택한다.
    // 비트 연산: K(우진)=1, J(정범석)=2, P(박윤하)=4 / 해당 비트가 1이면 사망
    // ─────────────────────────────────────────────────────────────────
    // idx | 우진(K) | 정범석(J) | 박윤하(P) | 살아있음 | 엔딩
    //  0  |  살음   |   살음    |   살음    |   3명    | 배드 (비동행)
    //  1  |  죽음   |   살음    |   살음    |   2명    | 배드 (비동행)
    //  2  |  살음   |   죽음    |   살음    |   2명    | 배드 (비동행)
    //  3  |  죽음   |   죽음    |   살음    |   1명    | 굿  (동행)
    //  4  |  살음   |   살음    |   죽음    |   2명    | 배드 (비동행)
    //  5  |  죽음   |   살음    |   죽음    |   1명    | 굿  (동행)
    //  6  |  살음   |   죽음    |   죽음    |   1명    | 굿  (동행)
    //  7  |  죽음   |   죽음    |   죽음    |   0명    | 굿  (동행)
    // ─────────────────────────────────────────────────────────────────
    // 굿엔딩(동행): 생존자 ≤ 1명 → IsCompanionEnding() == true
    // 배드엔딩(비동행): 생존자 ≥ 2명 → IsCompanionEnding() == false

    // ── 분기 대사 ───────────────────────────────────────────────────────
    // commonDialogueLines: 모든 엔딩에서 공통으로 재생되는 초기 대사
    // dialogueSets: idx(0~7)에 따라 선택되는 분기 대사 (배열 순서 = 비트 코드)
    [Header("공통 기본 대사")]
    [TextArea(2, 5)] public string[] commonDialogueLines;

    [Header("8가지 대사 세트 (0~7)")]
    public DialogueSet[] dialogueSets = new DialogueSet[8];

    [System.Serializable]
    public class DialogueSet
    {
        [TextArea(2, 5)] public string[] lines; // 해당 엔딩 분기의 대사 줄 배열
    }

    // ── 시네마틱 바 ─────────────────────────────────────────────────────
    // 컷씬 시작 시 화면 위아래에서 검정 바가 슬라이드 인(레터박스 효과).
    // barHeight: 화면 밖 초기 위치 (anchoredPosition Y의 오프셋)
    [Header("시네마틱 바")]
    public RectTransform topBar;           // 위쪽 레터박스 바
    public RectTransform bottomBar;        // 아래쪽 레터박스 바
    public float barHeight = 100f;         // 슬라이드 시작 오프셋 (픽셀)
    public float barSlideDuration = 1f;    // 슬라이드 완료까지 걸리는 시간 (초)

    // ── 카메라 팬업 ─────────────────────────────────────────────────────
    // 컷씬 시작 시 카메라가 위쪽으로 이동해 시네마틱 연출을 강조한다.
    [Header("카메라 팬업")]
    public Camera mainCamera;              // Inspector 미연결 시 Start()에서 Camera.main 자동 할당
    public float cameraPanUpDistance = 2f; // 카메라가 위로 이동하는 거리 (유닛)
    public float cameraPanDuration = 1.5f; // 팬업 완료까지 걸리는 시간 (초)

    // ── 교수님 등장 ─────────────────────────────────────────────────────
    // 컷씬 초반에 교수님이 화면 안으로 걸어들어오는 연출.
    // professorTargetPos: 교수님이 걸어갈 목표 지점 Transform
    [Header("교수님 등장")]
    public GameObject professorObject;     // 교수님 캐릭터 오브젝트
    public Transform  professorTargetPos;  // 교수님이 도달할 목표 위치
    public Animator   professorAnimator;   // 교수님 Animator (Blend Tree: DirX, DirY, IsWalking)
    public float      professorWalkSpeed = 1.5f; // 교수님 이동 속도 (유닛/초)

    // ── 헬기 ────────────────────────────────────────────────────────────
    // 엔딩 시퀀스에서 페이드아웃 중 활성화되고, 이륙 직전 비활성화된다.
    [Header("헬기")]
    public GameObject helicopterObject;

    // ── 엔딩 대사 ───────────────────────────────────────────────────────
    // 헬기 도착 후 재생되는 굿/배드 엔딩 전용 대사.
    // 굿엔딩: 교수님도 함께 탑승 / 배드엔딩: 교수님이 남고 주인공만 탑승
    [Header("엔딩 대사 (헬기 도착 후)")]
    [TextArea(2, 5)] public string[] endingDialogueGood; // 굿엔딩(동행) 대사
    [TextArea(2, 5)] public string[] endingDialogueBad;  // 배드엔딩(비동행) 대사

    // ── 독백 ────────────────────────────────────────────────────────────
    // 이륙 후 검정 화면에서 타이핑 애니메이션으로 표시되는 나레이션.
    // 굿엔딩: 주인공 독백 / 배드엔딩: 교수님 독백
    // monologueReadDelay: 한 줄 타이핑 완료 후 다음 줄로 넘어가기 전 대기 시간
    [Header("독백")]
    public GameObject      monologuePanel;          // 독백 전용 검정 배경 패널
    public TextMeshProUGUI monologueText;            // 독백 텍스트 TMP 컴포넌트
    [TextArea(3, 8)] public string[] monologueGood;  // 굿엔딩 주인공 독백 줄 배열
    [TextArea(3, 8)] public string[] monologueBad;   // 배드엔딩 교수님 독백 줄 배열
    public float monologueTypingSpeed = 0.07f;       // 독백 타이핑 속도 (초/글자)
    public float monologueReadDelay = 2f;            // 타이핑 완료 후 자동 넘김 대기 (초)

    // ── 크레딧 ──────────────────────────────────────────────────────────
    // 독백 이후 표시되는 엔딩 크레딧. 정적 텍스트로 creditsDuration 초 동안 유지된다.
    [Header("크레딧")]
    public GameObject      creditsPanel;   // 크레딧 전용 패널
    public TextMeshProUGUI creditsText;    // 크레딧 내용 TMP 컴포넌트
    [TextArea(5, 15)] public string creditsContent = "프레이스홀더 크레딧\n\n...\n\n감사합니다.";
    public float creditsDuration = 6f;    // 크레딧이 표시되는 시간 (초)

    // ── 씬 전환 ─────────────────────────────────────────────────────────
    [Header("씬 전환")]
    public string mainMenuSceneName = "MainMenu"; // 크레딧 이후 로드할 씬 이름

    // ── 타이밍 설정 ─────────────────────────────────────────────────────
    [Header("타이밍")]
    public float typingSpeed  = 0.05f;  // 대화창 타이핑 속도 (초/글자)
    public float fadeDuration = 1.5f;   // FadeTo() 페이드 완료까지 걸리는 시간 (초)

    // ── 내부 상태 변수 ───────────────────────────────────────────────────
    private bool      cutsceneStarted = false; // StartCutscene()의 중복 호출 방지
    private GameObject playerRef;              // EndingSequence에서 플레이어를 숨길 때 사용

    // 대화 입력 상태 — Update와 TypeLine/WaitForAdvance 코루틴이 공유한다.
    // dialogueActive: 대화창이 열려 있을 때 Update의 입력을 활성화하는 게이트
    // isTyping:       타이핑 중일 때 true → 스페이스 → skipTyping = true
    // skipTyping:     true이면 TypeLine이 루프를 즉시 빠져나와 전체 텍스트를 표시
    // advanceLine:    true이면 WaitForAdvance가 대기를 종료하고 다음 줄로 진행
    private bool dialogueActive = false;
    private bool isTyping       = false;
    private bool skipTyping     = false;
    private bool advanceLine    = false;


    // ===================================================================
    // 초기화
    // ===================================================================

    /// <summary>
    /// 대화 입력 게이트: dialogueActive가 true일 때만 스페이스 입력을 처리한다.
    /// - 타이핑 중(isTyping): skipTyping을 세워 TypeLine이 텍스트를 즉시 완성하게 함
    /// - 대기 중(!isTyping):  advanceLine을 세워 WaitForAdvance가 대기를 종료하게 함
    /// </summary>
    void Update()
    {
        if (!dialogueActive) return;
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        if (isTyping)
            skipTyping = true;  // 타이핑 스킵
        else
            advanceLine = true; // 다음 줄로 진행
    }

    void Start()
    {
        // 페이드 패널을 투명하게 초기화 (이전 씬에서 값이 남아있을 수 있음)
        if (fadePanel != null) fadePanel.color = new Color(0, 0, 0, 0);
        dialogueBox?.SetActive(false); // 대화창은 대사가 시작될 때까지 숨김

        // 시네마틱 바를 화면 밖(오프스크린) 위치로 초기화
        // Start → SlideBarsIn에서 (0, ±barHeight) → (0, 0)으로 슬라이드인
        if (topBar    != null) topBar.anchoredPosition    = new Vector2(0,  barHeight);
        if (bottomBar != null) bottomBar.anchoredPosition = new Vector2(0, -barHeight);

        // 교수님은 ProfessorEnter() 실행 전까지 숨김
        professorObject?.SetActive(false);

        // mainCamera가 Inspector에서 연결되지 않은 경우 자동 탐색
        if (mainCamera == null) mainCamera = Camera.main;
    }


    // ===================================================================
    // 컷씬 진입점
    // ===================================================================

    /// <summary>
    /// RooftopTrigger가 호출하는 컷씬 시작 메서드.
    /// cutsceneStarted 플래그로 중복 실행을 방지한다.
    /// </summary>
    public void StartCutscene()
    {
        if (cutsceneStarted) return;
        cutsceneStarted = true;
        StartCoroutine(CutsceneSequence());
    }

    /// <summary>
    /// 옥상 컷씬 전체 흐름을 순서대로 실행하는 메인 코루틴.
    /// 1. 플레이어 잠금 + HUD 숨김
    /// 2. 시네마틱 바 슬라이드인 + 카메라 팬업 (병렬)
    /// 3. 교수님 등장 연출
    /// 4. 엔딩 분기 인덱스 결정 (K/J/P 사망 플래그 비트 연산)
    /// 5. 공통 대사 → 분기 대사 재생
    /// 6. 굿/배드 엔딩 시퀀스 실행
    /// </summary>
    IEnumerator CutsceneSequence()
    {
        // 1. 플레이어 잠금 + HUD 숨김 + 타이머 정지
        TimerManager.Instance?.PauseTimer();
        SetPlayerControl(false); // 이동·전투 불가
        UICanvas.Instance?.HideUI();
        WeaponSlotUI.Instance?.Hide();
        HotbarManager.Instance?.Hide();
        // InventoryManager의 UI는 씬 전환 시 파괴된 오브젝트일 수 있으므로
        // C# ?. 연산자 대신 Unity의 == 연산자를 사용해 파괴된 오브젝트를 올바르게 감지
        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            if (inv.inventoryUI != null) inv.inventoryUI.SetActive(false);
            if (inv.hotbarUI    != null) inv.hotbarUI.SetActive(false);
        }
        yield return new WaitForSeconds(0.3f);

        // 2. 시네마틱 바 슬라이드인 + 카메라 팬업 동시 실행 (병렬 코루틴 패턴)
        // 두 코루틴을 동시에 StartCoroutine하고 순서대로 yield하면
        // 각각 독립적으로 진행되다가 둘 다 끝난 뒤 다음 단계로 진행된다.
        Coroutine bars = StartCoroutine(SlideBarsIn());
        Coroutine cam  = StartCoroutine(CameraPanUp());
        yield return bars; // 바 슬라이드 완료 대기
        yield return cam;  // 카메라 팬업 완료 대기 (이미 끝났으면 즉시 통과)

        // 3. 교수님 등장
        yield return StartCoroutine(ProfessorEnter());

        // 4. 엔딩 분기 인덱스 결정
        // GameManager가 없는 경우 idx=7 (모두 사망)을 기본값으로 사용
        int idx = 7;
        if (GameManager.Instance != null)
            idx = GameManager.Instance.GetDialogueIndex();
        Debug.Log($"[Rooftop] 대사 인덱스: {idx}");

        // 5. 공통 대사 → 분기 대사 재생
        yield return StartCoroutine(PlayDialogueLines(commonDialogueLines));
        yield return StartCoroutine(PlayDialogueSet(dialogueSets[idx]));

        // 6. 엔딩 시퀀스 (굿/배드 판정 후 헬기 → 독백 → 크레딧)
        bool companion = GameManager.Instance != null
                       && GameManager.Instance.IsCompanionEnding(); // 생존자 ≤1명이면 굿엔딩
        yield return StartCoroutine(EndingSequence(companion));
    }


    // ===================================================================
    // 시네마틱 효과
    // ===================================================================

    /// <summary>
    /// 위아래 레터박스 바를 화면 밖에서 안으로 SmoothStep으로 슬라이드인한다.
    /// SmoothStep을 사용해 시작과 끝에서 자연스럽게 감속한다.
    /// </summary>
    IEnumerator SlideBarsIn()
    {
        Vector2 topStart    = new Vector2(0,  barHeight); // 오프스크린 위
        Vector2 topEnd      = Vector2.zero;               // 화면 가장자리
        Vector2 bottomStart = new Vector2(0, -barHeight); // 오프스크린 아래
        Vector2 bottomEnd   = Vector2.zero;

        float t = 0f;
        while (t < barSlideDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0, 1, t / barSlideDuration); // 감속 보간
            if (topBar    != null) topBar.anchoredPosition    = Vector2.Lerp(topStart,    topEnd,    p);
            if (bottomBar != null) bottomBar.anchoredPosition = Vector2.Lerp(bottomStart, bottomEnd, p);
            yield return null;
        }
    }

    /// <summary>
    /// 메인 카메라를 위쪽으로 cameraPanUpDistance만큼 SmoothStep으로 이동한다.
    /// SlideBarsIn과 병렬로 실행되어 컷씬 시작 연출을 풍성하게 만든다.
    /// </summary>
    IEnumerator CameraPanUp()
    {
        if (mainCamera == null) yield break;

        Vector3 start = mainCamera.transform.position;
        Vector3 end   = start + Vector3.up * cameraPanUpDistance;

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

    /// <summary>
    /// 교수님 오브젝트를 활성화하고 professorTargetPos까지 걸어가게 한다.
    /// Animator Blend Tree 파라미터(DirX, DirY, IsWalking)를 직접 설정한다.
    /// 이동 거리와 속도로 duration을 계산해 Lerp로 부드럽게 이동한다.
    /// 도착 후 주인공 방향(아래)을 바라보고 정지한다.
    /// </summary>
    IEnumerator ProfessorEnter()
    {
        if (professorObject == null || professorTargetPos == null) yield break;

        professorObject.SetActive(true);

        Vector3 start  = professorObject.transform.position;
        Vector3 target = professorTargetPos.position;
        target.z = start.z; // 2D이므로 Z축은 유지

        // 위쪽으로 이동하는 애니메이션 파라미터 설정
        professorAnimator?.SetFloat("DirX", 0f);
        professorAnimator?.SetFloat("DirY", 1f);
        professorAnimator?.SetBool("IsWalking", true);

        // 거리 ÷ 속도 = 이동 시간 계산
        float dist     = Vector3.Distance(start, target);
        float duration = dist / professorWalkSpeed;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            professorObject.transform.position = Vector3.Lerp(start, target, t / duration);
            yield return null;
        }
        professorObject.transform.position = target; // 오차 보정: 정확한 목표 위치로 스냅

        // 도착 후 주인공 방향(아래)을 바라보며 정지
        professorAnimator?.SetFloat("DirX", 0f);
        professorAnimator?.SetFloat("DirY", -1f);
        professorAnimator?.SetBool("IsWalking", false);
        yield return new WaitForSeconds(0.5f); // 짧은 대기 후 대사 시작
    }


    // ===================================================================
    // 엔딩 시퀀스
    // ===================================================================

    /// <summary>
    /// 헬기 도착 → 탑승 → 이륙 → 독백 → 크레딧 순서의 최종 엔딩 코루틴.
    ///
    /// isGoodEnding(굿엔딩/동행): 생존자 ≤1명 → 교수님 + 주인공 둘 다 탑승
    /// !isGoodEnding(배드엔딩/비동행): 생존자 ≥2명 → 주인공만 탑승, 교수님 잔류
    ///
    /// 페이드 흐름:
    ///   FadeTo(1) [암전→헬기 스폰] → FadeTo(0) [헬기 공개] →
    ///   대사 → FadeTo(1) [탑승 처리] → FadeTo(0) [공개] →
    ///   FadeTo(1) [이륙] → 독백 → 크레딧 → 메인 메뉴
    /// </summary>
    IEnumerator EndingSequence(bool isGoodEnding)
    {
        playerRef = GameObject.FindWithTag("Player"); // 탑승 시 비활성화할 플레이어 참조

        // ── 1. 페이드아웃 → 암전 중에 헬기를 조용히 활성화 ──
        yield return StartCoroutine(FadeTo(1f));
        helicopterObject?.SetActive(true);
        AudioManager.Instance?.PlaySFX("helicopter1");
        yield return new WaitForSeconds(0.3f);

        // ── 2. 페이드인 → 헬기가 옥상에 있는 장면 공개 ──
        yield return StartCoroutine(FadeTo(0f));
        yield return new WaitForSeconds(0.5f);

        // ── 3. 엔딩 대사 (굿/배드 분기) ──
        string[] lines = isGoodEnding ? endingDialogueGood : endingDialogueBad;
        yield return StartCoroutine(PlayDialogueLines(lines));

        // ── 4. 페이드아웃 → 암전 중에 탑승 처리 (캐릭터 비활성화) ──
        yield return StartCoroutine(FadeTo(1f));
        yield return new WaitForSeconds(0.3f);

        if (isGoodEnding)
        {
            // 굿엔딩: 주인공 + 교수님 모두 헬기에 탑승 → 둘 다 숨김
            if (playerRef         != null) playerRef.SetActive(false);
            if (professorObject   != null) professorObject.SetActive(false);
        }
        else
        {
            // 배드엔딩: 주인공만 탑승 → 교수님은 화면에 남음
            if (playerRef != null) playerRef.SetActive(false);
        }

        // ── 5. 페이드인 → 빈 옥상 + 헬기만 보이는 이륙 준비 장면 ──
        yield return StartCoroutine(FadeTo(0f));
        yield return new WaitForSeconds(1f);

        // ── 6. 페이드아웃 → 암전 중에 헬기 비활성화 (이륙 표현) ──
        yield return StartCoroutine(FadeTo(1f));
        yield return new WaitForSeconds(0.3f);
        helicopterObject?.SetActive(false);
        yield return new WaitForSeconds(0.5f);

        // ── 7. 독백 (검정 화면 위에 타이핑으로 표시) ──
        // FadePanel이 알파=1인 상태에서 MonologuePanel이 그 위에 렌더링됨
        string[] monoLines = isGoodEnding ? monologueGood : monologueBad;
        yield return StartCoroutine(PlayMonologue(monoLines));

        // ── 8. 독백 패널 숨김 → 크레딧 표시 ──
        monologuePanel?.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(ShowCredits());

        // ── 9. 메인 메뉴로 씬 전환 ──
        GoToMainMenu();
    }


    // ===================================================================
    // 대사 / 독백 / 크레딧
    // ===================================================================

    /// <summary>
    /// DialogueSet 객체(8분기 대사)를 재생하는 코루틴.
    /// 각 줄을 TypeLine으로 타이핑하고, WaitForAdvance로 스페이스 입력을 기다린다.
    /// </summary>
    IEnumerator PlayDialogueSet(DialogueSet set)
    {
        if (set == null || set.lines == null) yield break;

        dialogueBox.SetActive(true);
        dialogueActive = true; // Update의 스페이스바 입력 처리 활성화
        foreach (string line in set.lines)
        {
            yield return StartCoroutine(TypeLine(line));
            yield return StartCoroutine(WaitForAdvance()); // 플레이어 스페이스 대기
        }
        dialogueActive = false;
        dialogueBox.SetActive(false);
    }

    /// <summary>
    /// string 배열(공통 대사·엔딩 대사)을 재생하는 코루틴.
    /// PlayDialogueSet과 동일한 로직이지만 DialogueSet 래퍼 없이 배열을 직접 받는다.
    /// </summary>
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

    /// <summary>
    /// 검정 화면(FadePanel 알파=1) 위에서 독백을 한 글자씩 표시하고 자동 진행하는 코루틴.
    /// 대화창(dialogueBox)과 달리 스페이스 입력 없이 monologueReadDelay 후 자동으로 다음 줄로 넘어간다.
    /// </summary>
    IEnumerator PlayMonologue(string[] lines)
    {
        if (lines == null || lines.Length == 0) yield break;
        if (monologuePanel == null || monologueText == null) yield break;

        monologuePanel.SetActive(true); // 검정 배경 패널 표시 (FadePanel 위에 렌더링됨)

        foreach (string line in lines)
        {
            monologueText.text = ""; // 이전 줄 텍스트 초기화
            foreach (char c in line)
            {
                monologueText.text += c;
                yield return new WaitForSeconds(monologueTypingSpeed);
            }

            // 타이핑 완료 후 읽을 시간을 주고 자동으로 다음 줄로 진행
            yield return new WaitForSeconds(monologueReadDelay);
            yield return new WaitForSeconds(0.3f); // 줄 간 짧은 공백
        }
    }

    /// <summary>
    /// 크레딧 텍스트를 표시하고 creditsDuration 동안 유지하는 코루틴.
    /// FadePanel이 알파=1인 상태에서 CreditsPanel을 활성화한 뒤 페이드인으로 공개한다.
    /// </summary>
    IEnumerator ShowCredits()
    {
        if (creditsPanel == null || creditsText == null) yield break;

        creditsText.text = creditsContent; // 크레딧 내용 설정
        creditsPanel.SetActive(true);      // FadePanel 위에 크레딧 패널 표시

        yield return StartCoroutine(FadeTo(0f));           // 페이드인: 크레딧 패널이 드러남
        yield return new WaitForSeconds(creditsDuration);  // 크레딧 유지 시간 대기
        yield return StartCoroutine(FadeTo(1f));           // 페이드아웃: 메인 메뉴 전환 준비

        creditsPanel.SetActive(false);
    }


    // ===================================================================
    // 공용 유틸 (타이핑 / 페이드 / 플레이어 제어)
    // ===================================================================

    /// <summary>
    /// 대화창에 한 글자씩 타이핑 효과를 적용하는 코루틴.
    /// skipTyping이 true가 되면 루프를 즉시 종료하고 전체 텍스트를 표시한다.
    /// 완료 후 clickHint를 활성화해 플레이어에게 스페이스 입력을 유도한다.
    /// </summary>
    IEnumerator TypeLine(string text)
    {
        clickHint?.SetActive(false);
        dialogueText.text = "";
        isTyping   = true;
        skipTyping = false;

        foreach (char c in text)
        {
            if (skipTyping) break; // 스페이스 스킵 요청 시 즉시 탈출
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        dialogueText.text = text; // 스킵 시에도 전체 텍스트가 보이도록 보장

        isTyping = false;
        clickHint?.SetActive(true); // "계속하려면 스페이스" 힌트 표시
    }

    /// <summary>
    /// advanceLine이 true가 될 때까지(= 플레이어가 스페이스를 누를 때까지) 대기하는 코루틴.
    /// 매 프레임 null을 yield해 다른 코루틴과 Update가 실행될 수 있도록 한다.
    /// </summary>
    IEnumerator WaitForAdvance()
    {
        advanceLine = false; // 이전 프레임에 세워진 플래그 초기화
        while (!advanceLine)
            yield return null;
    }

    /// <summary>
    /// fadePanel의 알파를 현재값에서 targetAlpha까지 fadeDuration 초 동안 선형 보간한다.
    /// FadeTo(1f): 화면 검정 / FadeTo(0f): 화면 밝아짐
    /// </summary>
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
        fadePanel.color = new Color(0, 0, 0, targetAlpha); // 오차 보정: 정확한 최종값 설정
    }

    /// <summary>
    /// 플레이어의 이동(PlayerMovement)을 활성화/비활성화하고,
    /// 비활성화 시 Rigidbody2D 속도를 0으로 만들고 Animator를 대기 상태로 초기화한다.
    /// </summary>
    void SetPlayerControl(bool enabled)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = enabled;

        if (!enabled)
        {
            // 관성 제거: 잠금 직전 이동 속도를 즉시 0으로 멈춤
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            // Animator 파라미터 초기화: 걷기/공격/피격 상태를 해제하고 위쪽을 바라보게 설정
            var anim = player.GetComponent<Animator>()
                    ?? player.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsWalking",   false);
                anim.SetBool("IsAttacking", false);
                anim.SetBool("IsHurt",      false);
                anim.SetFloat("DirX", 0f);
                anim.SetFloat("DirY", 1f); // 위쪽(컷씬 방향)을 바라보는 기본 자세
            }
        }
    }


    // ===================================================================
    // 씬 전환
    // ===================================================================

    /// <summary>
    /// GameManager 상태를 초기화하고 메인 메뉴 씬으로 전환한다.
    /// ResetGame()은 플래그·무기·엔딩 분기 변수를 모두 초기값으로 되돌린다.
    /// </summary>
    void GoToMainMenu()
    {
        GameManager.Instance?.ResetGame();
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }
}
