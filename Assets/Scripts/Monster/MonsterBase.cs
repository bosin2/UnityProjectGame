/*
 * MonsterBase.cs
 * 역할: 모든 몬스터의 HP, 피격, 사망, HP바, 시체 상호작용, 씬 간 상태 저장을 제공하는 기반 클래스입니다.
 * 연결: MonsterAI, StalkerMonster, RangedMonster가 상속하며 GameManager에 위치/HP/사망 여부를 저장합니다.
 * 주의: 특수몹 공유 버그 방지를 위해 저장 ID는 씬 이름과 persistentMonsterId를 조합하므로 ID 중복과 씬명을 함께 고려해야 합니다.
 */
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 엔딩 분기 플래그 — 이 몬스터가 죽었을 때 어떤 엔딩 변수를 올릴지를 나타낸다.
/// K=김우진, J=정범석, P=박윤하. 일반 몬스터는 None으로 둔다.
/// </summary>
public enum EndingKillFlag { None, K, J, P }

/// <summary>
/// 모든 몬스터의 공통 기능을 제공하는 추상 기반 클래스.
/// MonsterAI, RangedMonster, StalkerMonster 등이 이 클래스를 상속한다.
///
/// 공통 기능: HP 관리, 피격/사망 처리, 월드 HP바 생성, 씬 간 상태 영속화, 시체 상호작용
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class MonsterBase : MonoBehaviour
{
    // ── 엔딩 분기 ────────────────────────────────────────────────────────
    // 이 몬스터가 사망했을 때 GameManager의 어느 플래그를 true로 할지 결정한다.
    // None: 일반 몬스터 (엔딩 분기 없음)
    // J/P:  DieRoutine에서 GameManager.JKilled / PKilled를 true로 설정
    // (K=김우진은 GameFlowManager.GunShotEvent에서 처리)
    [Header("엔딩 분기")]
    [Tooltip("이 몬스터가 죽으면 어떤 플래그를 켤지 선택. 일반 몹은 None")]
    public EndingKillFlag killFlag = EndingKillFlag.None;

    // ── HP 설정 ──────────────────────────────────────────────────────────
    [Header("HP 설정")]
    public int maxHp = 60;

    // ── HP바 (선택) ──────────────────────────────────────────────────────
    // hpBarPrefab이 할당된 경우 Start()에서 월드 공간에 인스턴스를 생성한다.
    // hpBarOffset: 몬스터 중심에서 HP바까지의 오프셋 (보통 머리 위)
    [Header("HP바 (선택)")]
    public GameObject hpBarPrefab;
    public Vector2    hpBarOffset = new Vector2(0f, 1f);

    // ── 넉백 ─────────────────────────────────────────────────────────────
    [Header("넉백")]
    public float knockbackForce = 5f;

    // ── 피해 적용 조건 ───────────────────────────────────────────────────
    // takesMeleeDamage = false이면 TakeMeleeDamage()가 데미지를 무시한다.
    // 원거리 몬스터처럼 근접 공격에 내성이 필요한 경우 사용.
    [Header("피해 적용 조건")]
    [SerializeField] private bool takesMeleeDamage = true;

    // ── 시체 상호작용 ────────────────────────────────────────────────────
    // leaveCorpse: true이면 사망 후 오브젝트를 유지해 시체 상태로 전환
    // corpseInteractable: 시체에 Interactable 컴포넌트를 자동으로 부착할지 여부
    // corpseInteractableId: 시체 Interactable의 고유 ID (비우면 자동 생성)
    // corpseDialogueLines: 시체에 Q키를 눌렀을 때 표시되는 대사
    [Header("시체 상호작용")]
    [SerializeField] private bool     leaveCorpse          = true;
    [SerializeField] private bool     corpseInteractable   = true;
    [SerializeField] private string   corpseInteractableId = "";
    [SerializeField] private string[] corpseDialogueLines  = { "..." };

    // ── 씬 상태 유지 ─────────────────────────────────────────────────────
    // persistentMonsterId: 씬 간 HP/위치/사망 여부를 유지할 고유 ID.
    // 비우면 gameObject.name을 ID로 사용한다.
    // 실제 저장 키는 "{씬이름}_{persistentMonsterId}" 형태로 합성되므로
    // 서로 다른 씬에서 같은 ID를 써도 충돌하지 않는다.
    [Header("씬 상태 유지")]
    [SerializeField] private string persistentMonsterId = "";

    // ── 몬스터 통과 설정 ─────────────────────────────────────────────────
    // ignoreMonsterCollision = true이면 씬의 모든 MonsterAI 콜라이더와 물리 충돌을 무시.
    // JBS·PYH 등 특수 몬스터끼리 겹쳐 지나가야 할 때 사용한다.
    [Header("몬스터 통과 설정")]
    [Tooltip("true면 씬의 MonsterAI와 물리 충돌을 무시 (JBS·PYH 전용)")]
    [SerializeField] private bool ignoreMonsterCollision = false;

    // ── 자식 클래스 공유 필드 ────────────────────────────────────────────
    protected int  currentHp;
    protected bool isDead = false;

    protected Rigidbody2D    rb;
    protected Animator       anim;
    protected SpriteRenderer sr;

    // HP바 인스턴스 및 채움 이미지 (LateUpdate에서 위치 동기화)
    private GameObject hpBarInstance;
    private Image      hpFillImage;

    // 시체 복원 중 위치 오버라이드 플래그와 복원 위치
    private bool    restoringPersistentCorpse = false;
    private Vector3 restoredCorpsePosition;

    /// <summary>
    /// 씬 간 상태를 GameManager에 저장/복원할지 여부.
    /// 기본값 false — StalkerMonster, RangedMonster 등 특수 몬스터가 override해 true로 설정.
    /// </summary>
    protected virtual bool PersistsStateAcrossScenes => false;


    // ===================================================================
    // 초기화
    // ===================================================================

    protected virtual void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        sr   = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

        // MonsterHitbox가 없으면 자동으로 부착 (Collider2D가 있는 경우에만)
        EnsureMonsterHitbox();
    }

    protected virtual void Start()
    {
        currentHp = maxHp;
        SpawnHPBar();
        RestorePersistentState(); // 이전 씬 방문 시 저장된 상태 복원

        if (ignoreMonsterCollision)
            SetupMonsterPassThrough(); // 다른 MonsterAI들과 물리 충돌 무시 설정
    }

    protected virtual void OnDisable()
    {
        // 씬 전환이나 오브젝트 비활성화 시 현재 상태를 GameManager에 저장
        SavePersistentState();
    }


    // ===================================================================
    // 피격 / 사망
    // ===================================================================

    /// <summary>
    /// 외부에서 데미지를 주는 기본 메서드.
    /// 피격 시 Animator의 "IsHurt" 트리거 실행 + 플레이어 방향으로 넉백.
    /// HP가 0 이하이면 DieRoutine 코루틴을 시작한다.
    /// </summary>
    public virtual void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHp -= amount;
        currentHp  = Mathf.Max(0, currentHp); // HP는 항상 0 이상 유지
        UpdateHPBar();

        if (currentHp <= 0)
        {
            // isDead 플래그는 DieRoutine 내부 첫 줄에서 세워짐
            StartCoroutine(DieRoutine());
        }
        else
        {
            SavePersistentState(); // 피격 후 남은 HP를 즉시 저장
            anim?.SetTrigger("IsHurt");

            // 넉백: 플레이어 → 몬스터 방향 벡터로 밀어냄
            PlayerHealth player = FindPlayerHealth();
            if (player != null)
            {
                Vector2 knockDir = ((Vector2)transform.position
                                   - (Vector2)player.transform.position).normalized;
                rb.linearVelocity = knockDir * knockbackForce;
            }
        }
    }

    /// <summary>
    /// 근접 공격(MonsterHitbox)을 통해 들어오는 데미지.
    /// takesMeleeDamage = false이면 무시한다.
    /// </summary>
    public virtual void TakeMeleeDamage(int amount)
    {
        if (!takesMeleeDamage) return;
        TakeDamage(amount);
    }

    /// <summary>
    /// 사망 처리 코루틴.
    /// 1. isDead 플래그 세우기 + 상태 저장
    /// 2. 엔딩 분기 플래그 설정 (J/P)
    /// 3. 사망 준비 (콜라이더/히트박스 비활성)
    /// 4. HP바 제거
    /// 5. "IsDie" Animator 파라미터 세우기
    /// 6. Die 상태 진입 대기 (최대 1초 timeout)
    /// 7. Die 애니메이션 완료 대기 (최대 3초 timeout)
    /// 8. leaveCorpse에 따라 시체 전환 또는 오브젝트 제거
    /// </summary>
    protected virtual IEnumerator DieRoutine()
    {
        isDead = true;
        SavePersistentState();

        // ── 엔딩 분기 플래그 설정 ──
        if (GameManager.Instance != null)
        {
            switch (killFlag)
            {
                case EndingKillFlag.J:
                    GameManager.Instance.JKilled = true;
                    Debug.Log("[Ending] 정범석 사망 플래그 ON");
                    break;
                case EndingKillFlag.P:
                    GameManager.Instance.PKilled = true;
                    Debug.Log("[Ending] 박윤하 사망 플래그 ON");
                    break;
                // EndingKillFlag.K는 GameFlowManager.GunShotEvent에서 처리
            }
        }

        PrepareForDeathAnimation(); // 물리/콜라이더 정리

        if (hpBarInstance != null)
            Destroy(hpBarInstance); // 사망 즉시 HP바 제거

        anim?.SetBool("IsDie", true);

        // Die 상태(Animator State "Die")에 진입할 때까지 최대 1초 대기
        // 트랜지션이 없는 경우를 대비한 timeout 처리
        float waitTimeout = 1f;
        float waited      = 0f;
        while (anim != null
               && !anim.GetCurrentAnimatorStateInfo(0).IsName("Die")
               && waited < waitTimeout)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        // Die 애니메이션이 끝날 때까지 대기 (최대 3초 timeout, Loop Time OFF 시 normalizedTime >= 1)
        if (anim != null)
        {
            float timeout = 3f;
            float elapsed = 0f;
            while (anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // 시체 유지 여부에 따라 분기
        if (leaveCorpse)
            BecomeCorpse(); // 오브젝트 유지 + 상호작용 설정
        else
            Destroy(gameObject); // 오브젝트 완전 제거
    }


    // ===================================================================
    // 사망 연출 준비
    // ===================================================================

    /// <summary>
    /// 사망 애니메이션 재생 전 물리/히트박스를 정리한다.
    /// - Rigidbody2D 속도 0
    /// - 모든 Collider2D 비활성 (더 이상 피격받지 않음)
    /// - MonsterHitbox 비활성 (더 이상 공격하지 않음)
    /// - LineRenderer 비활성 (StalkerMonster 경로 시각화 등)
    /// </summary>
    protected virtual void PrepareForDeathAnimation()
    {
        rb.linearVelocity  = Vector2.zero;
        rb.angularVelocity = 0f;

        // 하위 오브젝트의 모든 물리 콜라이더 비활성 (공격 히트박스 포함)
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        // MonsterHitbox (근접 공격 인식기) 비활성
        foreach (MonsterHitbox hitbox in GetComponentsInChildren<MonsterHitbox>())
            hitbox.enabled = false;

        // LineRenderer 비활성 (StalkerMonster의 경로 시각화 등)
        foreach (LineRenderer line in GetComponentsInChildren<LineRenderer>())
            line.enabled = false;
    }

    /// <summary>
    /// 오브젝트를 시체 상태로 전환한다.
    /// - Rigidbody2D를 Static으로 변경 (물리 영향 없음, 위치만 유지)
    /// - Animator 정지 (마지막 사망 프레임 고정)
    /// - corpseInteractable = true이면 Interactable 컴포넌트 동적 부착
    /// - 씬 재진입 시 복원된 시체는 저장된 위치로 스냅
    /// </summary>
    protected virtual void BecomeCorpse()
    {
        rb.linearVelocity  = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType        = RigidbodyType2D.Static; // 물리 시뮬레이션에서 제외 (위치만 유지)
        rb.simulated       = true;

        if (anim != null)
            anim.speed = 0f; // 마지막 프레임(시체 자세)에 고정

        // 씬 재진입으로 시체를 복원하는 경우 저장된 위치로 스냅
        if (restoringPersistentCorpse)
            transform.position = restoredCorpsePosition;

        if (corpseInteractable)
            SetupCorpseInteractable(); // 시체에 대화 트리거 부착

        SavePersistentState(); // 시체 상태(isDead=true, 위치)를 최종 저장
    }


    // ===================================================================
    // 씬 간 상태 저장/복원
    // ===================================================================

    /// <summary>
    /// GameManager에 저장된 이전 상태를 읽어 몬스터에 적용한다.
    /// - HP 복원
    /// - 위치 복원
    /// - 사망 상태이면 RestoreCorpseRoutine으로 시체 연출 재현
    /// PersistsStateAcrossScenes = false이면 아무것도 하지 않는다.
    /// </summary>
    void RestorePersistentState()
    {
        if (!PersistsStateAcrossScenes || GameManager.Instance == null) return;
        if (!GameManager.Instance.TryGetMonsterState(GetPersistentStateId(), out GameManager.MonsterState state)) return;

        // hp=0이고 isDead=false인 것은 이전 버그로 인한 스테일(stale) 상태 — 무시
        // 정상적으로 HP가 0이 되면 반드시 isDead=true가 함께 저장된다
        if (state.hp <= 0 && !state.isDead) return;

        transform.position = state.position;
        currentHp          = Mathf.Clamp(state.hp, 0, maxHp);
        UpdateHPBar();

        if (state.isDead)
        {
            // 저장된 시체 위치를 기억해 두고 RestoreCorpseRoutine에서 사용
            currentHp                = 0;
            restoredCorpsePosition   = state.position;
            restoringPersistentCorpse = true;
            StartCoroutine(RestoreCorpseRoutine());
        }
    }

    /// <summary>
    /// 씬 재진입 시 이미 죽은 몬스터를 시체 상태로 즉시 복원하는 코루틴.
    /// DieRoutine과 달리 애니메이션의 마지막 프레임으로 직접 점프한다.
    /// (anim.Play("Die", 0, 1f) + anim.Update(0f) = 마지막 프레임 즉시 렌더링)
    /// </summary>
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
            yield return null; // 파라미터가 Animator에 반영될 때까지 한 프레임 대기

            // Die 애니메이션의 마지막 프레임(normalizedTime=1)으로 즉시 점프
            anim.Play("Die", 0, 1f);
            anim.Update(0f); // 즉시 렌더링 (다음 프레임까지 기다리지 않음)
        }

        if (leaveCorpse)
            BecomeCorpse();
        else
            gameObject.SetActive(false);

        restoringPersistentCorpse = false;
    }

    /// <summary>
    /// 현재 HP, 위치, 사망 여부를 GameManager에 저장한다.
    /// 시체 복원 중일 때는 실제 transform.position 대신 저장된 위치를 사용한다.
    /// (복원 중에는 위치가 아직 최종값이 아닐 수 있기 때문)
    /// </summary>
    void SavePersistentState()
    {
        if (!PersistsStateAcrossScenes || GameManager.Instance == null) return;

        // 시체 복원 중에는 transform.position이 아직 최종 위치가 아닐 수 있으므로
        // restoredCorpsePosition을 직접 사용
        Vector3 posToSave = (restoringPersistentCorpse && isDead)
            ? restoredCorpsePosition
            : transform.position;

        GameManager.Instance.SaveMonsterState(GetPersistentStateId(), posToSave, currentHp, isDead);
    }

    /// <summary>
    /// GameManager 저장에 사용할 고유 키를 반환한다.
    /// 형식: "{씬이름}_{persistentMonsterId 또는 gameObject.name}"
    /// 씬 이름을 접두사로 붙여 서로 다른 씬의 몬스터 ID 충돌을 방지한다.
    /// </summary>
    string GetPersistentStateId()
    {
        string localId = !string.IsNullOrEmpty(persistentMonsterId)
            ? persistentMonsterId
            : gameObject.name;

        return $"{gameObject.scene.name}_{localId}";
    }


    // ===================================================================
    // 시체 상호작용 설정
    // ===================================================================

    /// <summary>
    /// 시체 오브젝트에 Interactable 컴포넌트를 동적으로 부착한다.
    /// - 기존 Collider2D가 없으면 CircleCollider2D를 추가
    /// - 자식 "CorpseInteractTrigger" 오브젝트에 트리거 콜라이더 부착
    /// - Interactable 컴포넌트에 대화 데이터 주입
    /// </summary>
    void SetupCorpseInteractable()
    {
        Collider2D corpseCollider = GetComponent<Collider2D>();
        if (corpseCollider == null)
            corpseCollider = gameObject.AddComponent<CircleCollider2D>(); // 시체용 기본 콜라이더

        corpseCollider.enabled   = true;
        corpseCollider.isTrigger = false; // 물리 충돌 콜라이더로 유지

        // 별도 트리거 오브젝트에 Interactable 부착 (시체 콜라이더와 분리)
        GameObject  triggerObject = GetOrCreateCorpseInteractTrigger(corpseCollider);
        Interactable interactable = triggerObject.GetComponent<Interactable>();
        if (interactable == null)
            interactable = triggerObject.AddComponent<Interactable>();

        // ID 설정: 지정된 ID가 없으면 씬+오브젝트명으로 자동 생성
        if (string.IsNullOrEmpty(interactable.interactableId))
        {
            interactable.interactableId = string.IsNullOrEmpty(corpseInteractableId)
                ? $"{gameObject.scene.name}_{gameObject.name}_corpse"
                : corpseInteractableId;
        }

        // 대화 데이터가 아직 없을 때만 주입 (기존 데이터 덮어쓰기 방지)
        if (interactable.phases == null || interactable.phases.Length == 0)
        {
            interactable.phases = new DialoguePhase[]
            {
                new DialoguePhase { dialogueLines = corpseDialogueLines }
            };
        }
    }

    /// <summary>
    /// "CorpseInteractTrigger"라는 이름의 자식 오브젝트를 찾거나 새로 만든다.
    /// 트리거 콜라이더의 반지름은 시체 콜라이더의 bounds에서 자동으로 계산한다.
    /// </summary>
    GameObject GetOrCreateCorpseInteractTrigger(Collider2D corpseCollider)
    {
        const string triggerName = "CorpseInteractTrigger";

        Transform    existing     = transform.Find(triggerName);
        GameObject   triggerObj   = existing != null ? existing.gameObject : new GameObject(triggerName);

        triggerObj.transform.SetParent(transform, false);
        triggerObj.layer = gameObject.layer;

        CircleCollider2D trigger = triggerObj.GetComponent<CircleCollider2D>();
        if (trigger == null)
            trigger = triggerObj.AddComponent<CircleCollider2D>();

        // 시체 Bounds로부터 트리거 반지름과 오프셋을 자동 계산
        Bounds  bounds      = corpseCollider.bounds;
        Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
        float   radius      = Mathf.Max(bounds.size.x, bounds.size.y) * 0.5f;

        trigger.enabled   = true;
        trigger.isTrigger = true;
        trigger.offset    = localCenter;
        trigger.radius    = Mathf.Max(radius, 0.75f); // 최소 반지름 0.75 보장 (너무 작으면 탐지 불가)

        return triggerObj;
    }


    // ===================================================================
    // HP바
    // ===================================================================

    /// <summary>
    /// hpBarPrefab을 월드 공간에 인스턴스화하고 Fill Image를 찾아 캐시한다.
    /// HP바 프리팹 구조: 루트 오브젝트 > "Fill"이라는 이름의 자식에 Image 컴포넌트 필요.
    /// </summary>
    void SpawnHPBar()
    {
        if (hpBarPrefab == null) return;

        hpBarInstance = Instantiate(hpBarPrefab,
            (Vector2)transform.position + hpBarOffset,
            Quaternion.identity);

        // 프리팹 구조 규약: "Fill" 자식 오브젝트에 fillAmount 제어용 Image가 있어야 한다
        Transform fillTr = hpBarInstance.transform.Find("Fill");
        if (fillTr != null)
            hpFillImage = fillTr.GetComponent<Image>();

        if (hpFillImage != null)
            hpFillImage.fillAmount = 1f; // 최대 HP로 초기화
    }

    /// <summary>HP바 위치를 몬스터 위치에 동기화하고 채움 비율을 갱신한다.</summary>
    void UpdateHPBar()
    {
        if (hpBarInstance == null) return;

        hpBarInstance.transform.position = (Vector2)transform.position + hpBarOffset;

        if (hpFillImage != null)
            hpFillImage.fillAmount = (float)currentHp / maxHp;
    }

    void LateUpdate()
    {
        // FixedUpdate에서 이동하더라도 LateUpdate에서 위치를 최종 동기화
        if (hpBarInstance != null)
            hpBarInstance.transform.position = (Vector2)transform.position + hpBarOffset;
    }


    // ===================================================================
    // 유틸
    // ===================================================================

    /// <summary>
    /// 현재 씬(또는 DontDestroyOnLoad)에서 PlayerHealth를 찾아 반환한다.
    /// DontDestroyOnLoad 플레이어는 씬 이름이 "DontDestroyOnLoad"로 표시된다.
    /// </summary>
    protected PlayerHealth FindPlayerHealth()
    {
        PlayerHealth[] all = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (PlayerHealth ph in all)
        {
            if (ph.gameObject.scene == gameObject.scene
                || ph.gameObject.scene.name == "DontDestroyOnLoad")
                return ph;
        }
        return null;
    }

    /// <summary>
    /// ignoreMonsterCollision = true일 때 호출.
    /// 씬의 모든 MonsterAI 콜라이더와 이 몬스터의 콜라이더 간 물리 충돌을 무시한다.
    /// 특수 몬스터가 일반 몬스터를 통과해 이동해야 할 때 사용한다.
    /// </summary>
    void SetupMonsterPassThrough()
    {
        Collider2D[] myCols = GetComponentsInChildren<Collider2D>();
        MonsterAI[]  ais    = FindObjectsByType<MonsterAI>(FindObjectsSortMode.None);
        foreach (MonsterAI ai in ais)
        {
            if (ai.gameObject == gameObject) continue; // 자기 자신은 제외
            foreach (Collider2D aiCol in ai.GetComponentsInChildren<Collider2D>())
                foreach (Collider2D myCol in myCols)
                    Physics2D.IgnoreCollision(myCol, aiCol, true); // 영구 무시 설정
        }
    }

    /// <summary>
    /// MonsterHitbox 컴포넌트가 없으면 자동으로 추가한다.
    /// Collider2D가 없으면 히트박스가 의미 없으므로 건너뛴다.
    /// </summary>
    void EnsureMonsterHitbox()
    {
        if (GetComponentInChildren<MonsterHitbox>() != null) return;
        if (GetComponent<Collider2D>() == null) return;
        gameObject.AddComponent<MonsterHitbox>();
    }

    /// <summary>
    /// 이펙트 프리팹을 지정 위치에 스폰하고 lifetime 초 후 자동 제거한다.
    /// RangedMonster의 폭발/텔레포트 이펙트 등에서 사용.
    /// </summary>
    protected void SpawnEffect(GameObject prefab, Vector2 pos, float lifetime = 1f)
    {
        if (prefab == null) return;
        GameObject fx = Instantiate(prefab, pos, Quaternion.identity);
        Destroy(fx, lifetime);
    }
}
