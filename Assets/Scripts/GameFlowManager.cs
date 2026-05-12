using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// GameLap 씬 안에서 인트로 컷씬 → 튜토리얼 → 게임플레이 흐름을 관리하는 컴포넌트.
// 기존 IntroDialogue / TutorialManager / PipePickup 을 하나로 통합.
//
// [씬 구조]
// introRoot   : 인트로 컷씬 UI/오브젝트 그룹
// tutorialRoot: 튜토리얼 오브젝트 그룹 (시계, NPC, 파이프 등)
// gameplayRoot: 실제 게임플레이 오브젝트 그룹 (맵, 몬스터 등)
//
// [게임 흐름]
// 인트로(Space로 대사 진행) → TransitionToTutorial → 튜토리얼 이벤트
// → OnNPCDone → TransitionToGameplay → 이후 DoorTrigger로 씬 이동 가능
//
// [씬 복귀 처리]
// DoorTrigger로 다른 씬 갔다가 돌아올 때 GameManager 플래그로 상태 복원:
//   "tutorialDone" → 게임플레이 상태 복원
//   "introDone"    → 튜토리얼 상태 복원
public class GameFlowManager : MonoBehaviour
{
    [Header("단계별 루트 오브젝트")]
    public GameObject introRoot;      // 인트로 컷씬 오브젝트 그룹
    public GameObject tutorialRoot;   // 튜토리얼 오브젝트 그룹
    public GameObject gameplayRoot;   // 실제 게임플레이 오브젝트 그룹

    [Header("인트로 UI")]
    public TextMeshProUGUI dialogueText; // 대사 텍스트
    public GameObject clickHint;         // "Space를 눌러 계속" 힌트 UI
    public Image fadePanel;              // 페이드 인/아웃용 검정 패널
    public Image cutscene;               // 컷씬 이미지

    // 인트로 대사 한 줄과 표시할 이미지 정보
    [System.Serializable]
    public class DialogueLine
    {
        public string text;       // 대사 내용
        public Sprite image;      // null이면 이미지 변경 없음
        public bool clearImage;   // true면 현재 이미지 페이드 아웃
    }
    public DialogueLine[] lines; // 인트로 대사 배열

    [Header("총 획득 이벤트")]
    public GameObject gunNPC; // 총을 주는 NPC (조건 충족 시 활성화)

    [Header("개발용 - 인트로/튜토리얼 스킵")]
    [SerializeField] private bool skipIntroTutorial = false;

    // 인트로 상태 변수
    private int currentLine = 0;     // 현재 표시 중인 대사 인덱스
    private bool isTyping = false;   // 타이핑 연출 진행 중 여부
    private bool canClick = false;   // Space 입력으로 다음 줄 가능 여부
    private bool introActive = true; // false가 되면 Update 입력 무시

    // Space 연타 방지 쿨다운
    private float lastSpaceTime = -1f;
    private float spaceCooldown = 0.3f;

    void Start()
    {
        // 인트로 시작 전 플레이어 조작 비활성화
        SetPlayerControl(false);
        WeaponSlotUI.Instance?.Hide();
        HotbarManager.Instance?.Hide();

        // GameManager 없으면 씬 직접 실행으로 간주 → 게임플레이로 바로 진입
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GameFlowManager] GameManager 없음 - 게임플레이로 바로 진입");
            introRoot?.SetActive(false);
            tutorialRoot?.SetActive(false);
            gameplayRoot?.SetActive(true);
            SetPlayerControl(true);
            WeaponSlotUI.Instance?.Show();
            HotbarManager.Instance?.Show();
            UICanvas.Instance?.ShowUI();
            return;
        }

        // 튜토리얼까지 완료한 상태로 씬에 복귀한 경우
        if (GameManager.Instance.HasFlag("tutorialDone"))
        {
            introRoot?.SetActive(false);
            tutorialRoot?.SetActive(false);
            gameplayRoot?.SetActive(true);

            // 열쇠 있고 NPC 아직 안 죽었으면 gunNPC 활성화
            if (GameManager.Instance.hasRightCorridorKey
                && !GameManager.Instance.HasFlag("gunNPCDead"))
                gunNPC?.SetActive(true);
            else
                gunNPC?.SetActive(false);

            SetPlayerControl(true);
            WeaponSlotUI.Instance?.Show();
            HotbarManager.Instance?.Show();
            UICanvas.Instance?.ShowUI();
            AudioManager.Instance?.PlayBGM("prologue");
            return;
        }
    

        // 인트로만 완료한 상태로 복귀한 경우
        if (GameManager.Instance.HasFlag("introDone"))
        {
            introRoot?.SetActive(false);
            tutorialRoot?.SetActive(true);
            gameplayRoot?.SetActive(false);
            SetPlayerControl(true);
            WeaponSlotUI.Instance?.Show();
            HotbarManager.Instance?.Show();
            AudioManager.Instance?.PlayBGM("prologue");
            return;
        }

        // 에디터 디버그용 스킵
        if (skipIntroTutorial)
        {
            introRoot?.SetActive(false);
            tutorialRoot?.SetActive(false);
            gameplayRoot?.SetActive(true);
            SetPlayerControl(true);
            WeaponSlotUI.Instance?.Show();
            HotbarManager.Instance?.Show();
            UICanvas.Instance?.ShowUI();
            GameManager.Instance.stage = 1;
            GameManager.Instance.hasPipe = true; // 스킵 시 파이프 자동 지급
            AudioManager.Instance?.PlayBGM("prologue");
            return;
        }

        // 처음 시작: 인트로 재생
        introRoot?.SetActive(true);
        tutorialRoot?.SetActive(false);
        gameplayRoot?.SetActive(false);
        UICanvas.Instance?.HideUI();
        AudioManager.Instance?.StopBGM();

        cutscene.color = new Color(1, 1, 1, 0);
        clickHint.SetActive(false);
        StartCoroutine(FadeIn());
    }

    void Update()
    {
        // 인트로 단계에서만 Space 입력 처리
        if (!introActive) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Space 연타 방지
            if (Time.unscaledTime - lastSpaceTime < spaceCooldown) return;
            lastSpaceTime = Time.unscaledTime;

            if (isTyping)
            {
                // 타이핑 중 Space: 현재 줄 즉시 완성
                StopAllCoroutines();
                dialogueText.text = lines[currentLine].text;
                isTyping = false;
                canClick = true;
                clickHint.SetActive(true);
            }
            else if (canClick)
            {
                NextLine();
            }
        }
    }

    // ── 플레이어 조작 잠금/해제 ─────────────────────────────────────────

    // PlayerMovement 컴포넌트를 활성/비활성화해 조작을 잠그거나 해제
    void SetPlayerControl(bool enabled)
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) return;
        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = enabled;
    }

    // ── 인트로 컷씬 ─────────────────────────────────────────────────────

    // 씬 시작 시 검정 화면을 페이드 인 후 첫 대사 시작
    IEnumerator FadeIn()
    {
        float t = 1f;
        while (t > 0)
        {
            t -= Time.deltaTime;
            fadePanel.color = new Color(0, 0, 0, t);
            yield return null;
        }
        fadePanel.color = new Color(0, 0, 0, 0);
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(TypeLine(lines[currentLine]));
    }

    // 이미지 전환 후 한 글자씩 타이핑 연출
    IEnumerator TypeLine(DialogueLine line)
    {
        isTyping = true;
        canClick = false;
        clickHint.SetActive(false);
        dialogueText.text = "";

        // 이미지 전환 처리
        if (line.clearImage)
        {
            yield return StartCoroutine(FadeImage(cutscene, 0f));
        }
        else if (line.image != null)
        {
            yield return StartCoroutine(FadeImage(cutscene, 0f)); // 기존 이미지 페이드 아웃
            cutscene.sprite = line.image;
            yield return StartCoroutine(FadeImage(cutscene, 1f)); // 새 이미지 페이드 인
        }

        // 한 글자씩 출력
        foreach (char c in line.text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.07f);
        }

        isTyping = false;
        canClick = true;
        clickHint.SetActive(true);
    }

    // 이미지를 지정 알파값으로 부드럽게 전환
    IEnumerator FadeImage(Image img, float targetAlpha)
    {
        float start = img.color.a;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            img.color = new Color(1, 1, 1, Mathf.Lerp(start, targetAlpha, t));
            yield return null;
        }
    }

    // 다음 줄로 이동. 마지막 줄이면 튜토리얼로 전환
    void NextLine()
    {
        currentLine++;
        if (currentLine >= lines.Length)
        {
            StartCoroutine(TransitionToTutorial());
            return;
        }
        StartCoroutine(TypeLine(lines[currentLine]));
    }

    // 인트로 종료: 페이드 아웃 후 tutorialRoot 활성화
    IEnumerator TransitionToTutorial()
    {
        canClick = false;
        introActive = false; // 이후 Update 입력 차단
        clickHint.SetActive(false);

        // 페이드 아웃
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            fadePanel.color = new Color(0, 0, 0, t);
            yield return null;
        }

        introRoot?.SetActive(false);
        tutorialRoot?.SetActive(true);

        // 튜토리얼 진입 시 SpawnPos 초기화 (DoorTrigger 잔여값 방지)
        PlayerPrefs.DeleteKey("SpawnX");
        PlayerPrefs.DeleteKey("SpawnY");
        PlayerPrefs.DeleteKey("SpawnDirX");
        PlayerPrefs.DeleteKey("SpawnDirY");

        // 페이드 인
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            fadePanel.color = new Color(0, 0, 0, t);
            yield return null;
        }
        fadePanel.color = new Color(0, 0, 0, 0);

        // 인트로 완료 플래그 저장 (씬 복귀 시 상태 복원에 사용)
        GameManager.Instance?.SetFlag("introDone");
        AudioManager.Instance?.PlayBGM("prologue");
        HotbarManager.Instance?.Show();
        WeaponSlotUI.Instance?.Show();
        SetPlayerControl(true);
    }

    // ── 튜토리얼 이벤트 (Interactable.onComplete에 연결) ────────────────

    // 시계 오브젝트 상호작용 완료
    public void OnClockDone()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.SetFlag("ClockEnd");
    }

    // 파이프 아이템 획득 (기존 PipePickup.GivePipe 대체)
    public void GivePipe()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.hasPipe = true;
    }

    // NPC 대화 완료 → 페이드 후 게임플레이로 전환
    public void OnNPCDone()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.stage = 1;
        StartCoroutine(NPCFadeOutAndGameplay());
    }

    // 튜토리얼 NPC 페이드 아웃 후 gameplayRoot 활성화, HUD 표시
    IEnumerator NPCFadeOutAndGameplay()
    {
        // 대화 끝나고 timeScale 복구 먼저
        Time.timeScale = 1f;

        // 페이드 아웃
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            fadePanel.color = new Color(0, 0, 0, t);
            yield return null;
        }

        tutorialRoot?.SetActive(false);
        gameplayRoot?.SetActive(true);
        UICanvas.Instance?.ShowUI();
        GameManager.Instance?.SetFlag("tutorialDone");

        yield return new WaitForSeconds(0.3f);

        // 페이드 인
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            fadePanel.color = new Color(0, 0, 0, t);
            yield return null;
        }
        fadePanel.color = new Color(0, 0, 0, 0);

        SetPlayerControl(true);
        WeaponSlotUI.Instance?.Show();
        HotbarManager.Instance?.Show();
    }

    // ── 총 획득 이벤트 ──────────────────────────────────────────────────

    // gunNPC 1차 대화 완료 시 호출: 총 지급 및 무기 전환
    public void OnGunNPCDone()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.hasGun = true;

        // 플레이어 무기를 권총(1)으로 전환
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var pm = player.GetComponent<PlayerMovement>();
            pm?.SwitchWeapon(1);
        }
    }

    // gunNPC 2차 대화 완료 시 호출: 총 쏘는 연출 시작
    public void OnGunNPCShot()
    {
        StartCoroutine(GunShotEvent());
    }
    // 총알에 NPC 피격 시 호출
    public void OnGunNPCHit()
    {
        if (GameManager.Instance.HasFlag("gunNPCDead")) return;
        StartCoroutine(GunShotEvent());
    }
    // 총 쏘는 연출: 화면 붉어짐 → 검정 → NPC 사망 애니메이션 → 우는 대사
    IEnumerator GunShotEvent()
    {
        Time.timeScale = 1f;

        // 화면 붉어지기
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            fadePanel.color = new Color(1, 0, 0, Mathf.Lerp(0, 0.6f, t));
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        // 검정으로 전환
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            fadePanel.color = new Color(0, 0, 0, Mathf.Lerp(0, 1f, t));
            yield return null;
        }

        // NPC 사망 애니메이션 재생
        var anim = gunNPC?.GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Die");

        // 페이드 인
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            fadePanel.color = new Color(0, 0, 0, t);
            yield return null;
        }
        fadePanel.color = new Color(0, 0, 0, 0);

        // 애니메이션 완료 대기
        if (anim != null)
        {
            yield return null;
            while (anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f
                   && anim.GetCurrentAnimatorStateInfo(0).IsName("Die"))
            {
                yield return null;
            }
            anim.enabled = false; // 마지막 프레임 고정
        }

        // 시체 남기고 플래그 세팅
        GameManager.Instance?.SetFlag("gunNPCDead");
    }

}