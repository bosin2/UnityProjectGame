/*
 * GameFlowManager.cs
 * 역할: GameLap 씬의 인트로, 튜토리얼, 실제 게임플레이 전환을 관리하는 핵심 흐름 컨트롤러입니다.
 * 연결: GameManager 플래그(introDone/tutorialDone/gunNPCDead), PlayerCombat 무기 상태, UI/AudioManager와 강하게 연결됩니다.
 * 주의: 이 파일의 플래그 문자열은 씬의 Interactable.requiredFlag와 정확히 같아야 하며, 대소문자도 구분됩니다.
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// GameLap 씬 안에서 인트로 → 튜토리얼 → 게임플레이 흐름을 관리하는 컴포넌트.
///
/// [씬 구조]
/// introRoot    : 인트로 컷씬 그룹
/// tutorialRoot : 튜토리얼 그룹 (시계, NPC, 파이프 등)
/// gameplayRoot : 실제 게임플레이 그룹 (맵, 몬스터 등)
///
/// [UI 참조 방식]
/// weaponSlotUI / hotbarManager 는 Inspector에서 드래그 연결.
/// 없으면 씬에서 자동 탐색.
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    // ── 씬 루트 오브젝트 ────────────────────────────────────────────────
    // 각 단계(인트로/튜토리얼/게임플레이)에 해당하는 오브젝트 묶음.
    // Start()에서 진행 상태에 따라 하나만 활성화된다.
    [Header("단계별 루트 오브젝트")]
    public GameObject introRoot;      // 인트로 컷씬 전체 (대화창, 배경 이미지 등)
    public GameObject tutorialRoot;   // 튜토리얼 구역 (시계, 파이프 NPC 등)
    public GameObject gameplayRoot;   // 실제 게임플레이 맵과 몬스터

    // ── 인트로 UI 요소 ──────────────────────────────────────────────────
    [Header("인트로 UI")]
    public TextMeshProUGUI dialogueText; // 대사 텍스트가 타이핑되는 TMP 컴포넌트
    public GameObject      clickHint;    // "스페이스바를 누르세요" 힌트 오브젝트
    public Image           fadePanel;    // 화면 전환용 검정 패널 (알파 0~1)
    public Image           cutscene;     // 컷씬 이미지 (DialogueLine.image가 여기에 표시)

    // ── 대사 데이터 ─────────────────────────────────────────────────────
    // Inspector에서 각 줄마다 텍스트·이미지·효과음을 설정한다.
    [System.Serializable]
    public class DialogueLine
    {
        public string    text;       // 표시할 대사 문자열
        public Sprite    image;      // 이 줄과 함께 보여줄 컷씬 이미지 (null이면 유지)
        public bool      clearImage; // true이면 현재 이미지를 페이드아웃으로 지움
        public AudioClip sfx;        // 이 줄 시작 시 재생할 효과음 (없으면 무음)
    }
    public DialogueLine[] lines; // 인트로 대사 배열 (순서대로 재생)

    // ── 총 획득 NPC ─────────────────────────────────────────────────────
    // 우진(김우진) NPC. 튜토리얼 완료 후 게임플레이에서 다시 등장한다.
    // gunNPCDead 플래그가 없을 때만 활성화된다.
    [Header("총 획득 NPC")]
    public GameObject gunNPC;

    // ── UI 참조 ─────────────────────────────────────────────────────────
    // Inspector에서 연결하지 않으면 Start()에서 씬 탐색으로 자동 할당된다.
    [Header("UI 참조 (인스펙터에서 연결)")]
    [SerializeField] private WeaponSlotUI    weaponSlotUI;
    [SerializeField] private HotbarManager   hotbarManager;

    // ── 개발용 스킵 옵션 ────────────────────────────────────────────────
    // 에디터 테스트 시 인트로·튜토리얼을 건너뛰고 게임플레이로 바로 진입.
    // skipWithGun: true이면 처음부터 총을 소지한 상태로 시작.
    [Header("개발용 - 인트로/튜토리얼 스킵")]
    [SerializeField] private bool skipIntroTutorial = false;
    [SerializeField] private bool skipWithGun = false;

    // ── 인트로 내부 상태 변수 ────────────────────────────────────────────
    private int   currentLine  = 0;     // 현재 재생 중인 대사 인덱스
    private bool  isTyping     = false; // 타이핑 애니메이션 진행 중 여부
    private bool  skipTyping   = false; // true이면 타이핑을 즉시 완료 (스페이스로 스킵)
    private bool  canClick     = false; // true일 때만 스페이스로 다음 줄 진행 가능
    private bool  introActive  = true;  // false이면 Update의 입력 처리를 완전히 무시

    // GunShotEvent가 이미 실행 중일 때 중복 호출을 막는 잠금 플래그
    private bool _gunShotPlaying = false;

    // ===================================================================
    // 초기화
    // ===================================================================

    void Start()
    {
        // UI 참조가 Inspector에서 연결되지 않은 경우 씬 내에서 자동 탐색
        if (weaponSlotUI  == null) weaponSlotUI  = FindFirstObjectByType<WeaponSlotUI>();
        if (hotbarManager == null) hotbarManager = FindFirstObjectByType<HotbarManager>();

        // 씬 시작 시 플레이어 이동·전투를 잠그고 UI를 숨겨 인트로 상태로 초기화
        SetPlayerControl(false);
        weaponSlotUI?.Hide();
        hotbarManager?.Hide();

        // GameManager가 없는 경우(에디터 직접 실행 등) 게임플레이로 바로 진입
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GameFlowManager] GameManager 없음 - 게임플레이로 바로 진입");
            SwitchToGameplay();
            return;
        }

        // ── 복귀 분기: tutorialDone 플래그가 있으면 튜토리얼까지 마친 상태 ──
        // 다른 씬에서 이 씬으로 돌아왔을 때 인트로·튜토리얼을 다시 재생하지 않는다.
        if (GameManager.Instance.HasFlag("tutorialDone"))
        {
            introRoot?.SetActive(false);
            tutorialRoot?.SetActive(false);
            gameplayRoot?.SetActive(true);

            // 우진 NPC는 열쇠를 받은 적 있고 아직 죽지 않은 경우에만 표시
            bool gunNPCAlive = GameManager.Instance.hasRightCorridorKey
                            && !GameManager.Instance.HasFlag("gunNPCDead");
            gunNPC?.SetActive(gunNPCAlive);

            SetPlayerControl(true);
            weaponSlotUI?.Show();
            hotbarManager?.Show();
            UICanvas.Instance?.ShowUI();
            AudioManager.Instance?.PlayBGM("prologue");
            return;
        }

        // ── 복귀 분기: introDone 플래그가 있으면 인트로만 마친 상태 ──
        // 튜토리얼은 다시 재생해야 하므로 tutorialRoot만 활성화한다.
        if (GameManager.Instance.HasFlag("introDone"))
        {
            introRoot?.SetActive(false);
            tutorialRoot?.SetActive(true);
            gameplayRoot?.SetActive(false);
            SetPlayerControl(true);
            weaponSlotUI?.Show();
            hotbarManager?.Show();
            AudioManager.Instance?.PlayBGM("prologue");
            return;
        }

        // ── 에디터 디버그 스킵 ──
        // skipIntroTutorial이 체크된 경우 인트로·튜토리얼을 건너뛰고 게임플레이 진입
        if (skipIntroTutorial)
        {
            SwitchToGameplay();
            GameManager.Instance.stage = 1;
            GameManager.Instance.hasPipe = true;
            GameManager.Instance.hasRightCorridorKey = true;

            if (skipWithGun)
            {
                // 총 소지 상태로 시작: PlayerCombat 무기도 총(1)으로 전환
                GameManager.Instance.hasGun = true;
                GameObject player = GameObject.FindWithTag("Player");
                player?.GetComponent<PlayerCombat>()?.SwitchWeapon(1);
                gunNPC?.SetActive(false);
                GameManager.Instance.SetFlag("gunNPCDead");
            }
            else
            {
                // 총 없이 시작: gunNPC가 아직 살아 있으면 표시
                bool gunNPCAlive = !GameManager.Instance.HasFlag("gunNPCDead");
                gunNPC?.SetActive(gunNPCAlive);
            }

            AudioManager.Instance?.PlayBGM("prologue");
            return;
        }

        // ── 최초 시작: 인트로 컷씬 재생 ──
        // introRoot만 활성화하고 나머지는 숨긴 뒤 페이드인 후 첫 대사를 시작
        introRoot?.SetActive(true);
        tutorialRoot?.SetActive(false);
        gameplayRoot?.SetActive(false);
        UICanvas.Instance?.HideUI();
        AudioManager.Instance?.StopBGM();
        Fogeffect.Instance?.SetFogActive(false); // 인트로 컷씬 중 포그 비활성화

        if (cutscene != null) cutscene.color = new Color(1, 1, 1, 0); // 컷씬 이미지를 투명하게 초기화
        if (clickHint != null) clickHint.SetActive(false);
        StartCoroutine(FadeIn());
    }

    // ===================================================================
    // 입력 처리 (인트로 전용)
    // ===================================================================

    void Update()
    {
        // 인트로가 끝났거나 TransitionToTutorial이 시작되면 입력을 무시
        if (!introActive) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
                // 타이핑 중: 스킵 플래그를 세워 TypeLine이 즉시 전체 텍스트를 표시하도록 함
                skipTyping = true;
            else if (canClick)
                // 타이핑 완료 후 클릭 대기 중: 다음 줄로 진행
                NextLine();
        }
    }

    // ===================================================================
    // 플레이어 조작 잠금/해제
    // ===================================================================

    /// <summary>
    /// 플레이어의 이동(PlayerMovement)과 전투(PlayerCombat)를 동시에 활성화/비활성화한다.
    /// 인트로·GunShotEvent·튜토리얼→게임플레이 전환 시 호출된다.
    /// </summary>
    void SetPlayerControl(bool enabled)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        // 이동 컴포넌트: disabled이면 Update가 실행되지 않아 키 입력이 무시됨
        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = enabled;

        // 전투 컴포넌트: disabled이면 마우스 클릭 공격·무기 전환이 불가능해짐
        var combat = player.GetComponent<PlayerCombat>();
        if (combat != null) combat.enabled = enabled;
    }

    // ===================================================================
    // 인트로 컷씬 코루틴
    // ===================================================================

    /// <summary>
    /// 씬 시작 시 화면이 검정에서 밝아지는 페이드인.
    /// 완료 후 첫 번째 대사 줄을 타이핑 시작한다.
    /// </summary>
    IEnumerator FadeIn()
    {
        float t = 1f;
        while (t > 0) { t -= Time.deltaTime; fadePanel.color = new Color(0,0,0,t); yield return null; }
        fadePanel.color = new Color(0,0,0,0);
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(TypeLine(lines[currentLine]));
    }

    /// <summary>
    /// 한 줄의 대사를 한 글자씩 타이핑 효과로 표시한다.
    /// - line.clearImage: 현재 컷씬 이미지를 페이드아웃으로 지운다.
    /// - line.image:      새 이미지를 페이드인으로 전환한다.
    /// - line.sfx:        대사 시작 시 효과음을 재생한다.
    /// 타이핑 완료 후 canClick = true로 설정해 스페이스 입력을 받는다.
    /// </summary>
    IEnumerator TypeLine(DialogueLine line)
    {
        isTyping   = true;
        skipTyping = false;
        canClick   = false;
        clickHint.SetActive(false);
        dialogueText.text = "";

        // 효과음이 지정된 경우 대사 시작 직전에 재생
        if (line.sfx != null)
            AudioManager.Instance?.PlaySFX(line.sfx);

        // 이미지 처리: clearImage이면 현재 이미지를 지우고, image가 있으면 교체
        if (line.clearImage)
            yield return StartCoroutine(FadeImage(cutscene, 0f));
        else if (line.image != null)
        {
            yield return StartCoroutine(FadeImage(cutscene, 0f)); // 이전 이미지 페이드아웃
            cutscene.sprite = line.image;
            yield return StartCoroutine(FadeImage(cutscene, 1f)); // 새 이미지 페이드인
        }

        // 한 글자씩 타이핑. skipTyping이 true가 되면 루프를 즉시 탈출
        foreach (char c in line.text)
        {
            if (skipTyping) break;
            dialogueText.text += c;
            yield return new WaitForSeconds(0.07f);
        }
        // 스킵했을 때도 전체 텍스트가 표시되도록 보장
        dialogueText.text = line.text;

        isTyping = false;
        canClick = true;
        clickHint.SetActive(true); // "스페이스바" 힌트 표시
    }

    /// <summary>
    /// Image 컴포넌트의 알파값을 targetAlpha까지 선형으로 페이드한다.
    /// speed: Time.deltaTime * 2f (약 0.5초 소요)
    /// </summary>
    IEnumerator FadeImage(Image img, float targetAlpha)
    {
        float start = img.color.a;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            img.color = new Color(1,1,1, Mathf.Lerp(start, targetAlpha, t));
            yield return null;
        }
    }

    /// <summary>
    /// 현재 대사 인덱스를 1 증가시킨다.
    /// 마지막 줄이면 TransitionToTutorial 코루틴으로 전환한다.
    /// </summary>
    void NextLine()
    {
        currentLine++;
        if (currentLine >= lines.Length)
        { StartCoroutine(TransitionToTutorial()); return; }
        StartCoroutine(TypeLine(lines[currentLine]));
    }

    /// <summary>
    /// 인트로 → 튜토리얼 전환 코루틴.
    /// 페이드아웃 → introRoot 비활성 + tutorialRoot 활성 → 페이드인 순으로 진행.
    /// 완료 후 "introDone" 플래그를 세우고 플레이어 조작을 해제한다.
    /// </summary>
    IEnumerator TransitionToTutorial()
    {
        canClick    = false;
        introActive = false; // Update의 스페이스바 입력 처리를 완전히 중단
        clickHint.SetActive(false);

        // 페이드 아웃 (화면이 검정으로)
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime; fadePanel.color = new Color(0,0,0,t); yield return null; }

        introRoot?.SetActive(false);
        tutorialRoot?.SetActive(true);
        SceneLoader.ClearPendingSpawn(); // 씬 전환 시 남은 스폰 좌표 데이터 초기화

        // 페이드 인 (화면이 다시 밝아짐)
        t = 1f;
        while (t > 0f) { t -= Time.deltaTime; fadePanel.color = new Color(0,0,0,t); yield return null; }
        fadePanel.color = new Color(0,0,0,0);

        // 인트로 완료 플래그 저장 (씬 재진입 시 인트로를 건너뜀)
        GameManager.Instance?.SetFlag("introDone");
        Fogeffect.Instance?.SetFogActive(true); // 인트로 종료 → 포그 복원
        AudioManager.Instance?.PlayBGM("prologue");
        hotbarManager?.Show();
        weaponSlotUI?.Show();
        SetPlayerControl(true); // 플레이어 이동·전투 해제
    }

    // ===================================================================
    // 튜토리얼 이벤트 콜백 (Interactable.onComplete에서 호출됨)
    // ===================================================================

    /// <summary>
    /// 시계 오브젝트 상호작용 완료 시 호출.
    /// "ClockEnd" 플래그를 세워 이후 이벤트 분기에서 시계를 완료한 것으로 판단한다.
    /// </summary>
    public void OnClockDone()
    {
        GameManager.Instance?.SetFlag("ClockEnd");
    }

    /// <summary>
    /// 파이프 NPC 상호작용 완료 시 호출.
    /// GameManager.hasPipe = true로 설정해 PlayerCombat의 근접 공격을 허용한다.
    /// </summary>
    public void GivePipe()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.hasPipe = true;
    }

    /// <summary>
    /// 튜토리얼 완료 NPC 상호작용 완료 시 호출.
    /// stage를 1로 올려 게임이 본격적으로 시작됐음을 표시하고 게임플레이로 전환한다.
    /// </summary>
    public void OnNPCDone()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.stage = 1; // 0 = 튜토리얼 전, 1 = 게임 진행 중
        StartCoroutine(NPCFadeOutAndGameplay());
    }

    /// <summary>
    /// 튜토리얼 → 게임플레이 전환 코루틴.
    /// 페이드아웃 → tutorialRoot 비활성 + gameplayRoot 활성 → 페이드인 순으로 진행.
    /// "tutorialDone" 플래그를 세워 씬 재진입 시 튜토리얼을 건너뛴다.
    /// </summary>
    IEnumerator NPCFadeOutAndGameplay()
    {
        Time.timeScale = 1f; // 혹시 인벤토리 등으로 timeScale이 0이 됐을 경우를 대비

        // 페이드 아웃
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime; fadePanel.color = new Color(0,0,0,t); yield return null; }

        tutorialRoot?.SetActive(false);
        gameplayRoot?.SetActive(true);
        UICanvas.Instance?.ShowUI(); // HP바 등 게임플레이 UI 표시
        GameManager.Instance?.SetFlag("tutorialDone");

        yield return new WaitForSeconds(0.3f);

        // 페이드 인
        t = 1f;
        while (t > 0f) { t -= Time.deltaTime; fadePanel.color = new Color(0,0,0,t); yield return null; }
        fadePanel.color = new Color(0,0,0,0);

        // 플레이어 조작 해제 및 HUD 표시
        SetPlayerControl(true);
        weaponSlotUI?.Show();
        hotbarManager?.Show();
    }

    // ===================================================================
    // 총 획득 이벤트
    // ===================================================================

    /// <summary>
    /// 우진 NPC로부터 총을 받았을 때 호출.
    /// GameManager.hasGun = true 설정 후 PlayerCombat의 무기를 총(1)으로 전환한다.
    /// </summary>
    public void OnGunNPCDone()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.hasGun = true;

        // 플레이어의 PlayerCombat에서 무기 전환 (0=파이프, 1=총)
        GameObject player = GameObject.FindWithTag("Player");
        player?.GetComponent<PlayerCombat>()?.SwitchWeapon(1);
    }

    /// <summary>
    /// 플레이어가 우진 NPC를 공격했을 때 호출.
    /// 중복 실행 방지(_gunShotPlaying)와 이미 사망 처리된 경우를 걸러낸 뒤 GunShotEvent를 시작한다.
    /// </summary>
    public void OnGunNPCHit()
    {
        if (_gunShotPlaying) return;
        if (GameManager.Instance != null && GameManager.Instance.HasFlag("gunNPCDead")) return;
        StartCoroutine(GunShotEvent());
    }

    /// <summary>
    /// 우진 NPC 사망 연출 코루틴.
    /// 순서: 화면 붉어짐 → 검정 전환 (사망 트리거) → 페이드인 → 사망 애니메이션 대기 → 플래그 설정.
    /// KKilled = true로 설정해 엔딩 분기에 반영한다.
    /// </summary>
    IEnumerator GunShotEvent()
    {
        _gunShotPlaying = true;
        Time.timeScale = 1f;
        SetPlayerControl(false); // 연출 중 플레이어 조작 금지

        // 1단계: 화면이 붉게 물들어 총격 피격감을 연출
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            fadePanel.color = new Color(1,0,0, Mathf.Lerp(0, 0.6f, t));
            yield return null;
        }
        yield return new WaitForSeconds(0.3f);

        // 2단계: 붉은 화면에서 검정으로 전환 (암전)
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            fadePanel.color = new Color(0,0,0, Mathf.Lerp(0,1f,t));
            yield return null;
        }

        // 암전 중에 사망 애니메이션 트리거 (화면이 안 보이는 동안 처리)
        Animator animComp = gunNPC?.GetComponent<Animator>();
        if (animComp != null) animComp.SetTrigger("Die");

        // 3단계: 페이드 인 (사망 후 장면 공개)
        t = 1f;
        while (t > 0f) { t -= Time.deltaTime; fadePanel.color = new Color(0,0,0,t); yield return null; }
        fadePanel.color = new Color(0,0,0,0);

        // 4단계: "Die" 애니메이션이 완전히 끝날 때까지 대기 후 Animator 비활성화
        if (animComp != null)
        {
            yield return null; // 트리거가 반영될 때까지 한 프레임 대기
            while (animComp.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f
                   && animComp.GetCurrentAnimatorStateInfo(0).IsName("Die"))
                yield return null;
            animComp.enabled = false; // 마지막 프레임을 고정 (다시 Idle로 돌아가지 않도록)
        }

        // 플래그 및 엔딩 분기 플래그 저장
        GameManager.Instance?.SetFlag("gunNPCDead"); // 씬 재진입 시 NPC를 비활성화하는 데 사용
        _gunShotPlaying = false;
        GameManager.Instance.KKilled = true; // 엔딩 비트 K(우진) = 죽음
        SetPlayerControl(true); // 조작 해제
    }

    // ===================================================================
    // 내부 유틸
    // ===================================================================

    /// <summary>
    /// 인트로·튜토리얼 없이 바로 게임플레이 루트로 전환한다.
    /// skipIntroTutorial 옵션 또는 GameManager 없을 때 사용된다.
    /// </summary>
    void SwitchToGameplay()
    {
        introRoot?.SetActive(false);
        tutorialRoot?.SetActive(false);
        gameplayRoot?.SetActive(true);
        SetPlayerControl(true);
        weaponSlotUI?.Show();
        hotbarManager?.Show();
        UICanvas.Instance?.ShowUI();
    }
}
