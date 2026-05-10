using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// 플레이어 HP를 슬라이더 UI로 표시하고 피격 시 플래시 효과를 재생하는 싱글톤.
// UICanvas와 함께 DontDestroyOnLoad로 씬 전환 후에도 유지된다.
public class PlayerHPbar : MonoBehaviour
{
    public static PlayerHPbar Instance;

    [Header("UI 연결")]
    public Slider hpSlider;
    public Image fillImage;
    public GameObject damageFlash; // 피격 시 깜빡이는 플래시 오브젝트

    void Awake()
    {
        // UI 레퍼런스 누락 시 컴포넌트 비활성화
        if (hpSlider == null || fillImage == null)
        {
            Debug.LogWarning("[PlayerHPbar] HP UI references are missing. Disabling this component.", this);
            enabled = false;
            return;
        }

        if (Instance == null)
        {
            Instance = this;

            // 루트 오브젝트일 때만 DontDestroyOnLoad 적용 (Canvas 자식이면 적용 안 함)
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    // 씬 시작 시 현재 플레이어 HP로 슬라이더 초기화 (플래시 없음)
    void Start()
    {
        if (hpSlider == null || fillImage == null) return;
        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        if (player != null)
            Refresh(player.CurrentHp, player.maxHp, false);
    }

    // HP 비율에 따라 슬라이더와 색상을 갱신하고 선택적으로 플래시 효과 재생.
    // 50% 초과=초록, 25% 초과=노랑, 25% 이하=빨강
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

    // 피격 플래시 오브젝트를 0.2초 표시 후 숨김
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