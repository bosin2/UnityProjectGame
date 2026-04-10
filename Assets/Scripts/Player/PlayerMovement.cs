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

    // Resources 폴더에서 로드
    private Sprite bulletSprite;
    private Sprite hitEffectSprite;

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

        // Resources 폴더에서 Sprite 로드
        bulletSprite = Resources.Load<Sprite>("Bullet");
        hitEffectSprite = Resources.Load<Sprite>("Boom");

        if (bulletSprite == null)
            Debug.LogError("Bullet 스프라이트 못 찾았! Resources 폴더에 Bullet.png 있는지 확인!");
        if (hitEffectSprite == null)
            Debug.LogError("Boom 스프라이트 못 찾았! Resources 폴더에 Boom.png 있는지 확인!");

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
        StartCoroutine(ShootBulletCoroutine(lastDir)); // 총알이 도착했을때 피격처리
        float shootDuration = GetCurrentAnimationLength();
        CancelInvoke("EndAttack");
        Invoke("EndAttack", shootDuration);
    }

    // 총알 날아가고 → 도착하면 피격 처리하는 코루틴
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

        // 총알 오브젝트 생성
        GameObject bullet = new GameObject("Bullet");
        bullet.transform.position = startPos;
        SpriteRenderer bulletSR = bullet.AddComponent<SpriteRenderer>();
        bulletSR.sprite = bulletSprite;
        bulletSR.sortingLayerName = "Default";
        bulletSR.sortingOrder = 10;

        // 총알 크기 설정
        bullet.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        // 총알 방향 회전
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

        // 총알 날아가기
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

        // 총알 도착한 다음에 피격 처리
        if (hit.collider != null)
        {
            ShowHitEffect(hit.point);

            if (hit.collider.CompareTag("Monster"))
            {
                Debug.Log($"권총 발사! 몬스터 피격 위치: {hit.point}");
                EnhancedMonsterAI monster = hit.collider.GetComponent<EnhancedMonsterAI>();
                if (monster != null)
                {
                    Vector2 knockDir = direction.normalized;
                    monster.TakeDamage(shoot_damage, knockDir);
                }
            }
        }
    }

    void ShowHitEffect(Vector2 position)
    {
        if (hitEffectSprite == null) return;

        GameObject effect = new GameObject("HitEffect");
        effect.transform.position = position;
        SpriteRenderer effectSR = effect.AddComponent<SpriteRenderer>();
        effectSR.sprite = hitEffectSprite;
        effectSR.sortingLayerName = "Default";
        effectSR.sortingOrder = 10;
        effect.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
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