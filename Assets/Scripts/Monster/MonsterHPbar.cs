using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("스탯")]
    public float maxHP = 50f;
    private float currentHP;

    [Header("HP바 UI")]
    public GameObject hpBarPrefab; // HP바 프리팹
    private Slider hpSlider;
    private GameObject hpBarInstance;

    [Header("설정")]
    public Vector3 hpBarOffset = new Vector3(0, 1.2f, 0); // 머리 위 위치

    void Start()
    {
        currentHP = maxHP;

        // HP바 생성
        if (hpBarPrefab != null)
        {
            hpBarInstance = Instantiate(hpBarPrefab);
            hpSlider = hpBarInstance.GetComponentInChildren<Slider>();
            UpdateHPBar();
        }
    }

    void LateUpdate()
    {
        // HP바가 항상 몬스터 머리 위에 따라다니게
        if (hpBarInstance != null)
            hpBarInstance.transform.position = transform.position + hpBarOffset;
    }

    public void TakeDamage(float amount)
    {
        currentHP = Mathf.Max(0f, currentHP - amount);
        UpdateHPBar();

        if (currentHP <= 0f) Die();
    }

    void UpdateHPBar()
    {
        if (hpSlider != null)
            hpSlider.value = currentHP / maxHP;
    }

    void Die()
    {
        if (hpBarInstance != null) Destroy(hpBarInstance);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (hpBarInstance != null) Destroy(hpBarInstance);
    }
}