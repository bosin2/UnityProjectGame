using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    private static PlayerMovement instance;

    [Header("속도")]
    public float moveSpeed = 5f;

    [Header("공격 설정")]
    public int melee_damage = 20;
    public int shoot_damage = 30;
    public float shootBulletSpeed = 10f;

    [Header("공격 범위")]
    public Collider2D attack_Left;
    public Collider2D attack_Right;
    public Collider2D attack_Front;
    public Collider2D attack_Back;

    [Header("권총 이펙트")]
    public Sprite bulletSprite;
    public Sprite hitEffectSprite;

    [Header("피격 설정")]
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.2f;
    public float hurtDuration = 0.4f;

    private bool isHurt = false;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    private Vector2 movement;
    private Vector2 lastDir = new Vector2(0, -1);
    private bool isAttacking = false;
    private int currentWeapon = 0;
    private bool hasHitThisAttack = false;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update()
    {
        if (isAttacking || isHurt)
        {
            movement = Vector2.zero;
        }
        else
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");

            if (x != 0)
                movement = new Vector2(x, 0);
            else if (y != 0)
                movement = new Vector2(0, y);
            else
                movement = Vector2.zero;

            if (movement != Vector2.zero)
                lastDir = movement;
        }

        anim.SetFloat("DirX", lastDir.x);
        anim.SetFloat("DirY", lastDir.y);
        anim.SetBool("IsWalking", movement != Vector2.zero && !isAttacking && !isHurt);

        if (Input.GetMouseButtonDown(0) && !isAttacking && !isHurt)
        {
            Attack();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
            SwitchWeapon(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SwitchWeapon(1);
    }

    void FixedUpdate()
    {
        if (!isAttacking && !isHurt)
        {
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }
    }

    public void TakeHit(Vector2 knockbackDirection)
    {
        if (isHurt) return;
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(HurtRoutine(knockbackDirection));
    }

    IEnumerator HurtRoutine(Vector2 knockbackDirection)
    {
        isHurt = true;
        anim.SetBool("IsHurt", true);

        // 공격 중이었다면 캔슬
        if (isAttacking)
        {
            isAttacking = false;
            anim.SetBool("IsAttacking", false);
            CancelInvoke("EndAttack");
            attack_Left.enabled = false;
            attack_Right.enabled = false;
            attack_Front.enabled = false;
            attack_Back.enabled = false;
        }

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

        rb.linearVelocity = Vector2.zero;

        float remaining = hurtDuration - knockbackDuration;
        if (remaining > 0)
            yield return new WaitForSeconds(remaining);

        anim.SetBool("IsHurt", false);
        isHurt = false;
    }
    void Attack()
    {
        if (isAttacking) return;

        isAttacking = true;
        hasHitThisAttack = false;

        if (currentWeapon == 0)
            AttackMelee();
        else if (currentWeapon == 1)
            AttackShoot();
    }

    void AttackMelee()
    {
        anim.SetBool("IsAttacking", true);
        ActivateAttackCollider(lastDir);
        float attackDuration = GetCurrentAnimationLength();
        CancelInvoke("EndAttack");
        Invoke("EndAttack", attackDuration);
    }

    void ActivateAttackCollider(Vector2 direction)
    {
        attack_Left.enabled = false;
        attack_Right.enabled = false;
        attack_Front.enabled = false;
        attack_Back.enabled = false;

        if (direction.x > 0) attack_Right.enabled = true;
        else if (direction.x < 0) attack_Left.enabled = true;
        else if (direction.y > 0) attack_Front.enabled = true;
        else if (direction.y < 0) attack_Back.enabled = true;
    }

    void AttackShoot()
    {
        anim.SetBool("IsAttacking", true);
        ShootBullet(lastDir);
        float shootDuration = GetCurrentAnimationLength();
        CancelInvoke("EndAttack");
        Invoke("EndAttack", shootDuration);
    }

    void ShootBullet(Vector2 direction)
    {
        // Player 레이어 무시냥
        int layerMask = ~LayerMask.GetMask("Player");

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction,
            100f,
            layerMask
        );

        Vector2 endPoint = hit.collider != null ? hit.point : (Vector2)transform.position + direction * 100f;
        StartCoroutine(ShowBulletTrail(transform.position, endPoint));

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            transform.position,
            direction,
            100f,
            layerMask
        );

        foreach (RaycastHit2D h in hits)
        {
            if (h.collider.CompareTag("Monster") || h.collider.CompareTag("OutLine") || h.collider.CompareTag("Wall"))
            {
                ShowHitEffect(h.point);
                Debug.Log($"권총 발사! 몬스터 피격 위치: {h.point}");
            }
        }
    }

    IEnumerator ShowBulletTrail(Vector2 startPos, Vector2 endPos)
    {
        GameObject bullet = new GameObject("Bullet");
        bullet.transform.position = startPos;
        SpriteRenderer bulletSR = bullet.AddComponent<SpriteRenderer>();
        bulletSR.sprite = bulletSprite;
        bulletSR.sortingOrder = 5;

        float distance = Vector2.Distance(startPos, endPos);
        float duration = distance / shootBulletSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bullet.transform.position = Vector2.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }

        Destroy(bullet);
    }

    void ShowHitEffect(Vector2 position)
    {
        GameObject effect = new GameObject("HitEffect");
        effect.transform.position = position;
        SpriteRenderer effectSR = effect.AddComponent<SpriteRenderer>();
        effectSR.sprite = hitEffectSprite;
        effectSR.sortingOrder = 4;
        Destroy(effect, 0.3f);
    }

    void EndAttack()
    {
        isAttacking = false;
        anim.SetBool("IsAttacking", false);
        attack_Left.enabled = false;
        attack_Right.enabled = false;
        attack_Front.enabled = false;
        attack_Back.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 공격 중일 때만 데미지 처리냥!
        if (isAttacking && !hasHitThisAttack && collision.CompareTag("Monster"))
        {
            hasHitThisAttack = true;
            Debug.Log($"근접공격! 몬스터 맞음: {collision.gameObject.name}");

            EnhancedMonsterAI monster = collision.GetComponent<EnhancedMonsterAI>();
            if (monster != null)
            {
                Vector2 knockDir = (collision.transform.position - transform.position).normalized;
                monster.TakeDamage(melee_damage, knockDir);
            }
        }
    }

    void SwitchWeapon(int weaponType)
    {
        if (currentWeapon == weaponType) return;
        currentWeapon = weaponType;
        anim.SetInteger("Weapon", weaponType);
        Debug.Log(weaponType == 0 ? "근접무기 선택" : "권총 선택");
    }

    float GetCurrentAnimationLength()
    {
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        return stateInfo.length / anim.speed;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (PlayerPrefs.HasKey("SpawnX"))
        {
            float x = PlayerPrefs.GetFloat("SpawnX");
            float y = PlayerPrefs.GetFloat("SpawnY");
            transform.position = new Vector3(x, y, 0);
            PlayerPrefs.DeleteKey("SpawnX");
            PlayerPrefs.DeleteKey("SpawnY");
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}