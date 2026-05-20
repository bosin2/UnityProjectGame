using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
            StartCoroutine(DeathFadeRoutine());
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

        float speedBefore = pm.moveSpeed;   // 시작 시점의 속도를 기억
        pm.moveSpeed += amount;

        yield return new WaitForSeconds(duration);

        // 씬이 바뀌어 moveSpeed가 이미 재설정됐을 수 있으므로
        // "더했던 amount"를 빼는 대신, 기억해 둔 원래 값으로 복원
        // (단, 씬 전환 후 base+equip이 speedBefore보다 크면 그냥 냅둠)
        pm.moveSpeed = Mathf.Max(pm.BaseSpeed + 0f, pm.moveSpeed - amount);
    }

    // ── 내부 코루틴 ──────────────────────────────────────────────────

    IEnumerator HurtRoutine(Vector2 knockbackDir)
    {
        isHurt = true;
        anim.SetBool("IsHurt", true);

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

    IEnumerator DeathFadeRoutine()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            if (fadePanel != null)
                fadePanel.color = new Color(1, 0, 0, Mathf.Lerp(0, 0.6f, t));
            yield return null;
        }
        yield return new WaitForSeconds(0.3f);
        Die();
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        AudioManager.Instance?.PlaySFX("death");

        // 사망 방향에 맞게 스프라이트 플립
        float dirX = anim.GetFloat("DirX");
        float dirY = anim.GetFloat("DirY");
        if (Mathf.Abs(dirX) >= Mathf.Abs(dirY))
            sr.flipX = dirX > 0;
        else
            sr.flipX = false;

        anim.SetBool("IsDie", true);
        OnDied?.Invoke();
        StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        yield return null; // IsDie 파라미터 반영 대기
        while (anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f
               && anim.GetCurrentAnimatorStateInfo(0).IsName("Die"))
            yield return null;

        anim.enabled = false;
        GameManager.Instance?.ResetGame();
        gameObject.SetActive(false);
        SceneManager.LoadScene("MainMenu");
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
