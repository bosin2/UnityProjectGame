using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public enum EndingKillFlag
{
    None,
    K,
    J,
    P
}
/// <summary>
/// 모든 몬스터의 공통 기능을 제공하는 추상 기반 클래스.
/// MonsterAI, RangedMonster, StalkerMonster 등이 이 클래스를 상속한다.
///
/// 공통 기능: HP 관리, 피격/사망 처리, 월드 HP바 생성, 플레이어 탐색
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class MonsterBase : MonoBehaviour
{
    [Header("엔딩 분기")]
    [Tooltip("이 몬스터가 죽으면 어떤 플래그를 켤지 선택. 일반 몹은 None")]
    public EndingKillFlag killFlag = EndingKillFlag.None;

    [Header("HP 설정")]
    public int maxHp = 60;

    [Header("HP바 (선택)")]
    public GameObject hpBarPrefab;     // 월드 공간 HP바 프리팹 (없으면 생략)
    public Vector2 hpBarOffset = new Vector2(0f, 1f);

    [Header("넉백")]
    public float knockbackForce = 5f;

    [Header("Damage Filters")]
    [SerializeField] private bool takesMeleeDamage = true;

    [Header("Corpse Interaction")]
    [SerializeField] private bool leaveCorpse = true;
    [SerializeField] private bool corpseInteractable = true;
    [SerializeField] private string corpseInteractableId = "";
    [SerializeField] private string[] corpseDialogueLines = { "..." };

    [Header("Scene Persistence")]
    [SerializeField] private string persistentMonsterId = "";

    [Header("Monster Pass-Through")]
    [Tooltip("true면 씬의 MonsterAI와 물리 충돌을 무시 (JBS·PYH 전용)")]
    [SerializeField] private bool ignoreMonsterCollision = false;

    // ── 공용 필드 (자식 클래스에서 접근) ──────────────────────────────
    protected int currentHp;
    protected bool isDead = false;

    protected Rigidbody2D rb;
    protected Animator anim;
    protected SpriteRenderer sr;

    // HP바 인스턴스 (월드 공간 슬라이더 등)
    private GameObject hpBarInstance;
    private Image hpFillImage;
    private bool restoringPersistentCorpse = false;
    private Vector3 restoredCorpsePosition;

    protected virtual bool PersistsStateAcrossScenes => false;

    // ── 초기화 ──────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        EnsureMonsterHitbox();
    }

    protected virtual void Start()
    {
        currentHp = maxHp;
        SpawnHPBar();
        RestorePersistentState();

        if (ignoreMonsterCollision)
            SetupMonsterPassThrough();
    }

    protected virtual void OnDisable()
    {
        SavePersistentState();
    }

    // ── 피격 / 사망 ───────────────────────────────────────────────────

    /// <summary>외부에서 호출 — 데미지를 받는다. 0 이하면 사망 처리.</summary>
    public virtual void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHp -= amount;
        currentHp = Mathf.Max(0, currentHp); // hp는 항상 0 이상 유지
        UpdateHPBar();

        if (currentHp <= 0)
        {
            // isDead=true 설정 후 SavePersistentState가 DieRoutine 내부에서 호출됨
            StartCoroutine(DieRoutine());
        }
        else
        {
            SavePersistentState();
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

    public virtual void TakeMeleeDamage(int amount)
    {
        if (!takesMeleeDamage) return;
        TakeDamage(amount);
    }

    protected virtual IEnumerator DieRoutine()
    {
        isDead = true;
        SavePersistentState();

        // ── 엔딩 분기 플래그 설정 ──
        if (GameManager.Instance != null)   // ← GameManager로 변경!
        {
            switch (killFlag)
            {
                case EndingKillFlag.J:
                    GameManager.Instance.JKilled = true;   // ← GameManager로 변경!
                    Debug.Log("[Ending] 정범석 사망 플래그 ON");
                    break;
                case EndingKillFlag.P:
                    GameManager.Instance.PKilled = true;   // ← GameManager로 변경!
                    Debug.Log("[Ending] 박윤하 사망 플래그 ON");
                    break;
            }
        }
        PrepareForDeathAnimation();

        if (hpBarInstance != null)
            Destroy(hpBarInstance);

        anim?.SetBool("IsDie", true);

        // Die State에 진입할 때까지 대기
        float waitTimeout = 1f;
        float waited = 0f;
        while (anim != null
               && !anim.GetCurrentAnimatorStateInfo(0).IsName("Die")
               && waited < waitTimeout)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        // Die 애니메이션 재생 완료까지 대기
        if (anim != null)
        {
            float timeout = 3f;
            float elapsed = 0f;
            while (anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f
                   && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        if (leaveCorpse)
            BecomeCorpse();
        else
            Destroy(gameObject);
    }

    // ── HP바 ──────────────────────────────────────────────────────────

    protected virtual void PrepareForDeathAnimation()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        foreach (MonsterHitbox hitbox in GetComponentsInChildren<MonsterHitbox>())
            hitbox.enabled = false;

        foreach (LineRenderer line in GetComponentsInChildren<LineRenderer>())
            line.enabled = false;
    }

    protected virtual void BecomeCorpse()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Static;
        rb.simulated = true;

        if (anim != null)
            anim.speed = 0f;

        if (restoringPersistentCorpse)
            transform.position = restoredCorpsePosition;

        if (corpseInteractable)
            SetupCorpseInteractable();

        SavePersistentState();
    }

    void RestorePersistentState()
    {
        if (!PersistsStateAcrossScenes || GameManager.Instance == null) return;
        if (!GameManager.Instance.TryGetMonsterState(GetPersistentStateId(), out GameManager.MonsterState state)) return;

        // hp=0이고 isDead=false인 것은 이전 버그로 인한 스테일 상태 — 무시
        if (state.hp <= 0 && !state.isDead) return;

        transform.position = state.position;
        currentHp = Mathf.Clamp(state.hp, 0, maxHp);
        UpdateHPBar();

        if (state.isDead)
        {
            currentHp = 0;
            restoredCorpsePosition = state.position;
            restoringPersistentCorpse = true;
            StartCoroutine(RestoreCorpseRoutine());
        }
    }

    IEnumerator RestoreCorpseRoutine()
    {
        isDead = true;
        transform.position = restoredCorpsePosition;
        SavePersistentState();
        PrepareForDeathAnimation();

        if (hpBarInstance != null)
            Destroy(hpBarInstance);

        if (anim != null)
        {
            anim.SetBool("IsDie", true);
            yield return null;
            anim.Play("Die", 0, 1f);
            anim.Update(0f);
        }

        if (leaveCorpse)
            BecomeCorpse();
        else
            gameObject.SetActive(false);

        restoringPersistentCorpse = false;
    }

    void SavePersistentState()
    {
        if (!PersistsStateAcrossScenes || GameManager.Instance == null) return;

        Vector3 positionToSave = restoringPersistentCorpse && isDead
            ? restoredCorpsePosition
            : transform.position;

        GameManager.Instance.SaveMonsterState(
            GetPersistentStateId(),
            positionToSave,
            currentHp,
            isDead);
    }

    string GetPersistentStateId()
    {
        string localId = !string.IsNullOrEmpty(persistentMonsterId)
            ? persistentMonsterId
            : gameObject.name;

        return $"{gameObject.scene.name}_{localId}";
    }

    void SetupCorpseInteractable()
    {
        Collider2D corpseCollider = GetComponent<Collider2D>();
        if (corpseCollider == null)
            corpseCollider = gameObject.AddComponent<CircleCollider2D>();

        corpseCollider.enabled = true;
        corpseCollider.isTrigger = false;

        GameObject triggerObject = GetOrCreateCorpseInteractTrigger(corpseCollider);
        Interactable interactable = triggerObject.GetComponent<Interactable>();
        if (interactable == null)
            interactable = triggerObject.AddComponent<Interactable>();

        if (string.IsNullOrEmpty(interactable.interactableId))
        {
            interactable.interactableId = string.IsNullOrEmpty(corpseInteractableId)
                ? $"{gameObject.scene.name}_{gameObject.name}_corpse"
                : corpseInteractableId;
        }

        if (interactable.phases == null || interactable.phases.Length == 0)
        {
            interactable.phases = new DialoguePhase[]
            {
                new DialoguePhase
                {
                    dialogueLines = corpseDialogueLines
                }
            };
        }
    }

    GameObject GetOrCreateCorpseInteractTrigger(Collider2D corpseCollider)
    {
        const string triggerName = "CorpseInteractTrigger";

        Transform existing = transform.Find(triggerName);
        GameObject triggerObject = existing != null
            ? existing.gameObject
            : new GameObject(triggerName);

        triggerObject.transform.SetParent(transform, false);
        triggerObject.layer = gameObject.layer;

        CircleCollider2D trigger = triggerObject.GetComponent<CircleCollider2D>();
        if (trigger == null)
            trigger = triggerObject.AddComponent<CircleCollider2D>();

        Bounds bounds = corpseCollider.bounds;
        Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
        float radius = Mathf.Max(bounds.size.x, bounds.size.y) * 0.5f;

        trigger.enabled = true;
        trigger.isTrigger = true;
        trigger.offset = localCenter;
        trigger.radius = Mathf.Max(radius, 0.75f);

        return triggerObject;
    }

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

    void SetupMonsterPassThrough()
    {
        Collider2D[] myCols = GetComponentsInChildren<Collider2D>();
        MonsterAI[] ais = FindObjectsByType<MonsterAI>(FindObjectsSortMode.None);
        foreach (MonsterAI ai in ais)
        {
            if (ai.gameObject == gameObject) continue;
            foreach (Collider2D aiCol in ai.GetComponentsInChildren<Collider2D>())
                foreach (Collider2D myCol in myCols)
                    Physics2D.IgnoreCollision(myCol, aiCol, true);
        }
    }

    /// <summary>이펙트 프리팹을 지정 위치에 잠깐 스폰 (자동 제거)</summary>
    void EnsureMonsterHitbox()
    {
        if (GetComponentInChildren<MonsterHitbox>() != null) return;
        if (GetComponent<Collider2D>() == null) return;

        gameObject.AddComponent<MonsterHitbox>();
    }

    protected void SpawnEffect(GameObject prefab, Vector2 pos, float lifetime = 1f)
    {
        if (prefab == null) return;
        GameObject fx = Instantiate(prefab, pos, Quaternion.identity);
        Destroy(fx, lifetime);
    }
}
