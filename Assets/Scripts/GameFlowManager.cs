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
    [Header("단계별 루트 오브젝트")]
    public GameObject introRoot;
    public GameObject tutorialRoot;
    public GameObject gameplayRoot;

    [Header("인트로 UI")]
    public TextMeshProUGUI dialogueText;
    public GameObject      clickHint;
    public Image           fadePanel;
    public Image           cutscene;

    [System.Serializable]
    public class DialogueLine
    {
        public string    text;
        public Sprite    image;
        public bool      clearImage;
        public AudioClip sfx;        // 이 줄 시작 시 재생할 효과음 (없으면 무음)
    }
    public DialogueLine[] lines;

    [Header("총 획득 NPC")]
    public GameObject gunNPC;

    [Header("UI 참조 (Inspector에서 연결)")]
    [SerializeField] private WeaponSlotUI    weaponSlotUI;
    [SerializeField] private HotbarManager   hotbarManager;

    [Header("개발용 - 인트로/튜토리얼 스킵")]
    [SerializeField] private bool skipIntroTutorial = false;
    [SerializeField] private bool skipWithGun = false;

    // ── 인트로 상태 ──────────────────────────────────────────────────
    private int   currentLine  = 0;
    private bool  isTyping     = false;
    private bool  skipTyping   = false;
    private bool  canClick     = false;
    private bool  introActive  = true;

    // GunShotEvent 중복 실행 방지 락
    private bool _gunShotPlaying = false;

    void Start()
    {
        // UI 참조가 없으면 씬에서 자동 탐색
        if (weaponSlotUI  == null) weaponSlotUI  = FindFirstObjectByType<WeaponSlotUI>();
        if (hotbarManager == null) hotbarManager = FindFirstObjectByType<HotbarManager>();

        SetPlayerControl(false);
        weaponSlotUI?.Hide();
        hotbarManager?.Hide();

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GameFlowManager] GameManager 없음 - 게임플레이로 바로 진입");
            SwitchToGameplay();
            return;
        }

        // 튜토리얼까지 완료 후 복귀
        if (GameManager.Instance.HasFlag("tutorialDone"))
        {
            introRoot?.SetActive(false);
            tutorialRoot?.SetActive(false);
            gameplayRoot?.SetActive(true);

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

        // 인트로만 완료 후 복귀
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

        // 에디터 디버그 스킵
        if (skipIntroTutorial)
        {
            SwitchToGameplay();
            GameManager.Instance.stage = 1;
            GameManager.Instance.hasPipe = true;
            GameManager.Instance.hasRightCorridorKey = true;

            if (skipWithGun)
            {
                GameManager.Instance.hasGun = true;
                GameObject player = GameObject.FindWithTag("Player");
                player?.GetComponent<PlayerCombat>()?.SwitchWeapon(1);
                gunNPC?.SetActive(false);
                GameManager.Instance.SetFlag("gunNPCDead");
            }
            else
            {
                bool gunNPCAlive = !GameManager.Instance.HasFlag("gunNPCDead");
                gunNPC?.SetActive(gunNPCAlive);
            }

            AudioManager.Instance?.PlayBGM("prologue");
            return;
        }

        // 처음 시작: 인트로 재생
        introRoot?.SetActive(true);
        tutorialRoot?.SetActive(false);
        gameplayRoot?.SetActive(false);
        UICanvas.Instance?.HideUI();
        AudioManager.Instance?.StopBGM();

        if (cutscene != null) cutscene.color = new Color(1, 1, 1, 0);
        if (clickHint != null) clickHint.SetActive(false);
        StartCoroutine(FadeIn());
    }

    void Update()
    {
        if (!introActive) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
                skipTyping = true;
            else if (canClick)
                NextLine();
        }
    }

    // ── 플레이어 조작 잠금/해제 ──────────────────────────────────────

    void SetPlayerControl(bool enabled)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;
        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = enabled;
    }

    // ── 인트로 컷씬 ──────────────────────────────────────────────────

    IEnumerator FadeIn()
    {
        float t = 1f;
        while (t > 0) { t -= Time.deltaTime; fadePanel.color = new Color(0,0,0,t); yield return null; }
        fadePanel.color = new Color(0,0,0,0);
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(TypeLine(lines[currentLine]));
    }

    IEnumerator TypeLine(DialogueLine line)
    {
        isTyping   = true;
        skipTyping = false;
        canClick   = false;
        clickHint.SetActive(false);
        dialogueText.text = "";

        if (line.sfx != null)
            AudioManager.Instance?.PlaySFX(line.sfx);

        if (line.clearImage)
            yield return StartCoroutine(FadeImage(cutscene, 0f));
        else if (line.image != null)
        {
            yield return StartCoroutine(FadeImage(cutscene, 0f));
            cutscene.sprite = line.image;
            yield return StartCoroutine(FadeImage(cutscene, 1f));
        }

        foreach (char c in line.text)
        {
            if (skipTyping) break;
            dialogueText.text += c;
            yield return new WaitForSeconds(0.07f);
        }
        dialogueText.text = line.text; // 스킵 시에도 전체 텍스트 보장

        isTyping = false;
        canClick = true;
        clickHint.SetActive(true);
    }

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

    void NextLine()
    {
        currentLine++;
        if (currentLine >= lines.Length)
        { StartCoroutine(TransitionToTutorial()); return; }
        StartCoroutine(TypeLine(lines[currentLine]));
    }

    IEnumerator TransitionToTutorial()
    {
        canClick    = false;
        introActive = false;
        clickHint.SetActive(false);

        // 페이드 아웃
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime; fadePanel.color = new Color(0,0,0,t); yield return null; }

        introRoot?.SetActive(false);
        tutorialRoot?.SetActive(true);
        SceneLoader.ClearPendingSpawn(); // 잔여 스폰 데이터 초기화

        // 페이드 인
        t = 1f;
        while (t > 0f) { t -= Time.deltaTime; fadePanel.color = new Color(0,0,0,t); yield return null; }
        fadePanel.color = new Color(0,0,0,0);

        GameManager.Instance?.SetFlag("introDone");
        AudioManager.Instance?.PlayBGM("prologue");
        hotbarManager?.Show();
        weaponSlotUI?.Show();
        SetPlayerControl(true);
    }

    // ── 튜토리얼 이벤트 (Interactable.onComplete에 연결) ─────────────

    public void OnClockDone()
    {
        GameManager.Instance?.SetFlag("ClockEnd");
    }

    public void GivePipe()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.hasPipe = true;
    }

    public void OnNPCDone()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.stage = 1;
        StartCoroutine(NPCFadeOutAndGameplay());
    }

    IEnumerator NPCFadeOutAndGameplay()
    {
        Time.timeScale = 1f;

        float t = 0f;
        while (t < 1f) { t += Time.deltaTime; fadePanel.color = new Color(0,0,0,t); yield return null; }

        tutorialRoot?.SetActive(false);
        gameplayRoot?.SetActive(true);
        UICanvas.Instance?.ShowUI();
        GameManager.Instance?.SetFlag("tutorialDone");

        yield return new WaitForSeconds(0.3f);

        t = 1f;
        while (t > 0f) { t -= Time.deltaTime; fadePanel.color = new Color(0,0,0,t); yield return null; }
        fadePanel.color = new Color(0,0,0,0);

        SetPlayerControl(true);
        weaponSlotUI?.Show();
        hotbarManager?.Show();
    }

    // ── 총 획득 이벤트 ────────────────────────────────────────────────

    public void OnGunNPCDone()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.hasGun = true;

        // 플레이어의 PlayerCombat에서 무기 전환
        GameObject player = GameObject.FindWithTag("Player");
        player?.GetComponent<PlayerCombat>()?.SwitchWeapon(1);
    }

    public void OnGunNPCHit()
    {
        if (_gunShotPlaying) return;
        if (GameManager.Instance != null && GameManager.Instance.HasFlag("gunNPCDead")) return;
        StartCoroutine(GunShotEvent());
    }

    IEnumerator GunShotEvent()
    {
        _gunShotPlaying = true;
        Time.timeScale = 1f;
        SetPlayerControl(false);

        // 화면 붉어지기
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            fadePanel.color = new Color(1,0,0, Mathf.Lerp(0, 0.6f, t));
            yield return null;
        }
        yield return new WaitForSeconds(0.3f);

        // 검정으로 전환
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            fadePanel.color = new Color(0,0,0, Mathf.Lerp(0,1f,t));
            yield return null;
        }

        Animator animComp = gunNPC?.GetComponent<Animator>();
        if (animComp != null) animComp.SetTrigger("Die");

        // 페이드 인
        t = 1f;
        while (t > 0f) { t -= Time.deltaTime; fadePanel.color = new Color(0,0,0,t); yield return null; }
        fadePanel.color = new Color(0,0,0,0);

        // 사망 애니메이션 완료 대기
        if (animComp != null)
        {
            yield return null;
            while (animComp.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f
                   && animComp.GetCurrentAnimatorStateInfo(0).IsName("Die"))
                yield return null;
            animComp.enabled = false;
        }

        GameManager.Instance?.SetFlag("gunNPCDead");
        _gunShotPlaying = false;
        GameManager.Instance.KKilled = true;
        SetPlayerControl(true);
    }

    // ── 내부 유틸 ─────────────────────────────────────────────────────

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
