using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모든 몬스터의 공통 기능을 제공하는 추상 기반 클래스.
/// MonsterAI, RangedMonster, StalkerMonster 등이 이 클래스를 상속한다.
///
/// 공통 기능: HP 관리, 피격/사망 처리, 월드 HP바 생성, 플레이어 탐색
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class MonsterBase : MonoBehaviour
{
    [Header("HP 설정")]
    public int maxHp = 60;

    [Header("HP바 (선택)")]
    public GameObject hpBarPrefab;     // 월드 공간 HP바 프리팹 (없으면 생략)
    public Vector2    hpBarOffset = new Vector2(0f, 1f);

    [Header("넉백")]
    public float knockbackForce = 5f;

    // ── 공용 필드 (자식 클래스에서 접근) ──────────────────────────────
    protected int            currentHp;
    protected bool           isDead = false;

    protected Rigidbody2D    rb;
    protected Animator       anim;
    protected SpriteRenderer sr;

    // HP바 인스턴스 (월드 공간 슬라이더 등)
    private GameObject       hpBarInstance;
    private Image hpFillImage;

    // ── 초기화 ──────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr   = GetComponent<SpriteRenderer>();
    }

    protected virtual void Start()
    {
        currentHp = maxHp;
        SpawnHPBar();
    }

    // ── 피격 / 사망 ───────────────────────────────────────────────────

    /// <summary>외부에서 호출 — 데미지를 받는다. 0 이하면 사망 처리.</summary>
    public virtual void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHp -= amount;
        UpdateHPBar();

        if (currentHp <= 0)
        {
            currentHp = 0;
            StartCoroutine(DieRoutine());
        }
        else
        {
            anim?.SetTrigger("IsHurt");

            // 넉백: 플레이어 → 몬스터 방향으로 밀어냄
            PlayerHealth player = FindPlayerHealth();
            if (player != null)
            {
                Vector2 knockDir = ((Vector2)transform.position
                                   - (Vector2)player.transform.position).normalized;
                rb.linearVelocity = knockDir * knockbackForce;
            }
        }
    }

    protected virtual IEnumerator DieRoutine()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        // 콜라이더 비활성화 (죽은 뒤 충돌 방지)
        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = false;
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        if (hpBarInstance != null)
            Destroy(hpBarInstance);

        anim?.SetBool("IsDie", true);
        //AudioManager.Instance?.PlaySFX("monster_die");

        // 사망 애니메이션 완료 대기 (최대 3초 타임아웃 — 루핑 애니메이션 방지)
        yield return null;
        if (anim != null)
        {
            float timeout = 3f;
            float elapsed = 0f;
            while (anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f
                   && anim.GetCurrentAnimatorStateInfo(0).IsName("Die")
                   && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        Destroy(gameObject);
    }

    // ── HP바 ──────────────────────────────────────────────────────────

    void SpawnHPBar()
    {
        if (hpBarPrefab == null) return;

        hpBarInstance = Instantiate(hpBarPrefab,
            (Vector2)transform.position + hpBarOffset,
            Quaternion.identity);

        // "Fill" 이라는 이름의 자식 오브젝트에서 Image 찾기
        Transform fillTr = hpBarInstance.transform.Find("Fill");
        if (fillTr != null)
            hpFillImage = fillTr.GetComponent<Image>();

        if (hpFillImage != null)
            hpFillImage.fillAmount = 1f;
    }

    void UpdateHPBar()
    {
        if (hpBarInstance == null) return;

        hpBarInstance.transform.position = (Vector2)transform.position + hpBarOffset;

        if (hpFillImage != null)
            hpFillImage.fillAmount = (float)currentHp / maxHp;
    }

    void LateUpdate()
    {
        // HP바가 있으면 매 프레임 위치 동기화
        if (hpBarInstance != null)
            hpBarInstance.transform.position = (Vector2)transform.position + hpBarOffset;
    }

    // ── 유틸 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 현재 씬(또는 DontDestroyOnLoad)에서 PlayerHealth를 찾아 반환.
    /// MonsterAI, RangedMonster 등의 FindPlayer() 에서 공통 사용.
    /// </summary>
    protected PlayerHealth FindPlayerHealth()
    {
        // 같은 씬에서 먼저 탐색
        PlayerHealth[] all = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (PlayerHealth ph in all)
        {
            // 플레이어는 DontDestroyOnLoad 씬 또는 현재 씬에 있어야 유효
            if (ph.gameObject.scene == gameObject.scene
                || ph.gameObject.scene.name == "DontDestroyOnLoad")
                return ph;
        }
        return null;
    }

    /// <summary>이펙트 프리팹을 지정 위치에 잠깐 스폰 (자동 제거)</summary>
    protected void SpawnEffect(GameObject prefab, Vector2 pos, float lifetime = 1f)
    {
        if (prefab == null) return;
        GameObject fx = Instantiate(prefab, pos, Quaternion.identity);
        Destroy(fx, lifetime);
    }
}
