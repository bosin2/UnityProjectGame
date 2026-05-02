using UnityEngine;
using UnityEngine.UI;

public class PlayerHPbar : MonoBehaviour
{
    public static PlayerHPbar Instance;

    [Header("스탯")]
    public float maxHP = 100f;
    private float currentHP;

    [Header("UI")]
    public Slider hpSlider;
    public Image fillImage; // Slider의 Fill 이미지
    public GameObject damageFlash; // 화면 가장자리 빨간 플래시 오브젝트

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentHP = maxHP;
        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        currentHP = Mathf.Max(0f, currentHP - amount);
        UpdateUI();
        StartCoroutine(FlashEffect());

        if (currentHP <= 0f) Die();
    }

    public void Heal(float amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        UpdateUI();
    }

    void UpdateUI()
    {
        hpSlider.value = currentHP / maxHP;

        // HP 비율에 따라 색상 변화
        float ratio = currentHP / maxHP;
        fillImage.color = ratio > 0.5f ? Color.green
                        : ratio > 0.25f ? Color.yellow
                        : Color.red;
    }

    System.Collections.IEnumerator FlashEffect()
    {
        if (damageFlash != null)
        {
            damageFlash.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            damageFlash.SetActive(false);
        }
    }

    void Die()
    {
        // 게임오버 처리
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
    }
}