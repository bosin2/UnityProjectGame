using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 플레이어 HP 관리 (피격, 회복, 사망).
/// [같은 GameObject] PlayerMovement(이동), PlayerCombat(공격)과 함께 부착.
/// HP 변화는 OnHPChanged 이벤트로 알림 → PlayerHPbar가 구독해 UI를 갱신한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("HP 설정")]
    public int maxHp = 100;

    [Header("피격 설정")]
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.2f;
    public float hurtDuration = 0.4f;      // 피격 무적 시간

    [Header("사망 연출")]
    public Image fadePanel;                // Inspector에서 연결
    public TextMeshProUGUI gameOverText;   // Inspector에서 연결 (NEO 폰트)
    [Range(0.1f, 1f)]
    public float deathAnimSpeed = 0.35f;    // 죽는 애니메이션 재생 속도

    // ── 이벤트 (PlayerHPbar 같은 UI가 Subscribe) ──────────────────────
    /// <summary>HP가 바뀔 때마다 발생. 인수: (현재HP, 최대HP)</summary>
    public event System.Action<int, int> OnHPChanged;
    /// <summary>플레이어 사망 시 발생</summary>
    public event System.Action OnDied;

    // ── 프로퍼티 ──────────────────────────────────────────────────────
    public int CurrentHp => currentHp;
    public bool IsDead   => isDead;
    public bool IsHurt   => isHurt;

    // ── 내부 상태 ──────────────────────────────────────────────────────
    private int  currentHp;
    private bool isDead = false;
    private bool isHurt = false;

    private Rigidbody2D rb;
    private Animator    anim;
    private SpriteRenderer sr;
    private PlayerCombat   combat;   // 피격 시 공격 취소용

    void Awake()
    {
        rb     = GetComponent<Rigidbody2D>();
        anim   = GetComponent<Animator>();
        sr     = GetComponent<SpriteRenderer>();
        combat = GetComponent<PlayerCombat>();
    }

    void Start()
    {
        currentHp = maxHp;
        // 시작 시 초기값 알림 (UI 초기화)
        OnHPChanged?.Invoke(currentHp, maxHp);
    }

    // ── 외부 호출 메서드 ──────────────────────────────────────────────

    /// <summary>넉백 방향과 함께 피격 처리 (무적 시간 + 넉백)</summary>
    public void TakeHit(Vector2 knockbackDirection)
    {
        if (isHurt || isDead) return;
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(HurtRoutine(GetAxisAligned(knockbackDirection)));
    }

    /// <summary>HP 감소. 0 이하면 사망 연출 시작</summary>
    public void TakeDamage(int amount)
    {
        if (isDead) return;
        currentHp = Mathf.Max(0, currentHp - amount);
        OnHPChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0)
            TriggerGameOver();
    }

    /// <summary>HP 회복 (최대 HP 초과 불가)</summary>
    public void Heal(int amount)
    {
        if (isDead) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        OnHPChanged?.Invoke(currentHp, maxHp);
    }

    /// <summary>속도 증가 아이템 코루틴.
    /// moveSpeed에 임시 보너스를 더했다가 duration 후 원래 값으로 복원.
    /// 씬 전환이 일어나면 OnSceneLoaded에서 moveSpeed가 base+equip으로 덮이므로
    /// 복원 시 아무 영향도 없도록 시작값을 기억해 두고 그 값으로 되돌린다.</summary>
    public IEnumerator SpeedBoostCoroutine(float amount, float duration)
    {
        var pm = GetComponent<PlayerMovement>();
        if (pm == null) yield break;

        float speedBefore = pm.moveSpeed;   // 부스트 시작 시점의 속도를 기억
        pm.moveSpeed += amount;

        yield return new WaitForSeconds(duration);

        // 씬 전환 후 moveSpeed가 base+equip으로 재설정됐을 수 있으므로
        // 기억해 둔 speedBefore 값으로 복원하되, base 속도보다는 항상 높게 유지
        pm.moveSpeed = Mathf.Max(pm.BaseSpeed, speedBefore);
    }

    // ── 게임오버 ──────────────────────────────────────────────────────

    /// <summary>HP=0 또는 타이머 만료 시 호출. 빨간 점멸 → 죽는 애니메이션 → 검정화면 → 메인메뉴</summary>
    public void TriggerGameOver()
    {
        if (isDead) return;
        isDead    = true;
        currentHp = 0;
        isHurt    = false;
        StopAllCoroutines();   // HurtRoutine 등 방해 코루틴 즉시 중단
        OnHPChanged?.Invoke(0, maxHp);
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        // ── 1. 시간 멈춤 + 이동 중지 + UI 숨김 ───────────────────────
        rb.linearVelocity  = Vector2.zero;
        rb.angularVelocity = 0f;
        combat?.CancelAttack();
        Time.timeScale = 0f;

        // 인벤토리가 열려있으면 즉시 닫기
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsOpen)
        {
            InventoryManager.Instance.inventoryUI?.SetActive(false);
            InventoryManager.Instance.hotbarUI?.SetActive(false);
        }

        // ── 오버레이 확보 (fadePanel 미연결 시 자동 생성) ─────────────
        Image overlay = fadePanel;
        if (overlay == null && gameOverText != null)
        {
            Canvas canvas = gameOverText.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var go = new GameObject("_GameOverOverlay");
                go.transform.SetParent(canvas.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                overlay = go.AddComponent<Image>();
                overlay.color = new Color(0f, 0f, 0f, 0f);
                overlay.raycastTarget = false;
                // 오버레이는 텍스트 바로 아래에 렌더링
                Transform textRoot = gameOverText.transform;
                while (textRoot.parent != null && textRoot.parent != canvas.transform)
                    textRoot = textRoot.parent;
                go.transform.SetSiblingIndex(textRoot.GetSiblingIndex());
            }
        }

        // ── 2. 빨간 점멸 1회 (페이드인 → 페이드아웃) ────────────────
        if (overlay != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.15f;
                overlay.color = new Color(1f, 0f, 0f, Mathf.Lerp(0f, 0.7f, t));
                yield return null;
            }
            t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.15f;
                overlay.color = new Color(1f, 0f, 0f, Mathf.Lerp(0.7f, 0f, t));
                yield return null;
            }
            overlay.color = new Color(0f, 0f, 0f, 0f);
        }

        // ── 3. 죽는 애니메이션 (투명 상태에서 완전히 보이게, UnscaledTime) ──
        AudioManager.Instance?.PlaySFX("death");
        OnDied?.Invoke();

        if (anim != null)
        {
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            anim.speed = deathAnimSpeed;

            float dirX = anim.GetFloat("DirX");
            float dirY = anim.GetFloat("DirY");
            if (Mathf.Abs(dirX) >= Mathf.Abs(dirY))
                sr.flipX = dirX > 0;
            else
                sr.flipX = false;

            anim.SetBool("IsWalking",   false);
            anim.SetBool("IsAttacking", false);
            anim.SetBool("IsHurt",      false);
            anim.SetBool("IsDie",       true);

            // 트랜지션 없어도 Death 스테이트 강제 재생
            anim.Play("Death", 0, 0f);
            yield return null;

            // normalizedTime >= 1 대기 (Loop Time OFF 시 마지막 프레임 고정)
            float elapsed = 0f;
            while (anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f && elapsed < 10f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            anim.enabled = false;
        }

        // ── 4. 검정 페이드인 (애니메이션 완전히 끝난 후) ─────────────
        if (overlay != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 1.5f;
                overlay.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t));
                yield return null;
            }
            overlay.color = Color.black;
        }

        // ── 5. "죽었습니다." 텍스트만 표시 (나머지 UI 전부 끔) ───────
        UICanvas.Instance?.HideUI();
        WeaponSlotUI.Instance?.Hide();
        HotbarManager.Instance?.Hide();
        InventoryManager.Instance?.inventoryUI?.SetActive(false);
        InventoryManager.Instance?.hotbarUI?.SetActive(false);
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "죽었습니다.";
        }

        yield return new WaitForSecondsRealtime(2.5f);
        Time.timeScale = 1f;
        GameManager.Instance?.ResetGame();
        Destroy(gameObject);
        SceneManager.LoadScene("MainMenu");
    }

    // ── 내부 코루틴 ──────────────────────────────────────────────────

    IEnumerator HurtRoutine(Vector2 knockbackDir)
    {
        isHurt = true;
        anim.SetBool("IsHurt", true);
        AudioManager.Instance?.PlaySFX("hurt");

        // 공격 중이면 즉시 취소
        combat?.CancelAttack();

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDir.normalized * knockbackForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);
        rb.linearVelocity = Vector2.zero;

        float remaining = hurtDuration - knockbackDuration;
        if (remaining > 0) yield return new WaitForSeconds(remaining);

        anim.SetBool("IsHurt", false);
        isHurt = false;
    }

    // 대각선 벡터를 4방향(상/하/좌/우) 중 하나로 스냅
    Vector2 GetAxisAligned(Vector2 dir)
    {
        if (dir == Vector2.zero)
        {
            var pm = GetComponent<PlayerMovement>();
            Vector2 last = pm != null ? pm.LastDir : Vector2.down;
            return last == Vector2.zero ? Vector2.down : -last.normalized;
        }
        return Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)
            ? new Vector2(Mathf.Sign(dir.x), 0f)
            : new Vector2(0f, Mathf.Sign(dir.y));
    }
}
