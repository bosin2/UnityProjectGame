using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 복도 씬 최초 진입 시 연출:
///   검정화면 → 카메라 팬(맵 끝 → 플레이어 스폰) → 독백 → 검정 페이드아웃 → 게임 시작
///
/// [씬 셋업 방법]
/// 1. 빈 GameObject "CorridorIntroManager" 를 씬에 추가, 이 컴포넌트 부착.
/// 2. UI Canvas (Screen Space Overlay) 아래에 다음 오브젝트 생성:
///    - FadePanel       : Image, 검정(0,0,0,1), stretch 전체, Raycast Target OFF
///    - MonologuePanel  : 화면 하단 대사창 배경
///      └ MonologueText : TextMeshPro
///      └ ClickHint     : "Space" 힌트 텍스트
/// 3. Inspector에서 각 참조 연결 + CameraStartPoint(빈 Transform) 배치.
/// </summary>
public class CorridorIntroManager : MonoBehaviour
{
    [Header("카메라 팬 시작 위치 (맵 끝)")]
    [Tooltip("맵 끝에 빈 GameObject를 놓고 Transform을 연결하세요.")]
    public Transform cameraStartPoint;

    [Header("카메라 팬")]
    public float panDuration = 2.5f;

    [Header("독백 대사")]
    [TextArea(2, 5)]
    public string[] monologueLines;

    [Tooltip("타이핑 속도 (초/글자)")]
    public float typingSpeed = 0.05f;

    [Header("UI 참조")]
    public Image           fadePanel;
    public GameObject      monologuePanel;
    public TextMeshProUGUI monologueText;
    public GameObject      clickHint;

    [Header("타이밍")]
    public float fadeDuration = 0.8f;

    // ── 내부 ──────────────────────────────────────────────────────────
    private CameraFollow cameraFollow;
    private bool introRunning;
    private bool introFinished;

    // 대화 입력 상태 (Update ↔ PlayMonologue 코루틴 공유)
    private bool dialogueActive = false;
    private bool isTyping       = false;
    private bool skipTyping     = false;
    private bool advanceLine    = false;
    private readonly List<MonsterPauseState> pausedMonsters = new List<MonsterPauseState>();

    private struct MonsterPauseState
    {
        public MonsterBase monster;
        public bool monsterEnabled;
        public Rigidbody2D rb;
        public bool rbSimulated;
        public Vector2 rbVelocity;
        public Animator animator;
        public float animatorSpeed;
    }

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
        ResetIntroUI();

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[CorridorIntro] GameManager가 없어서 스킵됨");
            enabled = false; return;
        }

        string flag = SceneManager.GetActiveScene().name + "_introDone";
        if (GameManager.Instance.HasFlag(flag))
        {
            Debug.Log("[CorridorIntro] 이미 방문한 씬 — 인트로 스킵");
            enabled = false; return;
        }

        StartCoroutine(IntroSequence(flag));
    }

    // ===================================================================
    // 메인 시퀀스
    // ===================================================================

    IEnumerator IntroSequence(string flag)
    {
        introRunning = true;
        introFinished = false;

        // 한 프레임 대기 — PlayerMovement 스폰 위치 설정 완료 보장
        yield return null;

        // 대사창 미리 숨김 (씬에서 활성화 상태여도 카메라 팬 중엔 보이지 않게)
        ResetIntroUI();

        // ── 1. 플레이어 잠금 + HUD 숨김 + 타이머 정지 ──
        TimerManager.Instance?.PauseTimer();
        SetPlayerControl(false);
        SetMonstersPaused(true);
        UICanvas.Instance?.HideUI();
        WeaponSlotUI.Instance?.Hide();
        HotbarManager.Instance?.Hide();

        // ── 2. CameraFollow 비활성화 + 포그 비활성화 → 카메라 직접 제어 ──
        cameraFollow = FindFirstObjectByType<CameraFollow>();
        if (cameraFollow != null) cameraFollow.enabled = false;
        Fogeffect.Instance?.SetFogActive(false);

        Camera cam = Camera.main;

        // ── 3. 카메라를 맵 끝 위치로 즉시 이동 ──
        if (cameraStartPoint != null && cam != null)
        {
            cam.transform.position = new Vector3(
                cameraStartPoint.position.x,
                cameraStartPoint.position.y,
                cam.transform.position.z);
        }

        // ── 4. 검정 → 씬 페이드인 ──
        if (fadePanel != null) fadePanel.color = new Color(0, 0, 0, 1f);
        yield return StartCoroutine(FadeTo(0f));
        yield return new WaitForSeconds(0.4f);

        // ── 5. 카메라 팬: 맵 끝 → 플레이어 스폰 위치 ──
        GameObject player = GameObject.FindWithTag("Player");
        if (cam != null && player != null)
        {
            Vector3 panStart = cam.transform.position;
            Vector3 panEnd   = new Vector3(
                player.transform.position.x,
                player.transform.position.y,
                cam.transform.position.z);

            float t = 0f;
            while (t < panDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / panDuration);
                cam.transform.position = Vector3.Lerp(panStart, panEnd, p);
                yield return null;
            }
            cam.transform.position = panEnd;
        }

        yield return new WaitForSeconds(0.3f);

        // ── 6. 독백 표시 ──
        yield return StartCoroutine(PlayMonologue());

        // ── 7. 씬 → 검정 페이드아웃 ──
        yield return StartCoroutine(FadeTo(1f));
        yield return new WaitForSeconds(0.2f);

        // ── 8. CameraFollow 재활성화 + 포그 재활성화 ──
        if (cameraFollow != null) cameraFollow.enabled = true;
        Fogeffect.Instance?.SetFogActive(true);

        // ── 9. 검정 → 씬 페이드인 (게임 시작) ──
        yield return StartCoroutine(FadeTo(0f));

        // ── 10. 잠금 해제 + HUD 복원 ──
        SetMonstersPaused(false);
        SetPlayerControl(true);
        UICanvas.Instance?.ShowUI();
        WeaponSlotUI.Instance?.Show();
        HotbarManager.Instance?.Show();
        TimerManager.Instance?.ResumeTimer();

        // 방문 완료 플래그
        GameManager.Instance.SetFlag(flag);
        introFinished = true;
        introRunning = false;
    }

    // ===================================================================
    // 독백 재생
    // ===================================================================

    IEnumerator PlayMonologue()
    {
        if (monologueLines == null || monologueLines.Length == 0)
        {
            Debug.LogWarning("[CorridorIntro] monologueLines가 비어있음 — Inspector에서 대사 입력 필요");
            yield break;
        }
        if (monologuePanel == null || monologueText == null)
        {
            Debug.LogWarning("[CorridorIntro] monologuePanel 또는 monologueText 참조가 없음 — Inspector 연결 확인");
            yield break;
        }
        Debug.Log("[CorridorIntro] 독백 시작");

        monologuePanel.SetActive(true);
        clickHint?.SetActive(false);
        dialogueActive = true;

        foreach (string line in monologueLines)
        {
            monologueText.text = "";
            isTyping   = true;
            skipTyping = false;

            foreach (char c in line)
            {
                if (skipTyping) break;
                monologueText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }
            monologueText.text = line; // 스킵 시에도 전체 텍스트 보장

            isTyping = false;
            clickHint?.SetActive(true);

            // Update()에서 Space 입력을 받을 때까지 대기
            advanceLine = false;
            while (!advanceLine)
                yield return null;

            clickHint?.SetActive(false);
        }

        dialogueActive = false;
        monologuePanel.SetActive(false);
    }

    // ===================================================================
    // 페이드
    // ===================================================================

    IEnumerator FadeTo(float targetAlpha)
    {
        if (fadePanel == null) yield break;

        float startAlpha = fadePanel.color.a;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadePanel.color = new Color(0, 0, 0,
                Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration));
            yield return null;
        }
        fadePanel.color = new Color(0, 0, 0, targetAlpha);
    }

    void ResetIntroUI()
    {
        monologuePanel?.SetActive(false);
        clickHint?.SetActive(false);
        if (monologueText != null) monologueText.text = "";
        if (fadePanel != null) fadePanel.color = new Color(0, 0, 0, 0f);
    }

    // ===================================================================
    // 플레이어 제어 잠금/해제
    // ===================================================================

    void SetPlayerControl(bool on)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = on;

        if (!on)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            var anim = player.GetComponent<Animator>()
                    ?? player.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsWalking", false);
                anim.SetFloat("DirX", 0f);
                anim.SetFloat("DirY", -1f);
            }
        }
    }

    void SetMonstersPaused(bool paused)
    {
        if (paused)
        {
            pausedMonsters.Clear();

            MonsterBase[] monsters = FindObjectsByType<MonsterBase>(FindObjectsSortMode.None);
            foreach (MonsterBase monster in monsters)
            {
                if (monster == null || monster.gameObject.scene != gameObject.scene)
                    continue;

                Rigidbody2D monsterRb = monster.GetComponent<Rigidbody2D>();
                Animator monsterAnimator = monster.GetComponent<Animator>()
                    ?? monster.GetComponentInChildren<Animator>();

                pausedMonsters.Add(new MonsterPauseState
                {
                    monster = monster,
                    monsterEnabled = monster.enabled,
                    rb = monsterRb,
                    rbSimulated = monsterRb == null || monsterRb.simulated,
                    rbVelocity = monsterRb != null ? monsterRb.linearVelocity : Vector2.zero,
                    animator = monsterAnimator,
                    animatorSpeed = monsterAnimator != null ? monsterAnimator.speed : 1f
                });

                if (monsterRb != null)
                {
                    monsterRb.linearVelocity = Vector2.zero;
                    monsterRb.angularVelocity = 0f;
                    monsterRb.simulated = false;
                }

                if (monsterAnimator != null)
                    monsterAnimator.speed = 0f;

                monster.enabled = false;
            }

            return;
        }

        foreach (MonsterPauseState state in pausedMonsters)
        {
            if (state.monster != null)
                state.monster.enabled = state.monsterEnabled;

            if (state.animator != null)
                state.animator.speed = state.animatorSpeed;

            if (state.rb != null)
            {
                state.rb.simulated = state.rbSimulated;
                if (state.rbSimulated)
                    state.rb.linearVelocity = state.rbVelocity;
            }
        }

        pausedMonsters.Clear();
    }

    void OnDisable()
    {
        if (!introRunning || introFinished) return;

        SetPlayerControl(true);
        SetMonstersPaused(false);
        if (cameraFollow != null) cameraFollow.enabled = true;
        Fogeffect.Instance?.SetFogActive(true);
        UICanvas.Instance?.ShowUI();
        WeaponSlotUI.Instance?.Show();
        HotbarManager.Instance?.Show();
        TimerManager.Instance?.ResumeTimer();
        dialogueActive = false;
        ResetIntroUI();
        introRunning = false;
    }
}
