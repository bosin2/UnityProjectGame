using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 HP를 슬라이더 UI로 표시하는 컴포넌트.
/// PlayerHealth.OnHPChanged 이벤트를 구독해 반응형으로 갱신한다.
/// UICanvas의 일부로 씬 전환 후에도 유지된다.
/// </summary>
public class PlayerHPbar : MonoBehaviour
{
    [Header("UI 연결")]
    public Slider hpSlider;
    public Image  fillImage;
    public GameObject damageFlash; // 피격 시 깜빡이는 오브젝트

    private PlayerHealth playerHealth;

    void Awake()
    {
        if (hpSlider == null || fillImage == null)
        {
            Debug.LogWarning("[PlayerHPbar] HP UI 참조가 없습니다. 컴포넌트를 비활성화합니다.", this);
            enabled = false;
            return;
        }

        // 루트 오브젝트일 때만 DontDestroyOnLoad 적용
        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 플레이어를 찾아 이벤트 구독 (아직 없으면 코루틴으로 재시도)
        StartCoroutine(SubscribeToPlayer());
    }

    // 플레이어가 생성될 때까지 매 프레임 탐색 후 이벤트 구독
    IEnumerator SubscribeToPlayer()
    {
        while (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
            yield return null;
        }

        playerHealth.OnHPChanged += OnHPChanged;
        // 현재 HP로 초기화 (플래시 없이)
        Refresh(playerHealth.CurrentHp, playerHealth.maxHp, false);
    }

    /// <summary>HP 비율에 따라 슬라이더와 색상을 갱신. 선택적으로 피격 플래시 재생</summary>
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

    // Action<int,int> 델리게이트에 맞는 래퍼 메서드
    private void OnHPChanged(int current, int max) => Refresh(current, max, true);

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHPChanged -= OnHPChanged;
    }
}
