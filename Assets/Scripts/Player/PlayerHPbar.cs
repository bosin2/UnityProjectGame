using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHPbar : MonoBehaviour
{
    public static PlayerHPbar Instance;

    [Header("UI 연결")]
    public Slider hpSlider;
    public Image fillImage;
    public GameObject damageFlash;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (hpSlider == null || fillImage == null) return;
        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        if (player != null)
            Refresh(player.CurrentHp, player.maxHp, false);
    }

    public void Refresh(int current, int max, bool showFlash = true)
    {
        if (hpSlider == null || fillImage == null) return;
        float ratio = (float)current / max;
        hpSlider.value = ratio;
        fillImage.color = ratio > 0.5f ? Color.green
                        : ratio > 0.25f ? Color.yellow
                        : Color.red;
        if (showFlash)
            StartCoroutine(FlashEffect());
    }

    IEnumerator FlashEffect()
    {
        if (damageFlash != null)
        {
            damageFlash.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            damageFlash.SetActive(false);
        }
    }
}