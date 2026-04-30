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

    [Header("피격 설정")]
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.2f;
    public float hurtDuration = 0.4f;

    [Header("HP 설정")]
    public int maxHp = 100;
    private int currentHp;
    private bool isDead = false;

    [Header("총알 설정")]
    public GameObject bulletPrefab;
    public GameObject hitEffectPrefab;

    private bool isHurt = false;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private Vector2 movement;
    private Vector2 lastDir = new Vector2(0, -1);
    private bool isAttacking = false;
    public int currentWeapon = 0; // ← public으로 변경

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

    void Start()
    {
        currentHp = maxHp;
    }

    void Update()
    {
        if (isDead) return;

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
            Attack();
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // 권총 소유했을 때만 변경 가능
            if (GameManager.instance != null && GameManager.instance.hasGun)
            {
                if (currentWeapon == 0)
                    SwitchWeapon(1);
                else
                    SwitchWeapon(0);
            }
        }
    }

    void FixedUpdate()
    {
        if (!isAttacking && !isHurt && !isDead)
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    public void TakeHit(Vector2 knockbackDirection)
    {
        if (isHurt || isDead) return;
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(HurtRoutine(knockbackDirection));
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHp -= amount;

        if (currentHp <= 0)
        {
            currentHp = 0;
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        movement = Vector2.zero;

        float dirX = anim.GetFloat("DirX");
        float dirY = anim.GetFloat("DirY");

        if (Mathf.Abs(dirX) >= Mathf.Abs(dirY))
            sr.flipX = dirX > 0;
        else
            sr.flipX = false;

        anim.SetBool("IsDie", true);
        StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        gameObject.SetActive(false);
    }

    IEnumerator HurtRoutine(Vector2 knockbackDirection)
    {
        isHurt = true;
        anim.SetBool("IsHurt", true);

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

        if (currentWeapon == 0)
            AttackMelee();
        else if (currentWeapon == 1)
            AttackShoot();
    }

    void AttackMelee()
    {
        anim.SetBool("IsAttacking", true);
    }

    public void OnMeleeHitStart()
    {
        if (isHurt || isDead) return;
        ActivateAttackCollider(lastDir);
    }

    public void OnMeleeHitEnd()
    {
        DisableAllAttackColliders();
        EndAttack();
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

    private void DisableAllAttackColliders()
    {
        attack_Left.enabled = false;
        attack_Right.enabled = false;
        attack_Front.enabled = false;
        attack_Back.enabled = false;
    }

    void AttackShoot()
    {
        anim.SetBool("IsAttacking", true);
        StartCoroutine(ShootBulletCoroutine(lastDir));
        float shootDuration = GetCurrentAnimationLength();
        CancelInvoke("EndAttack");
        Invoke("EndAttack", shootDuration);
    }

    IEnumerator ShootBulletCoroutine(Vector2 direction)
    {
        int layerMask = ~LayerMask.GetMask("Player");

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction,
            100f,
            layerMask
        );

        Vector2 startPos = transform.position;
        Vector2 endPos = hit.collider != null ? hit.point : startPos + direction * 100f;

        GameObject bullet = Instantiate(bulletPrefab, startPos, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(bullet, SceneManager.GetActiveScene());

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

        float distance = Vector2.Distance(startPos, endPos);
        float duration = distance / shootBulletSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (bullet == null) yield break;
            bullet.transform.position = Vector2.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }

        Destroy(bullet);

        if (hit.collider != null)
        {
            ShowHitEffect(hit.point);

            if (hit.collider.CompareTag("Monster"))
            {
                MonsterAI monster = hit.collider.GetComponent<MonsterAI>();
                if (monster != null) monster.TakeDamage(shoot_damage);
            }
        }
    }

    void ShowHitEffect(Vector2 position)
    {
        GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(effect, SceneManager.GetActiveScene());
        Destroy(effect, 0.3f);
    }

    void EndAttack()
    {
        isAttacking = false;
        anim.SetBool("IsAttacking", false);
        DisableAllAttackColliders();
    }

    void SwitchWeapon(int weaponType)
    {
        if (currentWeapon == weaponType) return;
        currentWeapon = weaponType;
        anim.SetInteger("Weapon", weaponType);
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